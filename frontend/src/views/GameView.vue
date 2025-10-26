<template>
  <div class="game-container">
    <div class="game-header">
      <h1>Game #{{ gameId.substring(0, 8) }}</h1>
      <div class="game-info">
        <span class="connection-status" :class="connectionStatusClass">
          {{ connectionStatus }}
        </span>
        <span class="game-status">{{ gameStatus }}</span>
        <button @click="analyzePosition" class="btn btn-primary" :disabled="analyzing">
          {{ analyzing ? 'Analyzing...' : 'Analyze' }}
        </button>
      </div>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <div v-if="suggestion" class="suggestion">
      <strong>Engine suggestion:</strong> {{ suggestion }}
    </div>

    <div class="game-board">
      <div class="chessboard-container">
        <TheChessboard
          :board-config="boardConfig"
          @board-created="(api) => (boardAPI = api)"
          @move="onMove"
        />
      </div>
      
      <div class="game-sidebar">
        <div class="game-details">
          <h3>Game Details</h3>
          <p><strong>Game ID:</strong> {{ gameId }}</p>
          <p><strong>Current Player:</strong> {{ currentPlayer }}</p>
          <p><strong>Turn:</strong> {{ chess.turn() === 'w' ? 'White' : 'Black' }}</p>
        </div>

        <div class="move-history">
          <h3>Move History</h3>
          <div class="moves-list">
            <div v-for="(move, index) in moveHistory" :key="index" class="move-item">
              {{ Math.floor(index / 2) + 1 }}{{ index % 2 === 0 ? '.' : '...' }} {{ move }}
            </div>
          </div>
        </div>

        <div class="game-controls">
          <button @click="goBack" class="btn btn-secondary">
            Back to Games
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { Chess } from 'chess.js'
import { TheChessboard } from 'vue3-chessboard'
import 'vue3-chessboard/style.css'
import { gameService } from '../services/gameService'
import { authService } from '../services/authService'
import { gameConnectionService } from '../services/gameConnectionService'
import { markRaw } from 'vue'

