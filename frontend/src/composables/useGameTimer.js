import { ref, onMounted, onUnmounted } from 'vue'

export function useGameTimer({ chess, hasTimeControl, gameResult, t }) {
  const whiteTimeRemaining = ref(0)
  const blackTimeRemaining = ref(0)
  const timerInterval = ref(null)
  const analysisMode = ref(null) // late-bound ref from analysis composable

  function setTimes(white, black) {
    whiteTimeRemaining.value = white
    blackTimeRemaining.value = black
  }

  function setAnalysisModeRef(analysisModeRef) {
    analysisMode.value = analysisModeRef
  }

  function startTimer(error) {
    timerInterval.value = setInterval(() => {
      const inAnalysis = analysisMode.value ? analysisMode.value.value : false
      if (hasTimeControl.value && gameResult.value === 1 && !inAnalysis) {
        if (chess.turn() === 'w') {
          whiteTimeRemaining.value = Math.max(0, whiteTimeRemaining.value - 1)
          if (whiteTimeRemaining.value === 0) {
            error.value = t('game.whiteTimeout')
            stopTimer()
          }
        } else {
          blackTimeRemaining.value = Math.max(0, blackTimeRemaining.value - 1)
          if (blackTimeRemaining.value === 0) {
            error.value = t('game.blackTimeout')
            stopTimer()
          }
        }
      }
    }, 1000)
  }

  function stopTimer() {
    if (timerInterval.value) {
      clearInterval(timerInterval.value)
      timerInterval.value = null
    }
  }

  return {
    whiteTimeRemaining,
    blackTimeRemaining,
    setTimes,
    setAnalysisModeRef,
    startTimer,
    stopTimer
  }
}
