import * as signalR from '@microsoft/signalr'
import { authService } from './authService'
import { getSignalRHubUrl } from '../config/apiConfig'

class GameConnectionService {
  constructor() {
    this.connection = null
    this.currentGameId = null
    this.isConnected = false
    this.reconnectAttempts = 0
    this.maxReconnectAttempts = 5
    this.eventHandlers = new Map()
  }

  async connect() {
    if (this.isConnected) {
      console.log('SignalR: Already connected')
      return true
    }

    try {
      const token = authService.getToken()
      const hubUrl = getSignalRHubUrl()

      console.log(`SignalR: Connecting to ${hubUrl}`)

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.previousRetryCount < 5) {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000)
            }
            return null
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Setup connection event handlers
      this.connection.onreconnecting((error) => {
        console.warn('SignalR: Connection lost, reconnecting...', error)
        this.isConnected = false
      })

      this.connection.onreconnected((connectionId) => {
        console.log('SignalR: Reconnected with ID:', connectionId)
        this.isConnected = true
        this.reconnectAttempts = 0

        // Rejoin current game if exists
        if (this.currentGameId) {
          this.joinGame(this.currentGameId)
        }
      })

      this.connection.onclose((error) => {
        console.error('SignalR: Connection closed', error)
        this.isConnected = false

        // Attempt manual reconnect
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++
          const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000)
          console.log(`SignalR: Attempting manual reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`)
          setTimeout(() => this.connect(), delay)
        }
      })

      // Setup game event handlers
      this.connection.on('MoveReceived', (data) => {
        console.log('SignalR: Move received:', data)
        this.emit('moveReceived', data)
      })

      this.connection.on('GameStateChanged', (data) => {
        console.log('SignalR: Game state changed:', data)
        this.emit('gameStateChanged', data)
      })

      this.connection.on('PlayerJoined', (data) => {
        console.log('SignalR: Player joined:', data)
        this.emit('playerJoined', data)
      })

      this.connection.on('PlayerLeft', (data) => {
        console.log('SignalR: Player left:', data)
        this.emit('playerLeft', data)
      })

      await this.connection.start()
      this.isConnected = true
      this.reconnectAttempts = 0
      console.log('SignalR: Connected successfully')
      return true
    } catch (error) {
      console.error('SignalR: Connection failed', error)
      this.isConnected = false
      return false
    }
  }

  async disconnect() {
    if (!this.connection) {
      return
    }

    try {
      if (this.currentGameId) {
        await this.leaveGame(this.currentGameId)
      }

      await this.connection.stop()
      this.isConnected = false
      this.currentGameId = null
      this.eventHandlers.clear()
      console.log('SignalR: Disconnected')
    } catch (error) {
      console.error('SignalR: Error during disconnect', error)
    }
  }

  async joinGame(gameId) {
    if (!this.isConnected) {
      console.warn('SignalR: Not connected, attempting to connect...')
      const connected = await this.connect()
      if (!connected) {
        throw new Error('Failed to connect to SignalR')
      }
    }

    try {
      await this.connection.invoke('JoinGame', gameId)
      this.currentGameId = gameId
      console.log(`SignalR: Joined game ${gameId}`)
      return true
    } catch (error) {
      console.error('SignalR: Error joining game', error)
      throw error
    }
  }

  async leaveGame(gameId) {
    if (!this.isConnected || !this.connection) {
      return
    }

    try {
      await this.connection.invoke('LeaveGame', gameId)
      if (this.currentGameId === gameId) {
        this.currentGameId = null
      }
      console.log(`SignalR: Left game ${gameId}`)
    } catch (error) {
      console.error('SignalR: Error leaving game', error)
    }
  }

  on(eventName, handler) {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, [])
    }
    this.eventHandlers.get(eventName).push(handler)
  }

  off(eventName, handler) {
    if (!this.eventHandlers.has(eventName)) {
      return
    }

    if (!handler) {
      this.eventHandlers.delete(eventName)
    } else {
      const handlers = this.eventHandlers.get(eventName)
      const index = handlers.indexOf(handler)
      if (index > -1) {
        handlers.splice(index, 1)
      }
    }
  }

  emit(eventName, data) {
    if (!this.eventHandlers.has(eventName)) {
      return
    }

    const handlers = this.eventHandlers.get(eventName)
    handlers.forEach(handler => {
      try {
        handler(data)
      } catch (error) {
        console.error(`SignalR: Error in event handler for ${eventName}`, error)
      }
    })
  }

  getConnectionState() {
    if (!this.connection) {
      return 'Disconnected'
    }

    const states = {
      [signalR.HubConnectionState.Disconnected]: 'Disconnected',
      [signalR.HubConnectionState.Connected]: 'Connected',
      [signalR.HubConnectionState.Connecting]: 'Connecting',
      [signalR.HubConnectionState.Reconnecting]: 'Reconnecting'
    }

    return states[this.connection.state] || 'Unknown'
  }
}

export const gameConnectionService = new GameConnectionService()
