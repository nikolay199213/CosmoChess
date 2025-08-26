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
        <div class="board">
          <div v-for="(row, rowIndex) in board" :key="rowIndex" class="board-row">
            <div 
              v-for="(square, colIndex) in row" 
              :key="colIndex"
              class="board-square"
              :class="{
                'light-square': (rowIndex + colIndex) % 2 === 0,
                'dark-square': (rowIndex + colIndex) % 2 === 1,
                'selected': isSelected(rowIndex, colIndex),
                'valid-move': isValidMove(rowIndex, colIndex)
              }"
              @click="onSquareClick(rowIndex, colIndex)"
            >
              <span v-if="square" class="piece" :class="getPieceClass(square)">
                {{ getPieceSymbol(square) }}
              </span>
            </div>
          </div>
        </div>
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
import { gameService } from '../services/gameService'
import { authService } from '../services/authService'

export default {
  name: 'GameView',
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
      board: [],
      error: '',
      analyzing: false,
      suggestion: '',
      moveHistory: [],
      loading: true,
      selectedSquare: null,
      validMoves: []
    }
  },
  computed: {
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
        this.updateBoard()
        this.loading = false
      } catch (error) {
        this.error = 'Failed to initialize game'
        console.error('Game initialization error:', error)
      }
    },

    updateBoard() {
      const board = this.chess.board()
      this.board = board.map(row => 
        row.map(square => square ? square : null)
      )
    },

    onSquareClick(row, col) {
      if (!this.isPlayerTurn) return

      const square = this.getSquareNotation(row, col)
      
      if (this.selectedSquare === null) {
        // First click - select piece
        const piece = this.board[row][col]
        if (piece && piece.color === this.chess.turn()) {
          this.selectedSquare = { row, col }
          this.validMoves = this.chess.moves({ 
            square: square, 
            verbose: true 
          }).map(move => this.parseSquare(move.to))
        }
      } else {
        // Second click - try to move
        const from = this.getSquareNotation(this.selectedSquare.row, this.selectedSquare.col)
        const to = square
        
        this.makeMove(from, to)
        this.selectedSquare = null
        this.validMoves = []
      }
    },

    async makeMove(from, to) {
      this.error = ''

      try {
        const move = this.chess.move({
          from: from,
          to: to,
          promotion: 'q'
        })

        if (move === null) {
          this.error = 'Invalid move'
          return
        }

        this.moveHistory.push(move.san)
        this.updateBoard()

        // Make the move on the backend
        const result = await gameService.makeMove(
          this.gameId,
          move.san,
          this.chess.fen()
        )

        if (!result.success) {
          // Undo the move
          this.chess.undo()
          this.moveHistory.pop()
          this.updateBoard()
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Failed to make move'
        console.error('Move error:', error)
      }
    },

    async analyzePosition() {
      this.analyzing = true
      this.suggestion = ''

      try {
        const result = await gameService.analyzePosition(this.chess.fen(), 10)
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

    getSquareNotation(row, col) {
      const files = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h']
      const ranks = ['8', '7', '6', '5', '4', '3', '2', '1']
      return files[col] + ranks[row]
    },

    parseSquare(square) {
      const files = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h']
      const ranks = ['8', '7', '6', '5', '4', '3', '2', '1']
      const col = files.indexOf(square[0])
      const row = ranks.indexOf(square[1])
      return { row, col }
    },

    isSelected(row, col) {
      return this.selectedSquare && 
             this.selectedSquare.row === row && 
             this.selectedSquare.col === col
    },

    isValidMove(row, col) {
      return this.validMoves.some(move => move.row === row && move.col === col)
    },

    getPieceClass(piece) {
      return `piece-${piece.color}${piece.type}`
    },

    getPieceSymbol(piece) {
      const symbols = {
        'wk': '♔', 'wq': '♕', 'wr': '♖', 'wb': '♗', 'wn': '♘', 'wp': '♙',
        'bk': '♚', 'bq': '♛', 'br': '♜', 'bb': '♝', 'bn': '♞', 'bp': '♟'
      }
      return symbols[piece.color + piece.type] || ''
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
}

.board {
  width: 400px;
  height: 400px;
  margin: 0 auto;
  border: 2px solid #8b4513;
}

.board-row {
  display: flex;
  height: 50px;
}

.board-square {
  width: 50px;
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  position: relative;
  transition: background-color 0.2s;
}

.light-square {
  background-color: #f0d9b5;
}

.dark-square {
  background-color: #b58863;
}

.board-square:hover {
  opacity: 0.8;
}

.board-square.selected {
  background-color: #ffff00 !important;
  box-shadow: inset 0 0 10px rgba(0, 0, 0, 0.3);
}

.board-square.valid-move {
  background-color: #90ee90 !important;
}

.board-square.valid-move::after {
  content: '';
  position: absolute;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background-color: rgba(0, 128, 0, 0.7);
}

.piece {
  font-size: 32px;
  user-select: none;
  text-shadow: 1px 1px 1px rgba(0, 0, 0, 0.3);
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