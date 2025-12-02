<template>
  <div class="home-container">
    <div class="home-header">
      <h1>{{ $t('home.title') }}</h1>
    </div>

    <div class="actions-section">
      <div class="action-card">
        <h2>{{ $t('home.createNewGame') }}</h2>
        <p>{{ $t('home.createNewGameDesc') }}</p>
        <div class="action-controls">
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
            <option value="9">{{ $t('timeControl.daily') }}</option>
          </select>
          <button @click="createGame" class="btn btn-success" :disabled="creatingGame">
            {{ creatingGame ? $t('home.creating') : $t('home.createGame') }}
          </button>
        </div>
      </div>

      <div class="action-card">
        <h2>{{ $t('home.playVsBot') }}</h2>
        <p>{{ $t('home.playVsBotDesc') }}</p>
        <button @click="goToPlayBot" class="btn btn-primary">
          {{ $t('home.playBot') }}
        </button>
      </div>

      <div class="action-card">
        <h2>{{ $t('home.availableGames') }}</h2>
        <p>{{ $t('home.availableGamesDesc') }}</p>
        <button @click="goToGames" class="btn btn-primary">
          {{ $t('home.browseGames') }}
        </button>
      </div>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <div class="history-section">
      <h2>{{ $t('home.gameHistory') }}</h2>
      <div v-if="loadingHistory" class="loading">
        {{ $t('home.loadingGameHistory') }}
      </div>

      <div v-else-if="gameHistory.length === 0" class="no-games">
        {{ $t('home.noGamesYet') }}
      </div>

      <div v-else class="games-list">
        <div
          v-for="game in gameHistory"
          :key="game.id"
          class="game-card"
          @click="analyzeGame(game.id)"
        >
          <div class="game-info">
            <div class="game-header">
              <h3>Game #{{ game.id.substring(0, 8) }}</h3>
              <span :class="getResultClass(game)">{{ getGameResult(game) }}</span>
            </div>
            <p class="game-date">{{ formatDate(game.startedAt) }}</p>
            <p class="game-players">
              <span :class="{ 'you': game.whitePlayerId === userId }">{{ $t('game.white') }}</span>
              vs
              <span :class="{ 'you': game.blackPlayerId === userId }">{{ $t('game.black') }}</span>
            </p>
            <p class="game-time-control">{{ getTimeControlName(game.timeControl) }}</p>
          </div>
          <div class="game-actions">
            <button class="btn btn-secondary btn-sm">
              {{ $t('home.analyze') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { gameService } from '../services/gameService'
import { authService } from '../services/authService'

export default {
  name: 'HomeView',
  data() {
    return {
      gameHistory: [],
      loadingHistory: false,
      creatingGame: false,
      error: '',
      selectedTimeControl: 0,
      userId: null
    }
  },
  async mounted() {
    this.userId = authService.getUserId()
    await this.loadGameHistory()
  },
  methods: {
    async loadGameHistory() {
      this.loadingHistory = true
      this.error = ''

      try {
        const result = await gameService.getUserGames(this.userId, 0, 50)
        if (result.success) {
          this.gameHistory = result.games
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Failed to load game history'
        console.error('Error loading game history:', error)
      } finally {
        this.loadingHistory = false
      }
    },

    async createGame() {
      this.creatingGame = true
      this.error = ''

      try {
        const result = await gameService.createGame(parseInt(this.selectedTimeControl))
        if (result.success) {
          this.$router.push(`/game/${result.gameId}`)
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Failed to create game'
        console.error('Error creating game:', error)
      } finally {
        this.creatingGame = false
      }
    },

    goToGames() {
      this.$router.push('/games')
    },

    goToPlayBot() {
      this.$router.push('/play/bot')
    },

    analyzeGame(gameId) {
      this.$router.push(`/game/${gameId}`)
    },

    getGameResult(game) {
      const isWhite = game.whitePlayerId === this.userId
      switch (game.gameResult) {
        case 0: return this.$t('gameStatus.waiting')
        case 1: return this.$t('gameStatus.inProgress')
        case 2: return isWhite ? this.$t('gameStatus.victory') : this.$t('gameStatus.defeat')
        case 3: return isWhite ? this.$t('gameStatus.defeat') : this.$t('gameStatus.victory')
        case 4: return this.$t('gameStatus.draw')
        default: return this.$t('gameStatus.unknown')
      }
    },

    getResultClass(game) {
      const isWhite = game.whitePlayerId === this.userId
      switch (game.gameResult) {
        case 0: return 'result-waiting'
        case 1: return 'result-progress'
        case 2: return isWhite ? 'result-win' : 'result-loss'
        case 3: return isWhite ? 'result-loss' : 'result-win'
        case 4: return 'result-draw'
        default: return ''
      }
    },

    getTimeControlName(timeControl) {
      const names = {
        0: this.$t('timeControl.noTimeControl'),
        1: this.$t('timeControl.bullet1'),
        2: this.$t('timeControl.bullet1_1'),
        3: this.$t('timeControl.blitz3'),
        4: this.$t('timeControl.blitz3_2'),
        5: this.$t('timeControl.blitz5'),
        6: this.$t('timeControl.rapid10'),
        7: this.$t('timeControl.rapid10_5'),
        8: this.$t('timeControl.rapid15_10'),
        9: this.$t('timeControl.daily')
      }
      return names[timeControl] || this.$t('gameStatus.unknown')
    },

    formatDate(dateString) {
      const date = new Date(dateString)
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      })
    }
  }
}
</script>

<style scoped>
.home-container {
  max-width: 1200px;
  margin: 0 auto;
}

.home-header {
  text-align: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid rgba(197, 212, 255, 0.2);
}

.home-header h1 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 700;
  font-size: 2.5rem;
  margin: 0;
  text-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.actions-section {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-bottom: 3rem;
}

@media (max-width: 768px) {
  .actions-section {
    grid-template-columns: 1fr;
  }
}

.action-card {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: var(--card-radius, 12px);
  padding: 2rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  transition: all var(--transition-smooth, 200ms);
}

.action-card:hover {
  transform: translateY(-2px);
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4), 0 0 30px rgba(122, 76, 224, 0.15);
}

.action-card h2 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 600;
  font-size: 1.5rem;
  margin: 0 0 0.75rem 0;
}

