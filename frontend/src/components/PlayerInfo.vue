<template>
  <div class="player-info" :class="{ 'player-active': isActive }">
    <div class="player-main">
      <span class="player-name">{{ label }}</span>
      <div v-if="showCaptured && hasCapturedPieces" class="captured-pieces">
        <span
          v-for="(count, piece) in sortedCaptured"
          :key="piece"
          class="captured-piece"
        >
          <span class="piece-symbol">{{ getPieceSymbol(piece) }}</span>
          <span v-if="count > 1" class="piece-count">×{{ count }}</span>
        </span>
      </div>
    </div>
    <div v-if="showAdvantage && hasAdvantage" class="material-advantage">
      +{{ advantageDisplay }}
    </div>
  </div>
</template>

<script>
import {
  getCapturedPieces,
  getMaterialAdvantage,
  getPieceSymbol,
  sortCapturedPieces
} from '../utils/capturedPieces'

export default {
  name: 'PlayerInfo',
  props: {
    label: {
      type: String,
      required: true
    },
    fen: {
      type: String,
      required: true
    },
    playerColor: {
      type: String, // 'white' or 'black'
      required: true,
      validator: (value) => ['white', 'black'].includes(value)
    },
    isActive: {
      type: Boolean,
      default: false
    }
  },
  computed: {
    capturedPieces() {
      const captured = getCapturedPieces(this.fen)
      // Return pieces captured BY this player (opponent's lost pieces)
      return this.playerColor === 'white'
        ? captured.capturedByWhite
        : captured.capturedByBlack
    },

    hasCapturedPieces() {
      return Object.keys(this.capturedPieces).length > 0
    },

    sortedCaptured() {
      return Object.fromEntries(sortCapturedPieces(this.capturedPieces))
    },

    materialAdvantage() {
      return getMaterialAdvantage(this.fen)
    },

    hasAdvantage() {
      return this.materialAdvantage.side === this.playerColor
    },

    advantageDisplay() {
      const advantage = this.materialAdvantage.advantage
      // Display in whole pawns (rounded)
      if (advantage >= 100) {
        const pawns = Math.round(advantage / 100)
        return pawns.toString()
      }
      return '0'
    },

    showCaptured() {
      // Always show captured pieces section
      return true
    },

    showAdvantage() {
      // Only show advantage if this player has it and it's significant
      return this.hasAdvantage && this.materialAdvantage.advantage >= 100
    },

    opponentColor() {
      return this.playerColor === 'white' ? 'black' : 'white'
    }
  },
  methods: {
    getPieceSymbol(piece) {
      // Show opponent's pieces (the ones this player captured)
      return getPieceSymbol(piece, this.opponentColor)
    }
  }
}
</script>

<style scoped>
.player-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0.75rem;
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  transition: all 0.3s ease;
  gap: 0.75rem;
}

.player-info.player-active {
  border-color: rgba(122, 76, 224, 0.5);
  box-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.player-main {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex: 1;
  min-width: 0;
}

.player-name {
  font-family: var(--font-body, 'Inter', sans-serif);
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--cosmic-figures, #F2F2F2);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.captured-pieces {
  display: flex;
  gap: 0.2rem;
  flex-wrap: wrap;
  align-items: center;
}

.captured-piece {
  display: inline-flex;
  align-items: center;
  gap: 0.1rem;
  font-size: 1rem;
  opacity: 0.9;
}

.piece-symbol {
  line-height: 1;
}

.piece-count {
  font-size: 0.65rem;
  color: var(--cosmic-stars, #C5D4FF);
  font-weight: 600;
  vertical-align: super;
  line-height: 1;
}

.material-advantage {
  font-family: 'Courier New', monospace;
  font-size: 0.9rem;
  font-weight: 700;
  color: #4ade80;
  background: rgba(74, 222, 128, 0.15);
  padding: 0.25rem 0.5rem;
  border-radius: 6px;
  border: 1px solid rgba(74, 222, 128, 0.3);
  white-space: nowrap;
  text-shadow: 0 0 8px rgba(74, 222, 128, 0.4);
}

@media (max-width: 768px) {
  .player-info {
    padding: 0.4rem 0.6rem;
    gap: 0.5rem;
  }

  .player-name {
    font-size: 0.8rem;
  }

  .captured-piece {
    font-size: 0.9rem;
  }

  .piece-count {
    font-size: 0.6rem;
  }

  .material-advantage {
    font-size: 0.8rem;
    padding: 0.2rem 0.4rem;
  }
}
</style>
