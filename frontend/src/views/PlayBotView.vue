<template>
  <div class="play-bot-container">
    <div class="play-bot-header">
      <h1>Play vs Bot</h1>
      <p>Choose difficulty and time control to start a game against the computer</p>
    </div>

    <div class="settings-section">
      <div class="setting-card">
        <h2>Difficulty</h2>
        <div class="difficulty-options">
          <button
            v-for="diff in difficulties"
            :key="diff.value"
            :class="['difficulty-btn', { active: selectedDifficulty === diff.value }]"
            @click="selectedDifficulty = diff.value"
          >
            <span class="diff-name">{{ diff.name }}</span>
            <span class="diff-rating">~{{ diff.rating }}</span>
          </button>
        </div>
      </div>

      <div class="setting-card">
        <h2>Time Control</h2>
        <select v-model="selectedTimeControl" class="time-control-select">
          <option value="0">No time control</option>
          <option value="1">Bullet 1+0</option>
          <option value="2">Bullet 1+1</option>
          <option value="3">Blitz 3+0</option>
          <option value="4">Blitz 3+2</option>
          <option value="5">Blitz 5+0</option>
          <option value="6">Rapid 10+0</option>
          <option value="7">Rapid 10+5</option>
          <option value="8">Rapid 15+10</option>
        </select>
      </div>
    </div>

    <div class="action-section">
      <button @click="startGame" class="btn btn-success btn-large" :disabled="creatingGame">
        {{ creatingGame ? 'Starting...' : 'Start Game' }}
      </button>
      <button @click="goBack" class="btn btn-secondary">
        Back to Home
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
      selectedTimeControl: 0,
      creatingGame: false,
      error: '',
      difficulties: [
        { value: 1, name: 'Beginner', rating: '400' },
        { value: 2, name: 'Easy', rating: '800' },
        { value: 3, name: 'Medium', rating: '1200' },
        { value: 4, name: 'Hard', rating: '1600' },
        { value: 5, name: 'Expert', rating: '2000' },
        { value: 6, name: 'Master', rating: '2400+' }
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
          parseInt(this.selectedTimeControl)
        )

        if (result.success) {
          this.$router.push(`/game/${result.gameId}`)
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Failed to create game'
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
