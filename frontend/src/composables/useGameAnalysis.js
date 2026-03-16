import { ref } from 'vue'
import { Chess } from 'chess.js'
import { gameService } from '../services/gameService'

export function useGameAnalysis({ chess }) {
  const analysisMode = ref(false)
  const analyzing = ref(false)
  const topMoves = ref([])
  const currentEvaluation = ref({ score: 0, isMate: false, mateIn: null })
  const currentAnalysisDepth = ref(0)
  const analysisAbortController = ref(null)

  // Late-bound navigation refs
  let navRefs = null

  function setNavigationRefs(refs) {
    navRefs = refs
  }

  function toggleAnalysisMode() {
    analysisMode.value = !analysisMode.value

    if (analysisMode.value) {
      analyzeCurrentPosition()
    } else {
      if (analysisAbortController.value) {
        analysisAbortController.value.aborted = true
      }
      if (navRefs && navRefs.exitVariation) {
        navRefs.exitVariation()
      }
      topMoves.value = []
      currentEvaluation.value = { score: 0, isMate: false, mateIn: null }
      currentAnalysisDepth.value = 0
    }
  }

  async function analyzeCurrentPosition() {
    if (!analysisMode.value) return

    if (analysisAbortController.value) {
      analysisAbortController.value.aborted = true
    }
    analysisAbortController.value = {
      aborted: false,
      abort: function () { this.aborted = true }
    }
    const currentController = analysisAbortController.value

    analyzing.value = true
    const currentFen = chess.fen()

    const depths = [12, 18, 22]

    try {
      for (const depth of depths) {
        if (currentController.aborted || chess.fen() !== currentFen) {
          break
        }

        currentAnalysisDepth.value = depth

        const result = await gameService.analyzeMultiPv(currentFen, depth, 3)

        if (currentController.aborted || chess.fen() !== currentFen) {
          break
        }

        if (result.success && result.lines) {
          topMoves.value = result.lines.map(line => ({
            ...line,
            moveSan: convertUciToSan(line.move)
          }))

          if (topMoves.value.length > 0) {
            const bestLine = topMoves.value[0]
            currentEvaluation.value = {
              score: bestLine.score,
              isMate: bestLine.isMate,
              mateIn: bestLine.mateIn
            }
          }
        }

        if (depth < depths[depths.length - 1]) {
          await new Promise(resolve => setTimeout(resolve, 50))
        }
      }
    } catch (error) {
      console.error('Analysis error:', error)
    } finally {
      if (currentController === analysisAbortController.value) {
        analyzing.value = false
      }
    }
  }

  function convertUciToSan(uciMove) {
    if (!uciMove || uciMove.length < 4) return uciMove

    try {
      const tempChess = new Chess(chess.fen())
      const from = uciMove.substring(0, 2)
      const to = uciMove.substring(2, 4)
      const promotion = uciMove.length > 4 ? uciMove[4] : undefined

      const move = tempChess.move({ from, to, promotion })
      return move ? move.san : uciMove
    } catch {
      return uciMove
    }
  }

  function playMove(uciMove, onMove) {
    if (!uciMove || uciMove.length < 4) return

    const from = uciMove.substring(0, 2)
    const to = uciMove.substring(2, 4)
    const promotion = uciMove.length > 4 ? uciMove[4] : 'q'

    onMove({ from, to, promotion })
  }

  function formatEval(line) {
    if (line.isMate) {
      return `M${line.mateIn}`
    }
    const pawns = line.score / 100
    const sign = pawns >= 0 ? '+' : ''
    return sign + pawns.toFixed(1)
  }

  function getEvalClass(line) {
    if (line.isMate) {
      return line.mateIn > 0 ? 'eval-winning' : 'eval-losing'
    }
    if (line.score > 100) return 'eval-winning'
    if (line.score < -100) return 'eval-losing'
    return 'eval-equal'
  }

  return {
    analysisMode,
    analyzing,
    topMoves,
    currentEvaluation,
    currentAnalysisDepth,
    analysisAbortController,
    setNavigationRefs,
    toggleAnalysisMode,
    analyzeCurrentPosition,
    convertUciToSan,
    playMove,
    formatEval,
    getEvalClass
  }
}
