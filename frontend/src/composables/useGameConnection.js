import { ref, computed, onMounted, onUnmounted } from 'vue'
import { gameConnectionService } from '../services/gameConnectionService'
import { authService } from '../services/authService'

export function useGameConnection(gameId, { game, chess, currentFen, moveHistory, fenHistory, currentMoveIndex, error, stopTimer, t }) {
  const connectionStatus = ref('')

  const translatedConnectionStatus = computed(() => {
    const statusMap = {
      'Connected': t('game.connected'),
      'Disconnected': t('game.disconnected'),
      'Connecting': t('game.connecting'),
      'Reconnecting': t('game.reconnecting')
    }
    return statusMap[connectionStatus.value] || connectionStatus.value
  })

  const connectionStatusClass = computed(() => ({
    'status-connected': connectionStatus.value === 'Connected',
    'status-connecting': connectionStatus.value === 'Connecting' || connectionStatus.value === 'Reconnecting',
    'status-disconnected': connectionStatus.value === 'Disconnected'
  }))

  function handleMoveReceived(data) {
    console.log('Received move from server:', data)

    const currentUserId = authService.getUserId()

    if (data.whiteTimeRemainingSeconds !== undefined) {
      // Use setTimes if available, otherwise directly update refs exposed by timer composable
      // Timer refs are passed separately — use whiteTimeRemaining/blackTimeRemaining from parent
    }

    if (data.userId === currentUserId) {
      return
    }

    try {
      if (data.newFen) {
        chess.load(data.newFen)

        if (data.move) {
          moveHistory.value.push(data.move)
          fenHistory.value.push(data.newFen)
          currentMoveIndex.value = moveHistory.value.length
        }
      } else if (data.move) {
        const move = chess.move(data.move)
        if (move) {
          moveHistory.value.push(move.san)
          fenHistory.value.push(chess.fen())
          currentMoveIndex.value = moveHistory.value.length
        }
      }

      currentFen.value = chess.fen()
    } catch (err) {
      console.error('Error applying received move:', err)
      error.value = t('game.failedToApplyMove')
    }
  }

  function handleGameStateChanged(data) {
    console.log('Game state changed:', data)

    if (data.gameResult !== undefined && game.value) {
      game.value.gameResult = data.gameResult

      if (data.gameResult >= 2) {
        stopTimer()
      }
    }
  }

  function handlePlayerJoined(data) {
    console.log('Player joined:', data)
    if (game.value && game.value.gameResult === 0) {
      game.value.blackPlayerId = data.playerId
      game.value.blackPlayerUsername = data.username || 'Player'
      game.value.gameResult = 1
      console.log('Game started - gameResult set to InProgress')
    }
  }

  function handlePlayerLeft(data) {
    console.log('Player left:', data)
  }

  // Accepts whiteTimeRemaining/blackTimeRemaining refs so we can update them on move received
  let timerRefs = null

  function setTimerRefs(refs) {
    timerRefs = refs
  }

  // Wrap handleMoveReceived to also update timer
  function handleMoveReceivedWithTimer(data) {
    if (timerRefs) {
      if (data.whiteTimeRemainingSeconds !== undefined) {
        timerRefs.whiteTimeRemaining.value = data.whiteTimeRemainingSeconds
      }
      if (data.blackTimeRemainingSeconds !== undefined) {
        timerRefs.blackTimeRemaining.value = data.blackTimeRemainingSeconds
      }
    }

    const currentUserId = authService.getUserId()

    if (data.userId === currentUserId) {
      return
    }

    try {
      if (data.newFen) {
        chess.load(data.newFen)

        if (data.move) {
          moveHistory.value.push(data.move)
          fenHistory.value.push(data.newFen)
          currentMoveIndex.value = moveHistory.value.length
        }
      } else if (data.move) {
        const move = chess.move(data.move)
        if (move) {
          moveHistory.value.push(move.san)
          fenHistory.value.push(chess.fen())
          currentMoveIndex.value = moveHistory.value.length
        }
      }

      currentFen.value = chess.fen()
    } catch (err) {
      console.error('Error applying received move:', err)
      error.value = t('game.failedToApplyMove')
    }
  }

  async function setupRealtimeConnection() {
    try {
      console.log('Setting up real-time connection for game:', gameId)

      await gameConnectionService.connect()
      connectionStatus.value = gameConnectionService.getConnectionState()

      await gameConnectionService.joinGame(gameId)

      gameConnectionService.on('moveReceived', handleMoveReceivedWithTimer)
      gameConnectionService.on('gameStateChanged', handleGameStateChanged)
      gameConnectionService.on('playerJoined', handlePlayerJoined)
      gameConnectionService.on('playerLeft', handlePlayerLeft)

      console.log('Real-time connection established')
    } catch (err) {
      console.error('Failed to setup real-time connection:', err)
      error.value = t('game.failedToConnectToServer')
    }
  }

  async function cleanupRealtimeConnection() {
    try {
      console.log('Cleaning up real-time connection')

      gameConnectionService.off('moveReceived', handleMoveReceivedWithTimer)
      gameConnectionService.off('gameStateChanged', handleGameStateChanged)
      gameConnectionService.off('playerJoined', handlePlayerJoined)
      gameConnectionService.off('playerLeft', handlePlayerLeft)

      if (gameId) {
        await gameConnectionService.leaveGame(gameId)
      }

      console.log('Real-time connection cleaned up')
    } catch (err) {
      console.error('Error cleaning up connection:', err)
    }
  }

  return {
    connectionStatus,
    translatedConnectionStatus,
    connectionStatusClass,
    setTimerRefs,
    setupRealtimeConnection,
    cleanupRealtimeConnection
  }
}
