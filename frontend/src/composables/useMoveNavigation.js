import { ref, computed, onMounted, onUnmounted } from 'vue'

export function useMoveNavigation({ chess, currentFen, moveHistory, fenHistory }) {
  const currentMoveIndex = ref(0)
  const isInVariation = ref(false)
  const variationStartIndex = ref(0)
  const savedMoveHistory = ref([])
  const savedFenHistory = ref([])
  const savedMoveIndex = ref(0)

  // Late-bound analysis callback
  let analyzeCallback = null

  function setAnalyzeCallback(cb) {
    analyzeCallback = cb
  }

  // Computed
  const canGoBack = computed(() => currentMoveIndex.value > 0)
  const canGoForward = computed(() => {
    if (isInVariation.value) return false
    return currentMoveIndex.value < moveHistory.value.length
  })

  // Methods
  function goToMove(index) {
    if (index < 0 || index > fenHistory.value.length - 1) return

    // If going back while in variation, check if we're going back to game position
    if (isInVariation.value && index <= savedMoveIndex.value) {
      exitVariation()
      goToMove(index)
      return
    }

    currentMoveIndex.value = index
    currentFen.value = fenHistory.value[index]
    chess.load(currentFen.value)

    if (analyzeCallback) {
      analyzeCallback()
    }
  }

  function goToStart() {
    if (isInVariation.value) {
      exitVariation()
    }
    goToMove(0)
  }

  function goBack() {
    if (currentMoveIndex.value > 0) {
      goToMove(currentMoveIndex.value - 1)
    }
  }

  function goForward() {
    if (canGoForward.value) {
      goToMove(currentMoveIndex.value + 1)
    }
  }

  function goToEnd() {
    if (isInVariation.value) {
      exitVariation()
    }
    goToMove(moveHistory.value.length)
  }

  function startVariation() {
    if (isInVariation.value) return

    isInVariation.value = true
    variationStartIndex.value = currentMoveIndex.value
    savedMoveHistory.value = [...moveHistory.value]
    savedFenHistory.value = [...fenHistory.value]
    savedMoveIndex.value = currentMoveIndex.value

    // Truncate history to current position for variation
    moveHistory.value = moveHistory.value.slice(0, currentMoveIndex.value)
    fenHistory.value = fenHistory.value.slice(0, currentMoveIndex.value + 1)
  }

  function exitVariation() {
    if (!isInVariation.value) return

    // Restore original game history
    moveHistory.value = savedMoveHistory.value
    fenHistory.value = savedFenHistory.value
    currentMoveIndex.value = savedMoveIndex.value
    currentFen.value = fenHistory.value[currentMoveIndex.value]
    chess.load(currentFen.value)

    isInVariation.value = false
    variationStartIndex.value = 0
    savedMoveHistory.value = []
    savedFenHistory.value = []
    savedMoveIndex.value = 0

    if (analyzeCallback) {
      analyzeCallback()
    }
  }

  function handleKeyPress(event) {
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') return
    if (moveHistory.value.length === 0) return

    switch (event.key) {
      case 'ArrowLeft':
        event.preventDefault()
        goBack()
        break
      case 'ArrowRight':
        event.preventDefault()
        goForward()
        break
      case 'Home':
        event.preventDefault()
        goToStart()
        break
      case 'End':
        event.preventDefault()
        goToEnd()
        break
    }
  }

  onMounted(() => {
    window.addEventListener('keydown', handleKeyPress)
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', handleKeyPress)
  })

  return {
    currentMoveIndex,
    isInVariation,
    variationStartIndex,
    savedMoveHistory,
    savedFenHistory,
    savedMoveIndex,
    canGoBack,
    canGoForward,
    setAnalyzeCallback,
    goToMove,
    goToStart,
    goBack,
    goForward,
    goToEnd,
    startVariation,
    exitVariation,
    handleKeyPress
  }
}
