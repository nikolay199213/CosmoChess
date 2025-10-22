<template>
  <div class="game-container">
    <div class="game-header">
      <h1>Game #{{ gameId.substring(0, 8) }}</h1>
      <div class="game-info">
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
      chess: new Chess(),
      boardAPI: null,
      error: '',
      analyzing: false,
      suggestion: '',
      moveHistory: [],
      loading: true
    }
  },
  computed: {
    boardConfig() {
      return {
        fen: this.chess.fen(),
        orientation: 'white',
        movable: {
          free: false,
          color: this.chess.turn() === 'w' ? 'white' : 'black',
          dests: this.possibleMoves()
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
    }
  },
  async mounted() {
    await this.initializeGame()
  },
  methods: {
    async initializeGame() {
      try {
        this.chess.reset()
        this.loading = false
      } catch (error) {
        this.error = 'Failed to initialize game'
        console.error('Game initialization error:', error)
      }
    },

    possibleMoves() {
      const dests = new Map()
      const moves = this.chess.moves({ verbose: true })

      moves.forEach(move => {
        if (!dests.has(move.from)) {
          dests.set(move.from, [])
        }
        dests.get(move.from).push(move.to)
      })

      return dests
    },

    onMove(move) {
      this.error = ''

      try {
        const chessMove = this.chess.move({
          from: move.from,
          to: move.to,
          promotion: 'q'
        })

        if (chessMove === null) {
          this.error = 'Invalid move'
          return
        }

        this.moveHistory.push(chessMove.san)

        // Make the move on the backend
        gameService.makeMove(
          this.gameId,
          chessMove.san,
          this.chess.fen()
        ).then(result => {
          if (!result.success) {
            // Undo the move
            this.chess.undo()
            this.moveHistory.pop()
            this.error = result.error
          }
        }).catch(error => {
          this.error = 'Failed to make move'
          console.error('Move error:', error)
        })
      } catch (error) {
        this.error = 'Failed to make move'
        console.error('Move error:', error)
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
</style>