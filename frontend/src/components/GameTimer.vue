<template>
  <div class="game-timer" :class="{ 'timer-active': isActive, 'timer-low': isLowTime }">
    <div class="timer-label">{{ label }}</div>
    <div class="timer-display">{{ formattedTime }}</div>
  </div>
</template>

<script>
export default {
  name: 'GameTimer',
  props: {
    timeSeconds: {
      type: Number,
      required: true
    },
    isActive: {
      type: Boolean,
      default: false
    },
    label: {
      type: String,
      default: ''
    }
  },
  computed: {
    formattedTime() {
      if (this.timeSeconds <= 0) {
        return '0:00'
      }

      const minutes = Math.floor(this.timeSeconds / 60)
      const seconds = this.timeSeconds % 60
      return `${minutes}:${seconds.toString().padStart(2, '0')}`
    },
    isLowTime() {
      return this.timeSeconds <= 30 && this.timeSeconds > 0
    }
  }
}
</script>

<style scoped>
.game-timer {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  padding: 0.5rem 1rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  transition: all 0.3s ease;
}

.game-timer.timer-active {
  border-color: rgba(122, 76, 224, 0.5);
  box-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.game-timer.timer-low {
  border-color: rgba(224, 76, 76, 0.5);
  box-shadow: 0 0 20px rgba(224, 76, 76, 0.3);
  animation: pulse 1s infinite;
}

@keyframes pulse {
  0%, 100% {
    box-shadow: 0 0 20px rgba(224, 76, 76, 0.3);
  }
  50% {
    box-shadow: 0 0 30px rgba(224, 76, 76, 0.6);
  }
}

.timer-label {
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.75rem;
  font-family: var(--font-body, 'Inter', sans-serif);
  margin-bottom: 0.15rem;
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.timer-display {
  color: var(--cosmic-figures, #F2F2F2);
  font-size: 1.5rem;
  font-weight: 700;
  font-family: 'Courier New', monospace;
  text-align: center;
  text-shadow: 0 0 10px rgba(122, 76, 224, 0.3);
}

.timer-low .timer-display {
  color: #FF6B6B;
  text-shadow: 0 0 10px rgba(224, 76, 76, 0.5);
}

@media (max-width: 768px) {
  .game-timer {
    padding: 0.4rem 0.75rem;
  }

  .timer-label {
    font-size: 0.65rem;
  }

  .timer-display {
    font-size: 1.2rem;
  }
}
</style>