.action-card p {
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.9rem;
  margin: 0 0 1.5rem 0;
}

.action-controls {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.time-control-select {
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
  flex: 1;
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

.history-section {
  margin-top: 2rem;
}

.history-section h2 {
  color: var(--cosmic-stars, #C5D4FF);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 600;
  font-size: 1.3rem;
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid rgba(197, 212, 255, 0.15);
}

.loading, .no-games {
  text-align: center;
  color: var(--cosmic-stars, #C5D4FF);
  padding: 2rem;
  background: rgba(27, 35, 64, 0.3);
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: var(--card-radius, 12px);
  font-style: italic;
  backdrop-filter: blur(10px);
}

.games-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.game-card {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.1);
  border-radius: var(--card-radius, 12px);
  padding: 1.25rem 1.5rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  transition: all var(--transition-smooth, 200ms);
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.game-card:hover {
  transform: translateY(-2px);
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4), 0 0 30px rgba(122, 76, 224, 0.15);
}

.game-info {
  flex: 1;
}

.game-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.game-header h3 {
  margin: 0;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-body, 'Inter', sans-serif);
  font-size: 1.1rem;
  font-weight: 600;
}

.result-win {
  color: #4CAF50;
  font-weight: 600;
  font-size: 0.85rem;
}

.result-loss {
  color: #f44336;
  font-weight: 600;
  font-size: 0.85rem;
}

.result-draw {
  color: #FFC107;
  font-weight: 600;
  font-size: 0.85rem;
}

.result-progress {
  color: #2196F3;
  font-weight: 600;
  font-size: 0.85rem;
}

.result-waiting {
  color: var(--cosmic-stars, #C5D4FF);
  font-weight: 600;
  font-size: 0.85rem;
}

.game-date {
  margin: 0 0 0.3rem 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.8rem;
  opacity: 0.8;
}

.game-players {
  margin: 0 0 0.3rem 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.85rem;
}

.game-players .you {
  font-weight: 600;
  color: var(--cosmic-figures, #F2F2F2);
}

.game-time-control {
  margin: 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.8rem;
  opacity: 0.7;
}

.game-actions {
  margin-left: 1rem;
}

.btn-sm {
  padding: 0.5rem 1rem;
  font-size: 0.85rem;
}

.btn-secondary {
  background: rgba(197, 212, 255, 0.1);
  border: 1px solid rgba(197, 212, 255, 0.2);
  color: var(--cosmic-stars, #C5D4FF);
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
  margin-bottom: 1rem;
  text-align: center;
}
</style>
