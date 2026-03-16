<template>
  <div class="game-container">
    <div class="game-header">
      <div class="game-info">
        <span class="connection-status" :class="connectionStatusClass">
          {{ translatedConnectionStatus }}
        </span>
        <span class="game-status">{{ gameStatus }}</span>
        <button
          v-if="isGameOver"
          @click="toggleAnalysisMode"
          class="btn"
          :class="analysisMode ? 'btn-active' : 'btn-secondary'"
        >
          {{ analysisMode ? $t('game.exitAnalysis') : $t('game.analyze') }}
        </button>
      </div>
    </div>

    <!-- Game Over Notification -->
    <div v-if="isGameOver && !analysisMode" class="game-over-banner">
      <div class="game-over-content">
        <span class="game-over-icon">{{ gameOverIcon }}</span>
        <span class="game-over-text">{{ gameOverMessage }}</span>
        <button @click="toggleAnalysisMode" class="btn btn-primary btn-small">
          {{ $t('game.analyzeGame') }}
        </button>
      </div>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <div class="game-board" :class="{ 'with-eval-bar': analysisMode }">
      <!-- Evaluation Bar (left of board) -->
      <EvaluationBar
        v-if="analysisMode"
        :score="currentEvaluation.score"
        :isMate="currentEvaluation.isMate"
        :mateIn="currentEvaluation.mateIn"
        class="eval-bar"
      />

      <div class="board-section">
        <!-- Horizontal evaluation bar for mobile -->
        <EvaluationBar
          v-if="analysisMode"
          :score="currentEvaluation.score"
          :isMate="currentEvaluation.isMate"
          :mateIn="currentEvaluation.mateIn"
          :horizontal="true"
          class="eval-bar-mobile"
        />

        <div v-if="hasTimeControl && !analysisMode" class="player-section">
          <GameTimer
            :timeSeconds="blackTimeRemaining"
            :isActive="chess.turn() === 'b' && gameResult === 1"
            :label="blackPlayerLabel"
          />
        </div>
        <PlayerInfo
          v-else
          :label="blackPlayerLabel"
          :fen="currentFen"
          playerColor="black"
          :isActive="chess.turn() === 'b' && gameResult === 1"
        />

        <div class="chessboard-container">
          <div class="board-wrapper">
            <TheChessboard
              :board-config="boardConfig"
              @board-created="onBoardCreated"
              @move="onMove"
            />
          </div>
        </div>

        <div v-if="hasTimeControl && !analysisMode" class="player-section">
          <GameTimer
            :timeSeconds="whiteTimeRemaining"
            :isActive="chess.turn() === 'w' && gameResult === 1"
            :label="whitePlayerLabel"
          />
        </div>
        <PlayerInfo
          v-else
          :label="whitePlayerLabel"
          :fen="currentFen"
          playerColor="white"
          :isActive="chess.turn() === 'w' && gameResult === 1"
        />
      </div>

      <div class="game-sidebar">
        <div v-if="!analysisMode" class="game-details">
          <h3>{{ $t('game.gameInfo') }}</h3>
          <p><strong>{{ $t('game.id') }}</strong> {{ gameId.substring(0, 8) }}</p>
          <p><strong>{{ $t('game.turn') }}</strong> {{ chess.turn() === 'w' ? $t('game.white') : $t('game.black') }}</p>
        </div>

        <!-- Top 3 moves (analysis mode) -->
        <div v-if="analysisMode" class="top-moves">
          <h3>
            {{ $t('game.bestMoves') }}
            <span v-if="currentAnalysisDepth > 0" class="analysis-depth">
              {{ analyzing ? `(depth ${currentAnalysisDepth}...)` : `(depth ${currentAnalysisDepth})` }}
            </span>
          </h3>
          <div v-if="analyzing && topMoves.length === 0" class="moves-suggestions">
            <!-- Skeleton loader for 3 moves -->
            <div v-for="i in 3" :key="`skeleton-${i}`" class="suggestion-item skeleton-item">
              <span class="skeleton skeleton-rank"></span>
              <span class="skeleton skeleton-move"></span>
              <span class="skeleton skeleton-eval"></span>
            </div>
          </div>
          <div v-else-if="topMoves.length > 0" class="moves-suggestions">
            <div
              v-for="(line, index) in topMoves"
              :key="index"
              class="suggestion-item"
              :class="{ 'best-move': index === 0, 'analyzing-update': analyzing }"
              @click="playMove(line.move)"
            >
              <span class="move-rank">{{ index + 1 }}.</span>
              <span class="move-san">{{ line.moveSan }}</span>
              <span class="move-eval" :class="getEvalClass(line)">
                {{ formatEval(line) }}
              </span>
            </div>
          </div>
          <div v-else class="no-analysis">
            {{ $t('game.navigateToSeeAnalysis') }}
          </div>
        </div>

        <div class="move-history">
          <h3>{{ $t('game.moveHistory') }}</h3>
          <div class="moves-list">
            <div
              v-for="(move, index) in moveHistory"
              :key="index"
              class="move-item"
              :class="{ 'current-move': index === currentMoveIndex - 1, 'variation-move': isInVariation && index >= variationStartIndex }"
              @click="goToMove(index + 1)"
            >
              {{ Math.floor(index / 2) + 1 }}{{ index % 2 === 0 ? '.' : '...' }} {{ move }}
            </div>
          </div>
        </div>

        <!-- Navigation buttons -->
        <div v-if="moveHistory.length > 0" class="navigation-controls">
          <button
            @click="goToStart"
            class="nav-btn"
            :disabled="!canGoBack"
            title="Go to start"
          >
            ⏮
          </button>
          <button
            @click="goBack"
            class="nav-btn"
            :disabled="!canGoBack"
            title="Previous move"
          >
            ◀
          </button>
          <button
            @click="goForward"
            class="nav-btn"
            :disabled="!canGoForward"
            title="Next move"
          >
            ▶
          </button>
          <button
            @click="goToEnd"
            class="nav-btn"
            :disabled="!canGoForward"
            title="Go to end"
          >
            ⏭
          </button>
        </div>

        <div v-if="isInVariation" class="variation-indicator">
          <span>{{ $t('game.inVariation') }}</span>
          <button @click="exitVariation" class="btn btn-small">{{ $t('game.exitToGame') }}</button>
        </div>

        <div class="game-controls">
          <button @click="goBackToGames" class="btn btn-secondary">
            {{ $t('game.backToGames') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { TheChessboard } from 'vue3-chessboard'
import 'vue3-chessboard/style.css'
import { gameService } from '../services/gameService'
import { useI18n } from 'vue-i18n'
import { onMounted, onUnmounted, getCurrentInstance } from 'vue'
import GameTimer from '../components/GameTimer.vue'
import EvaluationBar from '../components/EvaluationBar.vue'
import PlayerInfo from '../components/PlayerInfo.vue'
import { useGameCore } from '../composables/useGameCore'
import { useGameTimer } from '../composables/useGameTimer'
import { useMoveNavigation } from '../composables/useMoveNavigation'
import { useGameAnalysis } from '../composables/useGameAnalysis'
import { useBoardConfig } from '../composables/useBoardConfig'
import { useGameConnection } from '../composables/useGameConnection'

export default {
  name: 'GameView',
  components: {
    TheChessboard,
    GameTimer,
    EvaluationBar,
    PlayerInfo
  },
  props: {
    gameId: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const { t } = useI18n()
    const instance = getCurrentInstance()

    // 1. Core game state
    const core = useGameCore(props.gameId, { t })

    // 2. Timer
    const timer = useGameTimer({
      chess: core.chess,
      hasTimeControl: core.hasTimeControl,
      gameResult: core.gameResult,
      t
    })

    // 3. Navigation
    const navigation = useMoveNavigation({
      chess: core.chess,
      currentFen: core.currentFen,
      moveHistory: core.moveHistory,
      fenHistory: core.fenHistory
    })

    // 4. Analysis
    const analysis = useGameAnalysis({ chess: core.chess })

    // Wire analysis ↔ navigation (late binding)
    analysis.setNavigationRefs({
      currentMoveIndex: navigation.currentMoveIndex,
      isInVariation: navigation.isInVariation,
      startVariation: navigation.startVariation,
      exitVariation: navigation.exitVariation
    })

    // Navigation calls analyzeCurrentPosition when in analysis mode
    navigation.setAnalyzeCallback(() => {
      if (analysis.analysisMode.value) {
        analysis.analyzeCurrentPosition()
      }
    })

    // Wire timer ↔ analysis mode ref
    timer.setAnalysisModeRef(analysis.analysisMode)

    // 5. Board config (watchers included)
    const board = useBoardConfig({
      game: core.game,
      chess: core.chess,
      currentFen: core.currentFen,
      moveHistory: core.moveHistory,
      currentMoveIndex: navigation.currentMoveIndex,
      isPlayerTurn: null, // computed inside useBoardConfig
      analysisMode: analysis.analysisMode,
      boardAPI: core.boardAPI,
      t
    })

    // 6. Connection
    const connection = useGameConnection(props.gameId, {
      game: core.game,
      chess: core.chess,
      currentFen: core.currentFen,
      moveHistory: core.moveHistory,
      fenHistory: core.fenHistory,
      currentMoveIndex: navigation.currentMoveIndex,
      error: core.error,
      stopTimer: timer.stopTimer,
      t
    })

    // Wire timer refs to connection so it can update times on move received
    connection.setTimerRefs({
      whiteTimeRemaining: timer.whiteTimeRemaining,
      blackTimeRemaining: timer.blackTimeRemaining
    })

    // --- Orchestrator: onMove ---
    function onMove(move) {
      core.error.value = ''

      if (!analysis.analysisMode.value && !board.isPlayerTurn.value) {
        core.error.value = t('game.notYourTurn')
        return
      }

      try {
        const chessMove = core.chess.move({
          from: move.from,
          to: move.to,
          promotion: 'q'
        })

        if (chessMove === null) {
          core.error.value = t('game.invalidMove')
          return
        }

        if (analysis.analysisMode.value) {
          if (!navigation.isInVariation.value) {
            navigation.startVariation()
          }

          core.moveHistory.value.push(chessMove.san)
          core.fenHistory.value.push(core.chess.fen())
          navigation.currentMoveIndex.value = core.moveHistory.value.length
          core.currentFen.value = core.chess.fen()

          analysis.analyzeCurrentPosition()
          return
        }

        // Normal game mode
        core.moveHistory.value.push(chessMove.san)
        core.fenHistory.value.push(core.chess.fen())
        navigation.currentMoveIndex.value = core.moveHistory.value.length
        core.currentFen.value = core.chess.fen()

        const gameEndInfo = {
          isCheckmate: core.chess.isCheckmate(),
          isStalemate: core.chess.isStalemate(),
          isDraw: core.chess.isDraw() && !core.chess.isStalemate()
        }

        gameService.makeMove(
          props.gameId,
          chessMove.san,
          core.chess.fen(),
          gameEndInfo
        ).then(result => {
          if (!result.success) {
            core.chess.undo()
            core.moveHistory.value.pop()
            core.fenHistory.value.pop()
            navigation.currentMoveIndex.value = core.moveHistory.value.length
            core.currentFen.value = core.chess.fen()
            core.error.value = result.error
          }
        }).catch(err => {
          console.error('Backend error:', err)
          core.error.value = t('game.failedToMakeMove')
        })
      } catch (err) {
        console.error('Move error:', err)
        core.error.value = t('game.failedToMakeMove')
      }
    }

    // --- playMove wrapper for analysis ---
    function playMove(uciMove) {
      analysis.playMove(uciMove, onMove)
    }

    // --- Android bridge ---
    function setupAndroidBridge() {
      window.isInAnalysisMode = () => analysis.analysisMode.value
      window.exitAnalysisMode = () => {
        if (analysis.analysisMode.value) {
          analysis.toggleAnalysisMode()
          return true
        }
        return false
      }
    }

    // --- goBackToGames ---
    function goBackToGames() {
      instance.proxy.$router.push('/games')
    }

    // --- Lifecycle ---
    onMounted(async () => {
      console.log('GameView mounted, gameId:', props.gameId)
      await core.initializeGame()

      // After loading, sync navigation index and timer
      navigation.currentMoveIndex.value = core.moveHistory.value.length
      timer.setTimes(
        core.game.value?.whiteTimeRemainingSeconds || 0,
        core.game.value?.blackTimeRemainingSeconds || 0
      )

      console.log('GameView initializeGame done, game:', core.game.value?.gameResult)
      await connection.setupRealtimeConnection()
      timer.startTimer(core.error)
      setupAndroidBridge()
    })

    onUnmounted(async () => {
      await connection.cleanupRealtimeConnection()
      timer.stopTimer()
    })

    // Return everything the template needs (same property names)
    return {
      // Core
      game: core.game,
      chess: core.chess,
      error: core.error,
      loading: core.loading,
      currentFen: core.currentFen,
      moveHistory: core.moveHistory,
      fenHistory: core.fenHistory,
      gameResult: core.gameResult,
      isGameOver: core.isGameOver,
      hasTimeControl: core.hasTimeControl,
      onBoardCreated: core.onBoardCreated,

      // Timer
      whiteTimeRemaining: timer.whiteTimeRemaining,
      blackTimeRemaining: timer.blackTimeRemaining,

      // Navigation
      currentMoveIndex: navigation.currentMoveIndex,
      isInVariation: navigation.isInVariation,
      variationStartIndex: navigation.variationStartIndex,
      canGoBack: navigation.canGoBack,
      canGoForward: navigation.canGoForward,
      goToMove: navigation.goToMove,
      goToStart: navigation.goToStart,
      goBack: navigation.goBack,
      goForward: navigation.goForward,
      goToEnd: navigation.goToEnd,
      exitVariation: navigation.exitVariation,

      // Analysis
      analysisMode: analysis.analysisMode,
      analyzing: analysis.analyzing,
      topMoves: analysis.topMoves,
      currentEvaluation: analysis.currentEvaluation,
      currentAnalysisDepth: analysis.currentAnalysisDepth,
      toggleAnalysisMode: analysis.toggleAnalysisMode,
      formatEval: analysis.formatEval,
      getEvalClass: analysis.getEvalClass,

      // Board config
      boardConfig: board.boardConfig,
      isPlayerTurn: board.isPlayerTurn,
      gameStatus: board.gameStatus,
      currentPlayer: board.currentPlayer,
      blackPlayerLabel: board.blackPlayerLabel,
      whitePlayerLabel: board.whitePlayerLabel,
      gameOverMessage: board.gameOverMessage,
      gameOverIcon: board.gameOverIcon,

      // Connection
      connectionStatus: connection.connectionStatus,
      translatedConnectionStatus: connection.translatedConnectionStatus,
      connectionStatusClass: connection.connectionStatusClass,

      // Orchestrators
      onMove,
      playMove,
      goBackToGames
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
  justify-content: flex-start;
  align-items: center;
  margin-bottom: 0.5rem;
  padding-bottom: 0.4rem;
  border-bottom: 1px solid rgba(197, 212, 255, 0.2);
}

.game-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.game-info .btn {
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
}

.btn-active {
  background: var(--cosmic-action-primary, #7A4CE0);
  color: white;
}

.game-status {
  font-weight: 600;
  color: var(--cosmic-action-primary, #7A4CE0);
  font-size: 0.9rem;
  font-family: var(--font-body, 'Inter', sans-serif);
  text-shadow: 0 0 10px rgba(122, 76, 224, 0.4);
}

/* Game Over Banner */
.game-over-banner {
  background: linear-gradient(
    135deg,
    rgba(122, 76, 224, 0.2) 0%,
    rgba(138, 90, 173, 0.15) 100%
  );
  border: 1px solid rgba(122, 76, 224, 0.4);
  border-radius: var(--card-radius, 12px);
  padding: 1rem;
  margin-bottom: 1rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 20px rgba(122, 76, 224, 0.3);
}

.game-over-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.game-over-icon {
  font-size: 2rem;
}

.game-over-text {
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
}

.btn-primary {
  background: var(--cosmic-action-primary, #7A4CE0);
  color: white;
  border: none;
}

.btn-primary:hover {
  background: #6a3cd0;
}

.game-board {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 1rem;
  align-items: start;
}

.game-board.with-eval-bar {
  grid-template-columns: 40px 1fr 300px;
}

.eval-bar {
  justify-self: center;
}

.eval-bar-mobile {
  display: none;
}

.board-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-width: 550px;
  width: 100%;
}

@media (max-width: 1200px) {
  .game-board,
  .game-board.with-eval-bar {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .board-section {
    margin: 0 auto;
  }

  .eval-bar {
    display: none;
  }

  .eval-bar-mobile {
    display: flex;
  }
}

@media (max-width: 768px) {
  .game-header {
    margin-bottom: 0.4rem;
    padding-bottom: 0.3rem;
  }

  .game-info {
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .connection-status {
    font-size: 0.7rem;
    padding: 0.25rem 0.5rem;
  }

  .game-status {
    font-size: 0.8rem;
  }
}

.chessboard-container {
  max-width: 550px;
  margin: 0 auto;
  min-width: 280px;
  box-sizing: border-box;
}

.board-wrapper {
  width: 100%;
  max-width: 100%;
  margin: 0 auto;
  aspect-ratio: 1 / 1;
  position: relative;
}

.game-sidebar {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: var(--card-radius, 12px);
  padding: 1rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  height: fit-content;
}

.game-details,
.move-history,
.game-controls,
.top-moves,
.navigation-controls {
  margin-bottom: 1rem;
}

.game-details:last-child,
.move-history:last-child,
.game-controls:last-child {
  margin-bottom: 0;
}

.game-details h3,
.move-history h3,
.top-moves h3 {
  margin: 0 0 0.5rem 0;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-size: 1rem;
  font-weight: 600;
  border-bottom: 1px solid rgba(197, 212, 255, 0.15);
  padding-bottom: 0.4rem;
}

.analysis-depth {
  font-size: 0.75rem;
  color: var(--cosmic-stars, #C5D4FF);
  font-weight: 400;
  opacity: 0.8;
  font-family: var(--font-body, 'Inter', sans-serif);
  margin-left: 0.5rem;
}

.game-details p {
  margin: 0.3rem 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-family: var(--font-body, 'Inter', sans-serif);
  font-size: 0.9rem;
}

.game-details p strong {
  color: var(--cosmic-figures, #F2F2F2);
  font-weight: 600;
}

/* Top moves styles */
.top-moves {
  margin-bottom: 1rem;
}

.analyzing-indicator {
  color: var(--cosmic-stars, #C5D4FF);
  font-style: italic;
  padding: 0.5rem;
  text-align: center;
}

.moves-suggestions {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  min-height: 120px;
}

.suggestion-item {
  display: flex;
  align-items: center;
  padding: 0.4rem 0.5rem;
  background: rgba(27, 35, 64, 0.4);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.suggestion-item:hover {
  background: rgba(122, 76, 224, 0.3);
}

.suggestion-item.best-move {
  background: rgba(122, 76, 224, 0.2);
  border: 1px solid rgba(122, 76, 224, 0.4);
}

.suggestion-item.analyzing-update {
  animation: pulse-update 1.5s ease-in-out infinite;
}

@keyframes pulse-update {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
}

.move-rank {
  color: var(--cosmic-stars, #C5D4FF);
  font-weight: 600;
  margin-right: 0.5rem;
  font-size: 0.85rem;
}

.move-san {
  flex: 1;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: 'Courier New', monospace;
  font-weight: 600;
}

.move-eval {
  font-family: 'Courier New', monospace;
  font-size: 0.85rem;
  font-weight: 600;
}

.eval-winning {
  color: #4ade80;
}

.eval-losing {
  color: #f87171;
}

.eval-equal {
  color: var(--cosmic-stars, #C5D4FF);
}

.no-analysis {
  color: var(--cosmic-stars, #C5D4FF);
  font-style: italic;
  font-size: 0.85rem;
  text-align: center;
  padding: 0.5rem;
  min-height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Skeleton loader styles */
.skeleton-item {
  pointer-events: none;
  cursor: default;
}

.skeleton {
  background: linear-gradient(
    90deg,
    rgba(197, 212, 255, 0.1) 25%,
    rgba(197, 212, 255, 0.2) 50%,
    rgba(197, 212, 255, 0.1) 75%
  );
  background-size: 200% 100%;
  animation: skeleton-loading 1.5s ease-in-out infinite;
  border-radius: 4px;
  display: inline-block;
}

@keyframes skeleton-loading {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

.skeleton-rank {
  width: 20px;
  height: 14px;
  margin-right: 0.5rem;
}

.skeleton-move {
  flex: 1;
  height: 14px;
  max-width: 60px;
}

.skeleton-eval {
  width: 40px;
  height: 14px;
}

/* Move history styles */
.moves-list {
  max-height: 50vh;
  overflow-y: auto;
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: 8px;
  padding: 0.4rem;
  background: rgba(10, 13, 20, 0.4);
}

.moves-list::-webkit-scrollbar {
  width: 8px;
}

.moves-list::-webkit-scrollbar-track {
  background: rgba(27, 35, 64, 0.3);
  border-radius: 4px;
}

.moves-list::-webkit-scrollbar-thumb {
  background: rgba(122, 76, 224, 0.4);
  border-radius: 4px;
}

.moves-list::-webkit-scrollbar-thumb:hover {
  background: rgba(122, 76, 224, 0.6);
}

.move-item {
  padding: 0.3rem 0.4rem;
  font-family: 'Courier New', monospace;
  font-size: 0.85rem;
  color: var(--cosmic-stars, #C5D4FF);
  border-radius: 4px;
  transition: background var(--transition-smooth, 200ms);
  cursor: pointer;
}

.move-item:nth-child(even) {
  background-color: rgba(27, 35, 64, 0.3);
}

.move-item:hover {
  background-color: rgba(122, 76, 224, 0.2);
  color: var(--cosmic-figures, #F2F2F2);
}

.move-item.current-move {
  background-color: rgba(122, 76, 224, 0.4);
  color: var(--cosmic-figures, #F2F2F2);
}

.move-item.variation-move {
  color: #fbbf24;
  font-style: italic;
}

/* Navigation controls */
.navigation-controls {
  display: flex;
  justify-content: center;
  gap: 0.5rem;
}

.nav-btn {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  border: 1px solid rgba(197, 212, 255, 0.2);
  background: rgba(27, 35, 64, 0.6);
  color: var(--cosmic-stars, #C5D4FF);
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.nav-btn:hover:not(:disabled) {
  background: rgba(122, 76, 224, 0.3);
  border-color: rgba(122, 76, 224, 0.5);
}

.nav-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

/* Variation indicator */
.variation-indicator {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem;
  background: rgba(251, 191, 36, 0.15);
  border: 1px solid rgba(251, 191, 36, 0.3);
  border-radius: 8px;
  margin-bottom: 1rem;
}

.variation-indicator span {
  color: #fbbf24;
  font-size: 0.85rem;
  font-weight: 500;
}

.btn-small {
  padding: 0.25rem 0.5rem;
  font-size: 0.75rem;
}

.game-controls .btn {
  width: 100%;
}

.player-section {
  width: 100%;
}

.connection-status {
  font-size: 0.9rem;
  padding: 0.4rem 0.9rem;
  border-radius: 8px;
  font-weight: 500;
  font-family: var(--font-body, 'Inter', sans-serif);
  backdrop-filter: blur(10px);
}

.status-connected {
  background: var(--status-success-bg, rgba(122, 76, 224, 0.15));
  color: var(--status-success-text, #C5D4FF);
  border: 1px solid rgba(122, 76, 224, 0.3);
  box-shadow: 0 0 12px rgba(122, 76, 224, 0.2);
}

.status-connecting {
  background: var(--status-warning-bg, rgba(138, 90, 173, 0.15));
  color: var(--status-warning-text, #E6E0FF);
  border: 1px solid rgba(138, 90, 173, 0.3);
}

.status-disconnected {
  background: var(--status-error-bg, rgba(101, 61, 137, 0.15));
  color: var(--status-error-text, #C5D4FF);
  border: 1px solid rgba(101, 61, 137, 0.3);
}
</style>

<style>
/* Override vue3-chessboard styles with cosmic theme */
.cg-wrap {
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 0 40px rgba(122, 76, 224, 0.2);
}

cg-board square.light {
  background-color: var(--chess-square-light, #10131C) !important;
}

cg-board square.dark {
  background-color: var(--chess-square-dark, #1E2433) !important;
}

cg-board square.move-dest {
  background: radial-gradient(
    circle,
    rgba(122, 76, 224, 0.4) 25%,
    rgba(122, 76, 224, 0.2) 50%,
    transparent 80%
  ) !important;
}

cg-board square.selected {
  background-color: rgba(122, 76, 224, 0.3) !important;
}

cg-board square.last-move {
  background-color: rgba(138, 90, 173, 0.25) !important;
}

cg-board square.move-dest::before {
  background: radial-gradient(
    circle,
    rgba(197, 212, 255, 0.8) 0%,
    rgba(122, 76, 224, 0.6) 50%,
    transparent 100%
  ) !important;
  box-shadow: 0 0 8px rgba(122, 76, 224, 0.6);
}

cg-board square:hover {
  background-color: rgba(122, 76, 224, 0.15) !important;
}

cg-board square.check {
  background: radial-gradient(
    circle,
    rgba(101, 61, 137, 0.6) 0%,
    rgba(101, 61, 137, 0.3) 50%,
    transparent 100%
  ) !important;
  box-shadow: 0 0 15px rgba(101, 61, 137, 0.5);
}

coords {
  color: var(--cosmic-stars, #C5D4FF) !important;
  opacity: 0.7;
  font-family: var(--font-body, 'Inter', sans-serif) !important;
  font-size: 0.8rem;
}

@media (max-width: 768px) {
  .chessboard-container {
    max-width: 100vw;
    width: 100%;
    margin: 0;
    box-sizing: border-box;
  }

  .board-wrapper {
    max-width: 100%;
  }

  .board-section {
    gap: 0.4rem;
    max-width: 100%;
  }

  coords {
    font-size: 0.6rem !important;
  }
}

@media (max-width: 480px) {
  .board-section {
    gap: 0.3rem;
  }
}

@supports not (aspect-ratio: 1 / 1) {
  .board-wrapper {
    height: 0;
    padding-bottom: 100%;
  }

  .board-wrapper > * {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
  }
}

.cg-wrap {
  max-width: 100%;
  overflow: visible !important;
}

cg-board {
  display: block !important;
}

cg-board square {
  display: inline-block !important;
}
</style>