export default {
  name: 'GameView',
  components: {
    TheChessboard
  },
  props: {
    gameId: {
      type: String,
      required: true
    }
  },
  data() {
    return {
      game: null,
      chess: markRaw(new Chess()), // Make chess instance non-reactive
      boardAPI: null,
      error: '',
      analyzing: false,
      suggestion: '',
      moveHistory: [],
      loading: true,
      currentFen: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1', // Track FEN manually
      connectionStatus: 'Disconnected'
    }
  },
  computed: {
    boardConfig() {
      // Calculate dests inline without caching
      const dests = new Map()
      const moves = this.chess.moves({ verbose: true })

      moves.forEach(move => {
        if (!dests.has(move.from)) {
          dests.set(move.from, [])
        }
        dests.get(move.from).push(move.to)
      })

      return {
        fen: this.currentFen,
        orientation: 'white',
        movable: {
          free: false,
          color: this.chess.turn() === 'w' ? 'white' : 'black',
          dests: dests
        },
        animation: {
          enabled: true,
          duration: 200
        }
      }
    },

    gameStatus() {
      if (this.chess.isGameOver()) {
        if (this.chess.isCheckmate()) {
          return `Checkmate! ${this.chess.turn() === 'w' ? 'Black' : 'White'} wins!`
        } else if (this.chess.isDraw()) {
          return 'Draw!'
        }
      } else if (this.chess.inCheck()) {
        return `${this.chess.turn() === 'w' ? 'White' : 'Black'} is in check`
      }
      
      return `${this.chess.turn() === 'w' ? 'White' : 'Black'} to move`
    },
    
    currentPlayer() {
      return this.chess.turn() === 'w' ? 'White' : 'Black'
    },

    isPlayerTurn() {
      const userId = authService.getUserId()
      if (!this.game || !userId) return true // Allow moves for demo

      const isWhite = this.game.creatorId === userId
      const currentTurn = this.chess.turn()

      return (isWhite && currentTurn === 'w') || (!isWhite && currentTurn === 'b')
    },

    connectionStatusClass() {
      return {
        'status-connected': this.connectionStatus === 'Connected',
        'status-connecting': this.connectionStatus === 'Connecting' || this.connectionStatus === 'Reconnecting',
        'status-disconnected': this.connectionStatus === 'Disconnected'
      }
    }
  },
  async mounted() {
    await this.initializeGame()
    await this.setupRealtimeConnection()
  },
  async unmounted() {
    await this.cleanupRealtimeConnection()
  },
  methods: {
    async initializeGame() {
      try {
        // Load game state from backend
        const result = await gameService.getGameById(this.gameId)

        if (result.success && result.game) {
          this.game = result.game

          // If game has a FEN, use it; otherwise use initial position
          const fenToLoad = result.game.currentFen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'

          this.chess.load(fenToLoad)
          this.currentFen = fenToLoad

          // TODO: Load move history if available
          // if (result.game.moves) {
          //   this.moveHistory = result.game.moves.map(m => m.move)
          // }

          console.log('Game loaded:', this.game)
        } else {
          // If game doesn't exist yet or failed to load, start with initial position
          this.chess.reset()
          this.currentFen = this.chess.fen()
          console.warn('Failed to load game, starting with initial position')
        }

        this.loading = false
      } catch (error) {
        this.error = 'Failed to initialize game'
        console.error('Game initialization error:', error)

        // Fallback to initial position
        this.chess.reset()
        this.currentFen = this.chess.fen()
        this.loading = false
      }
    },

    async setupRealtimeConnection() {
      try {
        console.log('Setting up real-time connection for game:', this.gameId)

        // Connect to SignalR hub
        await gameConnectionService.connect()
        this.connectionStatus = gameConnectionService.getConnectionState()

        // Join this game's room
        await gameConnectionService.joinGame(this.gameId)

        // Listen for move updates from other players
        gameConnectionService.on('moveReceived', this.handleMoveReceived)

        // Listen for game state changes
        gameConnectionService.on('gameStateChanged', this.handleGameStateChanged)

        // Listen for player join/leave events
        gameConnectionService.on('playerJoined', this.handlePlayerJoined)
        gameConnectionService.on('playerLeft', this.handlePlayerLeft)

        console.log('Real-time connection established')
      } catch (error) {
        console.error('Failed to setup real-time connection:', error)
        this.error = 'Failed to connect to game server. Moves may not sync in real-time.'
      }
    },

    async cleanupRealtimeConnection() {
      try {
        console.log('Cleaning up real-time connection')

        // Remove event listeners
        gameConnectionService.off('moveReceived', this.handleMoveReceived)
        gameConnectionService.off('gameStateChanged', this.handleGameStateChanged)
        gameConnectionService.off('playerJoined', this.handlePlayerJoined)
        gameConnectionService.off('playerLeft', this.handlePlayerLeft)

        // Leave game room
        if (this.gameId) {
          await gameConnectionService.leaveGame(this.gameId)
        }

        console.log('Real-time connection cleaned up')
      } catch (error) {
        console.error('Error cleaning up connection:', error)
      }
    },

    handleMoveReceived(data) {
      console.log('Received move from server:', data)

      const currentUserId = authService.getUserId()

      // Don't apply the move if it's from the current user (already applied locally)
      if (data.userId === currentUserId) {
        console.log('Ignoring own move')
        return
      }

      try {
        // Apply the move from the server
        // Option 1: If server sends move in SAN format
        if (data.move) {
          const move = this.chess.move(data.move)
          if (move) {
            this.moveHistory.push(move.san)
          }
        }

        // Option 2: If server sends new FEN (more reliable)
        if (data.newFen) {
          this.chess.load(data.newFen)
        }

        // Update the board
        this.currentFen = this.chess.fen()

        console.log('Move applied successfully')
      } catch (error) {
        console.error('Error applying received move:', error)
        this.error = 'Failed to apply opponent\'s move'
      }
    },

    handleGameStateChanged(data) {
      console.log('Game state changed:', data)
      // Handle game state changes (e.g., game ended, draw offered, etc.)
      if (data.gameResult) {
        // Update game result display
      }
    },

    handlePlayerJoined(data) {
      console.log('Player joined:', data)
      // Show notification that opponent joined
      if (this.game) {
        this.game.blackPlayerId = data.playerId
      }
    },

    handlePlayerLeft(data) {
      console.log('Player left:', data)
      // Show notification that opponent left
    },

    onMove(move) {
      this.error = ''
      console.log('onMove called with:', move)
      const startTime = performance.now()

      try {
        const chessMove = this.chess.move({
          from: move.from,
          to: move.to,
          promotion: 'q'
        })

        if (chessMove === null) {
          this.error = 'Invalid move'
          console.log('Invalid move')
          return
        }

        console.log('Move executed:', chessMove.san, `(${(performance.now() - startTime).toFixed(2)}ms)`)
        this.moveHistory.push(chessMove.san)

        // Update FEN to trigger board update
        const updateStart = performance.now()
        this.currentFen = this.chess.fen()
        console.log(`FEN updated in ${(performance.now() - updateStart).toFixed(2)}ms`)

        // Send move to backend asynchronously (don't wait)
        console.log('Sending move to backend...')
        gameService.makeMove(
          this.gameId,
          chessMove.san,
          this.chess.fen()
        ).then(result => {
          console.log('Backend response:', result)
          if (!result.success) {
            // Undo the move if backend rejects it
            this.chess.undo()
            this.moveHistory.pop()
            this.currentFen = this.chess.fen()
            this.error = result.error
          }
        }).catch(error => {
          console.error('Backend error:', error)
          this.error = 'Failed to make move'
        })

        console.log(`Total onMove time: ${(performance.now() - startTime).toFixed(2)}ms`)
      } catch (error) {
        console.error('Move error:', error)
        this.error = 'Failed to make move'
      }
    },

    async analyzePosition() {
      this.analyzing = true
      this.suggestion = ''

      try {
        // Using depth 5 for faster analysis (was 10, which was very slow)
        const result = await gameService.analyzePosition(this.chess.fen(), 5)
        if (result.success) {
          this.suggestion = result.bestMove
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Failed to analyze position'
        console.error('Analysis error:', error)
      } finally {
        this.analyzing = false
      }
    },

    goBack() {
      this.$router.push('/games')
    }
  }
}
</script>

<style scoped>
.game-container {
  max-width: 1400px;
  margin: 0 auto;
}

.game-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid #ecf0f1;
}

