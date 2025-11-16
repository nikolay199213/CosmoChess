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
  watch: {
    currentFen(newFen) {
      // Update board position through API when FEN changes
      if (this.boardAPI) {
        this.boardAPI.setPosition(newFen)
      }
    }
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

          // Load move history if available
          if (result.game.moves && result.game.moves.length > 0) {
            this.moveHistory = result.game.moves.map(m => m.move)
            console.log('Loaded move history:', this.moveHistory)
          }

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
      console.log('Current user ID:', currentUserId)
      console.log('Move from user ID:', data.userId)

      // Don't apply the move if it's from the current user (already applied locally)
      if (data.userId === currentUserId) {
        console.log('Ignoring own move')
        return
      }

      try {
        // Use FEN as the source of truth (most reliable)
        if (data.newFen) {
          console.log('Loading FEN from server:', data.newFen)
          this.chess.load(data.newFen)

          // Update move history
          if (data.move) {
            this.moveHistory.push(data.move)
          }
        } else if (data.move) {
          // Fallback to applying move in SAN format
          console.log('Applying move in SAN format:', data.move)
          const move = this.chess.move(data.move)
          if (move) {
            this.moveHistory.push(move.san)
          }
        }

        // Update the board - this triggers boardConfig recomputation
        this.currentFen = this.chess.fen()
        console.log('Move applied successfully, new FEN:', this.currentFen)
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
  border-bottom: 1px solid rgba(197, 212, 255, 0.2);
}

.game-header h1 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 700;
  font-size: 2rem;
  margin: 0;
  text-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.game-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.game-status {
  font-weight: 600;
  color: var(--cosmic-action-primary, #7A4CE0);
  font-size: 1.1rem;
  font-family: var(--font-body, 'Inter', sans-serif);
  text-shadow: 0 0 10px rgba(122, 76, 224, 0.4);
}

.suggestion {
  background: rgba(122, 76, 224, 0.15);
  border: 1px solid rgba(122, 76, 224, 0.3);
  border-radius: var(--card-radius, 12px);
  padding: 1rem;
  margin-bottom: 1rem;
  color: var(--cosmic-stars, #C5D4FF);
  backdrop-filter: blur(10px);
}

.suggestion strong {
  color: var(--cosmic-figures, #F2F2F2);
}

.game-board {
  display: grid;
  grid-template-columns: 1fr 350px;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 1024px) {
  .game-board {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
}

@media (max-width: 768px) {
  .game-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .game-header h1 {
    font-size: 1.5rem;
  }

  .game-info {
    flex-wrap: wrap;
    width: 100%;
  }

  .chessboard-container {
    padding: 0.75rem;
    max-width: 100%;
    width: 100%;
  }

  .main-content {
    padding: 1rem;
  }

  .connection-status {
    font-size: 0.8rem;
    padding: 0.3rem 0.6rem;
  }

  .game-status {
    font-size: 1rem;
  }
}

.chessboard-container {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: var(--card-radius, 12px);
  padding: 1.5rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 60px rgba(122, 76, 224, 0.1);
  max-width: 650px;
  margin: 0 auto;
}

.game-sidebar {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: var(--card-radius, 12px);
  padding: 1.5rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  height: fit-content;
}

.game-details,
.move-history,
.game-controls {
  margin-bottom: 2rem;
}

.game-details:last-child,
.move-history:last-child,
.game-controls:last-child {
  margin-bottom: 0;
}

.game-details h3,
.move-history h3 {
  margin: 0 0 1rem 0;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-size: 1.2rem;
  font-weight: 600;
  border-bottom: 1px solid rgba(197, 212, 255, 0.15);
  padding-bottom: 0.5rem;
}

.game-details p {
  margin: 0.5rem 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-family: var(--font-body, 'Inter', sans-serif);
}

.game-details p strong {
  color: var(--cosmic-figures, #F2F2F2);
  font-weight: 600;
}

.moves-list {
  max-height: 250px;
  overflow-y: auto;
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: 8px;
  padding: 0.5rem;
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
  padding: 0.4rem 0.5rem;
  font-family: 'Courier New', monospace;
  font-size: 0.95rem;
  color: var(--cosmic-stars, #C5D4FF);
  border-radius: 4px;
  transition: background var(--transition-smooth, 200ms);
}

.move-item:nth-child(even) {
  background-color: rgba(27, 35, 64, 0.3);
}

.move-item:hover {
  background-color: rgba(122, 76, 224, 0.2);
  color: var(--cosmic-figures, #F2F2F2);
}

.game-controls .btn {
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
/* Note: These styles are not scoped to affect the chessboard component */

.cg-wrap {
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 0 40px rgba(122, 76, 224, 0.2);
}

/* Chess board squares - Cosmic dark theme */
cg-board square.light {
  background-color: var(--chess-square-light, #10131C) !important;
}

cg-board square.dark {
  background-color: var(--chess-square-dark, #1E2433) !important;
}

/* Move highlights with cosmic glow */
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

/* Piece move indicator dots */
cg-board square.move-dest::before {
  background: radial-gradient(
    circle,
    rgba(197, 212, 255, 0.8) 0%,
    rgba(122, 76, 224, 0.6) 50%,
    transparent 100%
  ) !important;
  box-shadow: 0 0 8px rgba(122, 76, 224, 0.6);
}

/* Hover effect on squares */
cg-board square:hover {
  background-color: rgba(122, 76, 224, 0.15) !important;
}

/* Check indicator */
cg-board square.check {
  background: radial-gradient(
    circle,
    rgba(101, 61, 137, 0.6) 0%,
    rgba(101, 61, 137, 0.3) 50%,
    transparent 100%
  ) !important;
  box-shadow: 0 0 15px rgba(101, 61, 137, 0.5);
}

/* Coordinates (letters and numbers on board edges) */
coords {
  color: var(--cosmic-stars, #C5D4FF) !important;
  opacity: 0.7;
  font-family: var(--font-body, 'Inter', sans-serif) !important;
  font-size: 0.8rem;
}

/* Mobile responsiveness for chessboard */
@media (max-width: 768px) {
  .cg-wrap {
    width: 100% !important;
    height: auto !important;
  }

  cg-board {
    width: 100% !important;
    height: 100% !important;
  }

  coords {
    font-size: 0.6rem !important;
  }
}
</style>