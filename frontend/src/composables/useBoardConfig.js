import { computed, watch } from 'vue'
import { Chess } from 'chess.js'
import { authService } from '../services/authService'

export function useBoardConfig({ game, chess, currentFen, moveHistory, currentMoveIndex, isPlayerTurn: isPlayerTurnRef, analysisMode, boardAPI, t }) {
  const playerColor = computed(() => {
    const userId = authService.getUserId()
    const isWhite = game.value?.whitePlayerId === userId
    return isWhite ? 'white' : 'black'
  })

  const isPlayerTurn = computed(() => {
    const userId = authService.getUserId()

    const fenParts = currentFen.value.split(' ')
    const currentTurn = fenParts[1] || 'w'

    if (!game.value || !userId) return false
    if (game.value.gameResult !== 1) return false

    const emptyGuid = '00000000-0000-0000-0000-000000000000'
    if (!game.value.blackPlayerId || game.value.blackPlayerId === emptyGuid) return false

    const isWhite = game.value.whitePlayerId === userId

    return (isWhite && currentTurn === 'w') || (!isWhite && currentTurn === 'b')
  })

  const boardConfig = computed(() => {
    const dests = new Map()
    const tempChess = new Chess(currentFen.value)

    if (analysisMode.value) {
      const moves = tempChess.moves({ verbose: true })
      moves.forEach(move => {
        if (!dests.has(move.from)) {
          dests.set(move.from, [])
        }
        dests.get(move.from).push(move.to)
      })
    } else if (isPlayerTurn.value && currentMoveIndex.value === moveHistory.value.length) {
      const moves = tempChess.moves({ verbose: true })
      moves.forEach(move => {
        if (!dests.has(move.from)) {
          dests.set(move.from, [])
        }
        dests.get(move.from).push(move.to)
      })
    }

    const userId = authService.getUserId()
    const isWhite = game.value?.whitePlayerId === userId

    let movableColor
    if (analysisMode.value) {
      movableColor = 'both'
    } else {
      movableColor = isWhite ? 'white' : 'black'
    }

    return {
      fen: currentFen.value,
      orientation: 'white',
      coordinates: true,
      movable: {
        free: false,
        color: movableColor,
        dests: dests
      },
      animation: {
        enabled: true,
        duration: 200
      }
    }
  })

  const gameStatus = computed(() => {
    const fenParts = currentFen.value.split(' ')
    const currentTurn = fenParts[1] || 'w'
    const currentColor = currentTurn === 'w' ? t('game.white') : t('game.black')
    const oppositeColor = currentTurn === 'w' ? t('game.black') : t('game.white')

    if (chess.isGameOver()) {
      if (chess.isCheckmate()) {
        return t('game.checkmate', { color: oppositeColor })
      } else if (chess.isDraw()) {
        return t('game.draw')
      }
    } else if (chess.inCheck()) {
      return t('game.inCheck', { color: currentColor })
    }

    if (analysisMode.value) {
      return t('game.analysisMode', { current: currentMoveIndex.value, total: moveHistory.value.length })
    }

    return t('game.toMove', { color: currentColor })
  })

  const currentPlayer = computed(() => {
    return chess.turn() === 'w' ? 'White' : 'Black'
  })

  const blackPlayerLabel = computed(() => {
    const botPlayerId = '00000000-0000-0000-0000-000000000001'
    if (game.value?.blackPlayerId === botPlayerId) {
      return t('game.bot')
    }
    return game.value?.blackPlayerUsername || t('game.waiting')
  })

  const whitePlayerLabel = computed(() => {
    const botPlayerId = '00000000-0000-0000-0000-000000000001'
    if (game.value?.whitePlayerId === botPlayerId) {
      return t('game.bot')
    }
    return game.value?.whitePlayerUsername || t('game.white')
  })

  const gameOverMessage = computed(() => {
    if (chess.isCheckmate()) {
      // Bug fix: was returning blackWinsByCheckmate for both branches
      return chess.turn() === 'w' ? t('game.blackWinsByCheckmate') : t('game.whiteWinsByCheckmate')
    }
    if (chess.isStalemate()) {
      return t('game.drawByStalemate')
    }
    if (chess.isThreefoldRepetition()) {
      return t('game.drawByRepetition')
    }
    if (chess.isInsufficientMaterial()) {
      return t('game.drawByInsufficientMaterial')
    }
    if (chess.isDraw()) {
      return t('game.draw')
    }

    const gr = game.value?.gameResult || 0
    switch (gr) {
      case 2: return t('game.whiteWins')
      case 3: return t('game.blackWins')
      case 4: return t('game.draw')
      default: return t('game.gameOver')
    }
  })

  const gameOverIcon = computed(() => {
    if (chess.isCheckmate()) {
      return chess.turn() === 'w' ? '♚' : '♔'
    }
    const gr = game.value?.gameResult || 0
    if (gr === 2) return '♔'
    if (gr === 3) return '♚'
    if (gr === 4 || chess.isDraw()) return '½'
    return '🏁'
  })

  // Watchers
  watch(currentFen, (newFen) => {
    if (boardAPI.value) {
      boardAPI.value.setPosition(newFen)

      const config = boardConfig.value
      if (boardAPI.value.board && boardAPI.value.board.set) {
        boardAPI.value.board.set({
          movable: config.movable
        })
      }
    }
  })

  watch(() => game.value?.gameResult, () => {
    if (boardAPI.value && boardAPI.value.board && boardAPI.value.board.set) {
      const config = boardConfig.value
      boardAPI.value.board.set({
        movable: config.movable
      })
    }
  })

  watch(() => game.value?.blackPlayerId, () => {
    if (boardAPI.value && boardAPI.value.board && boardAPI.value.board.set) {
      const config = boardConfig.value
      boardAPI.value.board.set({
        movable: config.movable
      })
    }
  })

  return {
    boardConfig,
    isPlayerTurn,
    playerColor,
    gameStatus,
    currentPlayer,
    blackPlayerLabel,
    whitePlayerLabel,
    gameOverMessage,
    gameOverIcon
  }
}
