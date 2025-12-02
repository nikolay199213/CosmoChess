<template>
  <div class="play-bot-container">
    <div class="play-bot-header">
      <h1>{{ $t('playBot.title') }}</h1>
      <p>{{ $t('playBot.subtitle') }}</p>
    </div>

    <div class="settings-section">
      <div class="setting-card">
        <h2>{{ $t('playBot.difficulty') }}</h2>
        <div class="difficulty-options">
          <button
            v-for="diff in difficulties"
            :key="diff.value"
            :class="['difficulty-btn', { active: selectedDifficulty === diff.value }]"
            @click="selectedDifficulty = diff.value"
          >
            <span class="diff-name">{{ $t(diff.nameKey) }}</span>
            <span class="diff-rating">{{ $t(diff.ratingKey) }}</span>
          </button>
        </div>
      </div>

      <div class="setting-card">
        <h2>{{ $t('playBot.playingStyle') }}</h2>
        <div class="style-options">
          <button
            v-for="style in styles"
            :key="style.value"
            :class="['style-btn', { active: selectedStyle === style.value }]"
            @click="selectedStyle = style.value"
          >
            <span class="style-icon">{{ style.icon }}</span>
            <span class="style-name">{{ $t(style.nameKey) }}</span>
            <span class="style-desc">{{ $t(style.descKey) }}</span>
          </button>
        </div>
      </div>

      <div class="setting-card">
        <h2>{{ $t('playBot.timeControl') }}</h2>
        <select v-model="selectedTimeControl" class="time-control-select">
          <option value="0">{{ $t('timeControl.noTimeControl') }}</option>
          <option value="1">{{ $t('timeControl.bullet1') }}</option>
          <option value="2">{{ $t('timeControl.bullet1_1') }}</option>
          <option value="3">{{ $t('timeControl.blitz3') }}</option>
          <option value="4">{{ $t('timeControl.blitz3_2') }}</option>
          <option value="5">{{ $t('timeControl.blitz5') }}</option>
          <option value="6">{{ $t('timeControl.rapid10') }}</option>
          <option value="7">{{ $t('timeControl.rapid10_5') }}</option>
          <option value="8">{{ $t('timeControl.rapid15_10') }}</option>
        </select>
      </div>
    </div>

    <div class="action-section">
      <button @click="startGame" class="btn btn-success btn-large" :disabled="creatingGame">
        {{ creatingGame ? $t('playBot.starting') : $t('playBot.startGame') }}
      </button>
      <button @click="goBack" class="btn btn-secondary">
        {{ $t('playBot.backToHome') }}
      </button>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>
  </div>
</template>

<script>
import { gameService } from '../services/gameService'