.game-header h1 {
  color: #2c3e50;
  margin: 0;
}

.game-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.game-status {
  font-weight: bold;
  color: #27ae60;
  font-size: 1.1rem;
}

.suggestion {
  background-color: #e8f5e8;
  border: 1px solid #27ae60;
  border-radius: 4px;
  padding: 1rem;
  margin-bottom: 1rem;
  color: #2d5a2d;
}

.game-board {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 1024px) {
  .game-board {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
}

.chessboard-container {
  background: white;
  border-radius: 8px;
  padding: 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  max-width: 600px;
  margin: 0 auto;
}

.game-sidebar {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  height: fit-content;
}

.game-details,
.move-history,
.game-controls {
  margin-bottom: 2rem;
}

.game-details h3,
.move-history h3 {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.2rem;
  border-bottom: 1px solid #ecf0f1;
  padding-bottom: 0.5rem;
}

.game-details p {
  margin: 0.5rem 0;
  color: #7f8c8d;
}

.moves-list {
  max-height: 200px;
  overflow-y: auto;
  border: 1px solid #ecf0f1;
  border-radius: 4px;
  padding: 0.5rem;
}

.move-item {
  padding: 0.25rem;
  font-family: monospace;
  font-size: 0.9rem;
  color: #2c3e50;
}

.move-item:nth-child(even) {
  background-color: #f8f9fa;
}

.game-controls .btn {
  width: 100%;
}

.connection-status {
  font-size: 0.9rem;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-weight: 500;
}

.status-connected {
  background-color: #d4edda;
  color: #155724;
}

.status-connecting {
  background-color: #fff3cd;
  color: #856404;
}

.status-disconnected {
  background-color: #f8d7da;
  color: #721c24;
}
</style>