import { ref, computed, markRaw } from 'vue'
import { Chess } from 'chess.js'
import { gameService } from '../services/gameService'

const START_FEN = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'

export function useGameCore(gameId, { t }) {
  const game = ref(null)
  const chess = markRaw(new Chess())
  const boardAPI = ref(null)
  const error = ref('')
  const loading = ref(true)
  const currentFen = ref(START_FEN)
  const moveHistory = ref([])
  const fenHistory = ref([])

  // Computed
  const gameResult = computed(() => game.value?.gameResult || 0)
  const isGameOver = computed(() => gameResult.value >= 2 || chess.isGameOver())
  const hasTimeControl = computed(() => game.value && game.value.timeControl !== 0)

  // Methods
  function onBoardCreated(api) {
    boardAPI.value = markRaw(api)
  }

  async function initializeGame() {
    try {
      const result = await gameService.getGameById(gameId)

      if (result.success && result.game) {
        game.value = result.game

        const fenToLoad = result.game.currentFen || START_FEN

        chess.load(fenToLoad)
        currentFen.value = fenToLoad

        // Build FEN history from moves
        fenHistory.value = [START_FEN]
        const tempChess = new Chess()

        if (result.game.moves && result.game.moves.length > 0) {
          moveHistory.value = []
          for (const m of result.game.moves) {
            let san = m.move
            try {
              const chessMove = tempChess.move(m.move)
              san = chessMove.san
              fenHistory.value.push(tempChess.fen())
            } catch {
              try {
                if (m.move.length >= 4) {
                  const from = m.move.substring(0, 2)
                  const to = m.move.substring(2, 4)
                  const promotion = m.move.length > 4 ? m.move[4] : undefined
                  const chessMove = tempChess.move({ from, to, promotion })
                  san = chessMove.san
                  fenHistory.value.push(tempChess.fen())
                }
              } catch {
                if (m.resultFen) {
                  tempChess.load(m.resultFen)
                  fenHistory.value.push(m.resultFen)
                }
              }
            }
            moveHistory.value.push(san)
          }
        }

        loading.value = false
        return { success: true }
      } else {
        chess.reset()
        currentFen.value = chess.fen()
        fenHistory.value = [currentFen.value]
        console.warn('Failed to load game, starting with initial position')
        loading.value = false
        return { success: false }
      }
    } catch (err) {
      error.value = t('game.failedToInitializeGame')
      console.error('Game initialization error:', err)

      chess.reset()
      currentFen.value = chess.fen()
      fenHistory.value = [currentFen.value]
      loading.value = false
      return { success: false }
    }
  }

  // Helpers for other composables to mutate shared state
  function pushMove(san, fen) {
    moveHistory.value.push(san)
    fenHistory.value.push(fen)
  }

  function popMove() {
    moveHistory.value.pop()
    fenHistory.value.pop()
  }

  function loadPosition(fen) {
    chess.load(fen)
    currentFen.value = fen
  }

  function resetError() {
    error.value = ''
  }

  return {
    game,
    chess,
    boardAPI,
    error,
    loading,
    currentFen,
    moveHistory,
    fenHistory,
    gameResult,
    isGameOver,
    hasTimeControl,
    onBoardCreated,
    initializeGame,
    pushMove,
    popMove,
    loadPosition,
    resetError
  }
}
