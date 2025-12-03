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
              @board-created="(api) => (boardAPI = api)"
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
          <h3>{{ $t('game.bestMoves') }}</h3>
          <div v-if="analyzing" class="moves-suggestions">
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
              :class="{ 'best-move': index === 0 }"
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

        <!-- Navigation buttons (analysis mode) -->
        <div v-if="analysisMode" class="navigation-controls">
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
import { Chess } from 'chess.js'
import { TheChessboard } from 'vue3-chessboard'
import 'vue3-chessboard/style.css'
import { gameService } from '../services/gameService'
import { authService } from '../services/authService'
import { gameConnectionService } from '../services/gameConnectionService'
import { markRaw } from 'vue'
import GameTimer from '../components/GameTimer.vue'
import EvaluationBar from '../components/EvaluationBar.vue'
import PlayerInfo from '../components/PlayerInfo.vue'

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
  data() {
    return {
      game: null,
      chess: markRaw(new Chess()),
      boardAPI: null,
      error: '',
      analyzing: false,
      moveHistory: [],
      loading: true,
      currentFen: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
      connectionStatus: '',
      whiteTimeRemaining: 0,
      blackTimeRemaining: 0,
      timerInterval: null,

      // Analysis mode state
      analysisMode: false,
      currentMoveIndex: 0,      // Current position in move history (0 = start)
      fenHistory: [],           // FEN for each position
      topMoves: [],             // Top 3 moves from engine
      currentEvaluation: {
        score: 0,
        isMate: false,
        mateIn: null
      },

      // Variation tracking
      isInVariation: false,
      variationStartIndex: 0,
      variationMoves: [],
      savedMoveHistory: [],
      savedFenHistory: [],
      savedMoveIndex: 0
    }
  },
  computed: {
    boardConfig() {
      const dests = new Map()

      // Create temp chess from currentFen for reactivity (this.chess is markRaw/non-reactive)
      const tempChess = new Chess(this.currentFen)

      // In analysis mode, always allow moves for exploration
      if (this.analysisMode) {
        const moves = tempChess.moves({ verbose: true })
        moves.forEach(move => {
          if (!dests.has(move.from)) {
            dests.set(move.from, [])
          }
          dests.get(move.from).push(move.to)
        })
      } else if (this.isPlayerTurn) {
        // Only allow moves if it's the current player's turn
        const moves = tempChess.moves({ verbose: true })
        moves.forEach(move => {
          if (!dests.has(move.from)) {
            dests.set(move.from, [])
          }
          dests.get(move.from).push(move.to)
        })
      }
      // If not player's turn, dests stays empty - no moves allowed

      // Determine player's color based on whether they're the creator
      const userId = authService.getUserId()
      const isWhite = this.game?.whitePlayerId === userId

      // Set movable color to player's color (they can only interact with their pieces)
      // In analysis mode, allow both colors
      let movableColor
      if (this.analysisMode) {
        movableColor = 'both'
      } else {
        movableColor = isWhite ? 'white' : 'black'
      }

      return {
        fen: this.currentFen,
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
    },

    gameStatus() {
      // Extract current turn from FEN for reactivity
      const fenParts = this.currentFen.split(' ')
      const currentTurn = fenParts[1] || 'w'
      const currentColor = currentTurn === 'w' ? this.$t('game.white') : this.$t('game.black')
      const oppositeColor = currentTurn === 'w' ? this.$t('game.black') : this.$t('game.white')

      if (this.chess.isGameOver()) {
        if (this.chess.isCheckmate()) {
          return this.$t('game.checkmate', { color: oppositeColor })
        } else if (this.chess.isDraw()) {
          return this.$t('game.draw')
        }
      } else if (this.chess.inCheck()) {
        return this.$t('game.inCheck', { color: currentColor })
      }

      if (this.analysisMode) {
        return this.$t('game.analysisMode', { current: this.currentMoveIndex, total: this.moveHistory.length })
      }

      return this.$t('game.toMove', { color: currentColor })
    },

    currentPlayer() {
      return this.chess.turn() === 'w' ? 'White' : 'Black'
    },

    isPlayerTurn() {
      const userId = authService.getUserId()

      // Extract current turn from FEN (reactive) instead of chess.turn() (non-reactive)
      const fenParts = this.currentFen.split(' ')
      const currentTurn = fenParts[1] || 'w'

      if (!this.game || !userId) return false

      // Don't allow moves if game is not in progress
      // gameResult: 0=WaitJoin, 1=InProgress, 2+=GameOver
      if (this.game.gameResult !== 1) return false

      // Don't allow moves if second player hasn't joined yet
      // Check for empty GUID or falsy value
      const emptyGuid = '00000000-0000-0000-0000-000000000000'
      if (!this.game.blackPlayerId || this.game.blackPlayerId === emptyGuid) return false

      const isWhite = this.game.whitePlayerId === userId

      return (isWhite && currentTurn === 'w') || (!isWhite && currentTurn === 'b')
    },

    translatedConnectionStatus() {
      const statusMap = {
        'Connected': this.$t('game.connected'),
        'Disconnected': this.$t('game.disconnected'),
        'Connecting': this.$t('game.connecting'),
        'Reconnecting': this.$t('game.reconnecting')
      }
      return statusMap[this.connectionStatus] || this.connectionStatus
    },

    connectionStatusClass() {
      return {
        'status-connected': this.connectionStatus === 'Connected',
        'status-connecting': this.connectionStatus === 'Connecting' || this.connectionStatus === 'Reconnecting',
        'status-disconnected': this.connectionStatus === 'Disconnected'
      }
    },

    hasTimeControl() {
      return this.game && this.game.timeControl !== 0
    },

    gameResult() {
      return this.game?.gameResult || 0
    },

    blackPlayerLabel() {
      // Check if black player is a bot (special GUID)
      const botPlayerId = '00000000-0000-0000-0000-000000000001'
      if (this.game?.blackPlayerId === botPlayerId) {
        return this.$t('game.bot')
      }
      return this.game?.blackPlayerUsername || this.$t('game.waiting')
    },

    whitePlayerLabel() {
      // Check if white player is a bot (special GUID)
      const botPlayerId = '00000000-0000-0000-0000-000000000001'
      if (this.game?.whitePlayerId === botPlayerId) {
        return this.$t('game.bot')
      }
      return this.game?.whitePlayerUsername || this.$t('game.white')
    },

    // Game over detection
    isGameOver() {
      // GameResult: 0=WaitJoin, 1=InProgress, 2=WhiteWins, 3=BlackWins, 4=Draw
      return this.gameResult >= 2 || this.chess.isGameOver()
    },

    gameOverMessage() {
      if (this.chess.isCheckmate()) {
        return this.chess.turn() === 'w' ? this.$t('game.blackWinsByCheckmate') : this.$t('game.blackWinsByCheckmate')
      }
      if (this.chess.isStalemate()) {
        return this.$t('game.drawByStalemate')
      }
      if (this.chess.isThreefoldRepetition()) {
        return this.$t('game.drawByRepetition')
      }
      if (this.chess.isInsufficientMaterial()) {
        return this.$t('game.drawByInsufficientMaterial')
      }
      if (this.chess.isDraw()) {
        return this.$t('game.draw')
      }

      // From backend game result
      switch (this.gameResult) {
        case 2: return this.$t('game.whiteWins')
        case 3: return this.$t('game.blackWins')
        case 4: return this.$t('game.draw')
        default: return this.$t('game.gameOver')
      }
    },

    gameOverIcon() {
      if (this.chess.isCheckmate()) {
        return this.chess.turn() === 'w' ? '♚' : '♔'
      }
      if (this.gameResult === 2) return '♔'
      if (this.gameResult === 3) return '♚'
      if (this.gameResult === 4 || this.chess.isDraw()) return '½'
      return '🏁'
    },

    // Navigation computed properties
    canGoBack() {
      return this.currentMoveIndex > 0
    },

    canGoForward() {
      // Can only go forward if not in variation and not at end
      if (this.isInVariation) {
        return false
      }
      return this.currentMoveIndex < this.moveHistory.length
    }
  },

  async mounted() {
    await this.initializeGame()
    await this.setupRealtimeConnection()
    this.startTimer()
    // Add keyboard navigation support
    window.addEventListener('keydown', this.handleKeyPress)
    // Expose functions for Android bridge
    this.setupAndroidBridge()
  },

  async unmounted() {
    await this.cleanupRealtimeConnection()
    this.stopTimer()
    // Remove keyboard navigation support
    window.removeEventListener('keydown', this.handleKeyPress)
  },

  watch: {
    currentFen(newFen) {
      if (this.boardAPI) {
        this.boardAPI.setPosition(newFen)

        // Also update movable configuration to reflect new turn
        // Use underlying chessground API
        const config = this.boardConfig
        if (this.boardAPI.board && this.boardAPI.board.set) {
          this.boardAPI.board.set({
            movable: config.movable
          })
        }
      }
    },
    // Update board when game data changes (e.g., after loading or when player joins)
    'game.gameResult'() {
      if (this.boardAPI && this.boardAPI.board && this.boardAPI.board.set) {
        const config = this.boardConfig
        this.boardAPI.board.set({
          movable: config.movable
        })
      }
    },
    'game.blackPlayerId'() {
      if (this.boardAPI && this.boardAPI.board && this.boardAPI.board.set) {
        const config = this.boardConfig
        this.boardAPI.board.set({
          movable: config.movable
        })
      }
    }
  },

  methods: {
    async initializeGame() {
      try {
        const result = await gameService.getGameById(this.gameId)

        if (result.success && result.game) {
          this.game = result.game

          const fenToLoad = result.game.currentFen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'

          this.chess.load(fenToLoad)
          this.currentFen = fenToLoad

          // Build FEN history from moves
          this.fenHistory = ['rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1']
          const tempChess = new Chess()

          if (result.game.moves && result.game.moves.length > 0) {
            this.moveHistory = result.game.moves.map(m => m.move)

            // Build FEN history
            for (const move of this.moveHistory) {
              tempChess.move(move)
              this.fenHistory.push(tempChess.fen())
            }
          }

          // Set current move index to end
          this.currentMoveIndex = this.moveHistory.length

          this.whiteTimeRemaining = result.game.whiteTimeRemainingSeconds || 0
          this.blackTimeRemaining = result.game.blackTimeRemainingSeconds || 0

          console.log('Game loaded:', this.game)
        } else {
          this.chess.reset()
          this.currentFen = this.chess.fen()
          this.fenHistory = [this.currentFen]
          console.warn('Failed to load game, starting with initial position')
        }

        this.loading = false
      } catch (error) {
        this.error = this.$t('game.failedToInitializeGame')
        console.error('Game initialization error:', error)

        this.chess.reset()
        this.currentFen = this.chess.fen()
        this.fenHistory = [this.currentFen]
        this.loading = false
      }
    },

    async setupRealtimeConnection() {
      try {
        console.log('Setting up real-time connection for game:', this.gameId)

        await gameConnectionService.connect()
        this.connectionStatus = gameConnectionService.getConnectionState()

        await gameConnectionService.joinGame(this.gameId)

        gameConnectionService.on('moveReceived', this.handleMoveReceived)
        gameConnectionService.on('gameStateChanged', this.handleGameStateChanged)
        gameConnectionService.on('playerJoined', this.handlePlayerJoined)
        gameConnectionService.on('playerLeft', this.handlePlayerLeft)

        console.log('Real-time connection established')
      } catch (error) {
        console.error('Failed to setup real-time connection:', error)
        this.error = this.$t('game.failedToConnectToServer')
      }
    },

    async cleanupRealtimeConnection() {
      try {
        console.log('Cleaning up real-time connection')

        gameConnectionService.off('moveReceived', this.handleMoveReceived)
        gameConnectionService.off('gameStateChanged', this.handleGameStateChanged)
        gameConnectionService.off('playerJoined', this.handlePlayerJoined)
        gameConnectionService.off('playerLeft', this.handlePlayerLeft)

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

      if (data.whiteTimeRemainingSeconds !== undefined) {
        this.whiteTimeRemaining = data.whiteTimeRemainingSeconds
      }
      if (data.blackTimeRemainingSeconds !== undefined) {
        this.blackTimeRemaining = data.blackTimeRemainingSeconds
      }

      if (data.userId === currentUserId) {
        return
      }

      try {
        if (data.newFen) {
          this.chess.load(data.newFen)

          if (data.move) {
            this.moveHistory.push(data.move)
            this.fenHistory.push(data.newFen)
            this.currentMoveIndex = this.moveHistory.length
          }
        } else if (data.move) {
          const move = this.chess.move(data.move)
          if (move) {
            this.moveHistory.push(move.san)
            this.fenHistory.push(this.chess.fen())
            this.currentMoveIndex = this.moveHistory.length
          }
        }

        this.currentFen = this.chess.fen()
      } catch (error) {
        console.error('Error applying received move:', error)
        this.error = this.$t('game.failedToApplyMove')
      }
    },

    handleGameStateChanged(data) {
      console.log('Game state changed:', data)

      // Update game result when game ends
      if (data.gameResult !== undefined && this.game) {
        this.game.gameResult = data.gameResult

        // Stop timer when game ends
        if (data.gameResult >= 2) {
          this.stopTimer()
        }
      }
    },

    handlePlayerJoined(data) {
      console.log('Player joined:', data)
      if (this.game) {
        this.game.blackPlayerId = data.playerId
        this.game.blackPlayerUsername = data.username || 'Player'
        this.game.gameResult = 1 // Set to InProgress
        console.log('Game started - gameResult set to InProgress')
      }
    },

    handlePlayerLeft(data) {
      console.log('Player left:', data)
    },

    onMove(move) {
      this.error = ''

      // Don't allow moves if it's not the player's turn (safety check)
      if (!this.analysisMode && !this.isPlayerTurn) {
        this.error = this.$t('game.notYourTurn')
        return
      }

      try {
        const chessMove = this.chess.move({
          from: move.from,
          to: move.to,
          promotion: 'q'
        })

        if (chessMove === null) {
          this.error = this.$t('game.invalidMove')
          return
        }

        // If in analysis mode
        if (this.analysisMode) {
          // If not in variation and not at end of game, start variation
          if (!this.isInVariation && this.currentMoveIndex < this.moveHistory.length) {
            this.startVariation()
          } else if (!this.isInVariation && this.currentMoveIndex === this.moveHistory.length) {
            // At end of game moves, start variation
            this.startVariation()
          }

          // Add move to current history
          this.moveHistory.push(chessMove.san)
          this.fenHistory.push(this.chess.fen())
          this.currentMoveIndex = this.moveHistory.length
          this.currentFen = this.chess.fen()

          // Analyze new position
          this.analyzeCurrentPosition()
          return
        }

        // Normal game mode
        this.moveHistory.push(chessMove.san)
        this.fenHistory.push(this.chess.fen())
        this.currentMoveIndex = this.moveHistory.length
        this.currentFen = this.chess.fen()

        // Check for game end conditions
        const gameEndInfo = {
          isCheckmate: this.chess.isCheckmate(),
          isStalemate: this.chess.isStalemate(),
          isDraw: this.chess.isDraw() && !this.chess.isStalemate()
        }

        // Send move to backend
        gameService.makeMove(
          this.gameId,
          chessMove.san,
          this.chess.fen(),
          gameEndInfo
        ).then(result => {
          if (!result.success) {
            this.chess.undo()
            this.moveHistory.pop()
            this.fenHistory.pop()
            this.currentMoveIndex = this.moveHistory.length
            this.currentFen = this.chess.fen()
            this.error = result.error
          }
        }).catch(error => {
          console.error('Backend error:', error)
          this.error = this.$t('game.failedToMakeMove')
        })
      } catch (error) {
        console.error('Move error:', error)
        this.error = this.$t('game.failedToMakeMove')
      }
    },

    // Analysis mode methods
    toggleAnalysisMode() {
      this.analysisMode = !this.analysisMode

      if (this.analysisMode) {
        // Enter analysis mode at current position
        this.analyzeCurrentPosition()
      } else {
        // Exit analysis mode - reset to game state
        this.exitVariation()
        this.topMoves = []
        this.currentEvaluation = { score: 0, isMate: false, mateIn: null }
      }
    },

    async analyzeCurrentPosition() {
      if (!this.analysisMode) return

      this.analyzing = true

      try {
        const result = await gameService.analyzeMultiPv(this.chess.fen(), 15, 3)

        if (result.success && result.lines) {
          this.topMoves = result.lines.map(line => ({
            ...line,
            moveSan: this.convertUciToSan(line.move)
          }))

          // Set current evaluation from best line
          if (this.topMoves.length > 0) {
            const bestLine = this.topMoves[0]
            this.currentEvaluation = {
              score: bestLine.score,
              isMate: bestLine.isMate,
              mateIn: bestLine.mateIn
            }
          }
        }
      } catch (error) {
        console.error('Analysis error:', error)
      } finally {
        this.analyzing = false
      }
    },

    convertUciToSan(uciMove) {
      if (!uciMove || uciMove.length < 4) return uciMove

      try {
        // Create a temporary chess instance to convert UCI to SAN
        const tempChess = new Chess(this.chess.fen())
        const from = uciMove.substring(0, 2)
        const to = uciMove.substring(2, 4)
        const promotion = uciMove.length > 4 ? uciMove[4] : undefined

        const move = tempChess.move({ from, to, promotion })
        return move ? move.san : uciMove
      } catch {
        return uciMove
      }
    },

    playMove(uciMove) {
      if (!uciMove || uciMove.length < 4) return

      const from = uciMove.substring(0, 2)
      const to = uciMove.substring(2, 4)
      const promotion = uciMove.length > 4 ? uciMove[4] : 'q'

      // Simulate the move
      this.onMove({ from, to, promotion })
    },

    // Navigation methods
    handleKeyPress(event) {
      // Only handle arrow keys in analysis mode
      if (!this.analysisMode) return

      // Ignore if user is typing in an input field
      if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') return

      switch (event.key) {
        case 'ArrowLeft':
          event.preventDefault()
          this.goBack()
          break
        case 'ArrowRight':
          event.preventDefault()
          this.goForward()
          break
        case 'Home':
          event.preventDefault()
          this.goToStart()
          break
        case 'End':
          event.preventDefault()
          this.goToEnd()
          break
      }
    },

    goToStart() {
      if (this.isInVariation) {
        this.exitVariation()
      }
      this.goToMove(0)
    },

    goBack() {
      if (this.currentMoveIndex > 0) {
        this.goToMove(this.currentMoveIndex - 1)
      }
    },

    goForward() {
      if (this.canGoForward) {
        this.goToMove(this.currentMoveIndex + 1)
      }
    },

    goToEnd() {
      if (this.isInVariation) {
        this.exitVariation()
      }
      this.goToMove(this.moveHistory.length)
    },

    goToMove(index) {
      if (index < 0 || index > this.fenHistory.length - 1) return

      // If going back while in variation, check if we're going back to game position
      if (this.isInVariation && index <= this.savedMoveIndex) {
        // Exit variation if going back to or before variation start
        this.exitVariation()
        this.goToMove(index)
        return
      }

      this.currentMoveIndex = index
      this.currentFen = this.fenHistory[index]
      this.chess.load(this.currentFen)

      // Analyze new position
      if (this.analysisMode) {
        this.analyzeCurrentPosition()
      }
    },

    // Variation methods
    startVariation() {
      if (this.isInVariation) return

      this.isInVariation = true
      this.variationStartIndex = this.currentMoveIndex
      this.savedMoveHistory = [...this.moveHistory]
      this.savedFenHistory = [...this.fenHistory]
      this.savedMoveIndex = this.currentMoveIndex

      // Truncate history to current position for variation
      this.moveHistory = this.moveHistory.slice(0, this.currentMoveIndex)
      this.fenHistory = this.fenHistory.slice(0, this.currentMoveIndex + 1)
    },

    exitVariation() {
      if (!this.isInVariation) return

      // Restore original game history
      this.moveHistory = this.savedMoveHistory
      this.fenHistory = this.savedFenHistory
      this.currentMoveIndex = this.savedMoveIndex
      this.currentFen = this.fenHistory[this.currentMoveIndex]
      this.chess.load(this.currentFen)

      this.isInVariation = false
      this.variationStartIndex = 0
      this.savedMoveHistory = []
      this.savedFenHistory = []
      this.savedMoveIndex = 0

      if (this.analysisMode) {
        this.analyzeCurrentPosition()
      }
    },

    // Evaluation formatting
    formatEval(line) {
      if (line.isMate) {
        return `M${line.mateIn}`
      }
      const pawns = line.score / 100
      const sign = pawns >= 0 ? '+' : ''
      return sign + pawns.toFixed(1)
    },

    // Android bridge methods
    setupAndroidBridge() {
      // Expose methods for Android WebView
      window.isInAnalysisMode = () => this.analysisMode
      window.exitAnalysisMode = () => {
        if (this.analysisMode) {
          this.toggleAnalysisMode()
          return true
        }
        return false
      }
    },

    getEvalClass(line) {
      if (line.isMate) {
        return line.mateIn > 0 ? 'eval-winning' : 'eval-losing'
      }
      if (line.score > 100) return 'eval-winning'
      if (line.score < -100) return 'eval-losing'
      return 'eval-equal'
    },

    goBackToGames() {
      this.$router.push('/games')
    },

    startTimer() {
      this.timerInterval = setInterval(() => {
        // Only tick if time control is enabled and game is in progress (gameResult === 1)
        // This ensures timer doesn't start until second player joins
        if (this.hasTimeControl && this.gameResult === 1 && !this.analysisMode) {
          if (this.chess.turn() === 'w') {
            this.whiteTimeRemaining = Math.max(0, this.whiteTimeRemaining - 1)
            if (this.whiteTimeRemaining === 0) {
              this.error = 'White ran out of time! Black wins.'
              this.stopTimer()
            }
          } else {
            this.blackTimeRemaining = Math.max(0, this.blackTimeRemaining - 1)
            if (this.blackTimeRemaining === 0) {
              this.error = 'Black ran out of time! White wins.'
              this.stopTimer()
            }
          }
        }
      }, 1000)
    },

    stopTimer() {
      if (this.timerInterval) {
        clearInterval(this.timerInterval)
        this.timerInterval = null
      }
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