export default {
  name: 'PlayBotView',
  data() {
    return {
      selectedDifficulty: 3, // Medium by default
      selectedStyle: 0, // Balanced by default
      selectedTimeControl: 0,
      creatingGame: false,
      error: '',
      difficulties: [
        { value: 1, nameKey: 'playBot.beginner', ratingKey: 'playBot.beginnerRating' },
        { value: 2, nameKey: 'playBot.easy', ratingKey: 'playBot.easyRating' },
        { value: 3, nameKey: 'playBot.medium', ratingKey: 'playBot.mediumRating' },
        { value: 4, nameKey: 'playBot.hard', ratingKey: 'playBot.hardRating' },
        { value: 5, nameKey: 'playBot.expert', ratingKey: 'playBot.expertRating' },
        { value: 6, nameKey: 'playBot.master', ratingKey: 'playBot.masterRating' }
      ],
      styles: [
        { value: 0, nameKey: 'playBot.balanced', icon: '⚖️', descKey: 'playBot.balancedDesc' },
        { value: 1, nameKey: 'playBot.aggressive', icon: '⚔️', descKey: 'playBot.aggressiveDesc' },
        { value: 2, nameKey: 'playBot.solid', icon: '🛡️', descKey: 'playBot.solidDesc' }
      ]
    }
  },
  methods: {
    async startGame() {
      this.creatingGame = true
      this.error = ''

      try {
        const result = await gameService.createBotGame(
          this.selectedDifficulty,
          this.selectedStyle,
          parseInt(this.selectedTimeControl)
        )

        if (result.success) {
          this.$router.push(`/game/${result.gameId}`)
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = this.$t('playBot.failedToCreateGame')
        console.error('Error creating bot game:', error)
      } finally {
        this.creatingGame = false
      }
    },

    goBack() {
      this.$router.push('/home')
    }
  }
}
</script>

<style scoped>
.play-bot-container {
  max-width: 800px;
  margin: 0 auto;
}

.play-bot-header {
  text-align: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid rgba(197, 212, 255, 0.2);
}

.play-bot-header h1 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 700;
  font-size: 2.5rem;
  margin: 0 0 0.5rem 0;
  text-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.play-bot-header p {
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 1rem;
  margin: 0;
}

.settings-section {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  margin-bottom: 2rem;
}

.setting-card {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: var(--card-radius, 12px);
  padding: 1.5rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.setting-card h2 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 600;
  font-size: 1.3rem;
  margin: 0 0 1rem 0;
}

.difficulty-options {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
}

@media (max-width: 600px) {
  .difficulty-options {
    grid-template-columns: repeat(2, 1fr);
  }
}

.difficulty-btn {
  background: rgba(27, 35, 64, 0.6);
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  padding: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.difficulty-btn:hover {
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.2);
}

.difficulty-btn.active {
  background: linear-gradient(135deg, rgba(122, 76, 224, 0.3) 0%, rgba(157, 113, 255, 0.2) 100%);
  border-color: rgba(122, 76, 224, 0.5);
  box-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.diff-name {
  color: var(--cosmic-figures, #F2F2F2);
  font-weight: 600;
  font-size: 0.95rem;
}

.diff-rating {
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.8rem;
  opacity: 0.8;
}

.style-options {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
}

@media (max-width: 600px) {
  .style-options {
    grid-template-columns: 1fr;
  }
}

.style-btn {
  background: rgba(27, 35, 64, 0.6);
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  padding: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.style-btn:hover {
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.2);
}

.style-btn.active {
  background: linear-gradient(135deg, rgba(122, 76, 224, 0.3) 0%, rgba(157, 113, 255, 0.2) 100%);
  border-color: rgba(122, 76, 224, 0.5);
  box-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.style-icon {
  font-size: 2rem;
}

.style-name {
  color: var(--cosmic-figures, #F2F2F2);
  font-weight: 600;
  font-size: 0.95rem;
}

.style-desc {
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.75rem;
  opacity: 0.8;
  text-align: center;
}

.time-control-select {
  width: 100%;
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-body, 'Inter', sans-serif);
  font-size: 0.9rem;
  cursor: pointer;
  backdrop-filter: blur(10px);
  transition: all 0.3s ease;
}

.time-control-select:hover {
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.2);
}

.time-control-select:focus {
  outline: none;
  border-color: rgba(122, 76, 224, 0.5);
  box-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.time-control-select option {
  background: #1B2340;
  color: #F2F2F2;
}

.action-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

.btn-large {
  padding: 1rem 3rem;
  font-size: 1.1rem;
  width: 100%;
  max-width: 300px;
}

.btn-secondary {
  background: rgba(197, 212, 255, 0.1);
  border: 1px solid rgba(197, 212, 255, 0.2);
  color: var(--cosmic-stars, #C5D4FF);
  padding: 0.75rem 2rem;
}

.btn-secondary:hover {
  background: rgba(197, 212, 255, 0.2);
  border-color: rgba(197, 212, 255, 0.3);
}

.error {
  background: rgba(244, 67, 54, 0.1);
  border: 1px solid rgba(244, 67, 54, 0.3);
  border-radius: var(--card-radius, 12px);
  padding: 1rem;
  color: #f44336;
  margin-top: 1rem;
  text-align: center;
}
</style>
