<template>
  <div class="evaluation-bar">
    <div class="bar-container">
      <div class="bar-fill white-fill" :style="{ height: whitePercentage + '%' }"></div>
      <div class="bar-fill black-fill" :style="{ height: blackPercentage + '%' }"></div>
    </div>
    <div class="score-display">
      <span v-if="isMate" class="mate-score">M{{ mateIn }}</span>
      <span v-else class="cp-score">{{ formattedScore }}</span>
    </div>
  </div>
</template>

<script>
export default {
  name: 'EvaluationBar',
  props: {
    score: {
      type: Number,
      default: 0
    },
    isMate: {
      type: Boolean,
      default: false
    },
    mateIn: {
      type: Number,
      default: null
    }
  },
  computed: {
    // Convert centipawns to percentage for bar display
    // Score range: -1000 to +1000 cp maps to 0% to 100%
    whitePercentage() {
      if (this.isMate) {
        return this.mateIn > 0 ? 100 : 0
      }

      // Clamp score between -1000 and +1000
      const clampedScore = Math.max(-1000, Math.min(1000, this.score))
      // Convert to percentage (0-100)
      return ((clampedScore + 1000) / 2000) * 100
    },

    blackPercentage() {
      return 100 - this.whitePercentage
    },

    formattedScore() {
      // Convert centipawns to pawns (e.g., 150 -> +1.5)
      const pawns = this.score / 100
      const sign = pawns >= 0 ? '+' : ''
      return sign + pawns.toFixed(1)
    }
  }
}
</script>

<style scoped>
.evaluation-bar {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 30px;
  height: 100%;
  min-height: 400px;
}

.bar-container {
  flex: 1;
  width: 20px;
  display: flex;
  flex-direction: column;
  border-radius: 4px;
  overflow: hidden;
  background: rgba(27, 35, 64, 0.6);
  border: 1px solid rgba(197, 212, 255, 0.2);
  position: relative;
}

.bar-fill {
  width: 100%;
  transition: height 0.3s ease;
}

.white-fill {
  background: linear-gradient(180deg, #f0f0f0 0%, #d0d0d0 100%);
  position: absolute;
  bottom: 0;
}

.black-fill {
  background: linear-gradient(180deg, #2a2a2a 0%, #1a1a1a 100%);
  position: absolute;
  top: 0;
}

.score-display {
  margin-top: 8px;
  font-family: 'Courier New', monospace;
  font-size: 0.75rem;
  font-weight: 600;
  text-align: center;
}

.cp-score {
  color: var(--cosmic-stars, #C5D4FF);
}

.mate-score {
  color: #ff6b6b;
  font-weight: 700;
}
</style>
