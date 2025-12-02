<template>
  <div class="games-container">
    <div class="games-header">
      <h1>{{ $t('games.title') }}</h1>
      <div class="header-controls">
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
        <button @click="createGame" class="btn btn-success" :disabled="loading">
          {{ loading ? $t('games.creating') : $t('games.createNewGame') }}
        </button>
      </div>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <div class="games-grid">
      <div class="games-section">
        <h2>{{ $t('games.availableGames') }}</h2>
        <div v-if="loadingGames" class="loading">
          {{ $t('games.loadingGames') }}
        </div>

        <div v-else-if="availableGames.length === 0" class="no-games">
          {{ $t('games.noGamesAvailable') }}
        </div>

        <div v-else class="games-list">
          <div
            v-for="game in availableGames"
            :key="game.id"
            class="game-card"
          >
            <div class="game-info">
              <h3>Game #{{ game.id.substring(0, 8) }}</h3>
              <p>{{ $t('games.createdBy') }}</p>
              <p>{{ $t('games.status', { status: getGameStatus(game) }) }}</p>
              <p>FEN: {{ game.currentFen.substring(0, 30) }}...</p>
            </div>
            <div class="game-actions">
              <button
                @click="joinGame(game.id)"
                class="btn btn-primary"
                :disabled="joiningGame === game.id"
              >
                {{ joiningGame === game.id ? $t('games.joining') : $t('games.joinGame') }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="games-section">
        <h2>{{ $t('games.yourGames') }}</h2>
        <div v-if="userGames.length === 0" class="no-games">
          {{ $t('games.noGamesCreated') }}
        </div>

        <div v-else class="games-list">
          <div
            v-for="game in userGames"
            :key="game.id"
            class="game-card"
          >
            <div class="game-info">
              <h3>Game #{{ game.id.substring(0, 8) }}</h3>
              <p>{{ $t('games.status', { status: getGameStatus(game) }) }}</p>
              <p>Players: {{ getPlayerCount(game) }}/2</p>
            </div>
            <div class="game-actions">
              <button
                @click="playGame(game.id)"
                class="btn btn-primary"
                :disabled="!canPlayGame(game)"
              >
                {{ canPlayGame(game) ? $t('games.play') : $t('games.waitingForPlayer') }}
              </button>
            </div>
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
  name: 'GamesView',
  data() {
    return {
      games: [],
      loading: false,
      loadingGames: false,
      joiningGame: null,
      error: '',
      refreshInterval: null,
      selectedTimeControl: 0
    }
  },
  computed: {
    availableGames() {
      const userId = authService.getUserId()
      console.log('Filtering games for userId:', userId)
      const available = this.games.filter(game => {
        const isNotCreator = game.whitePlayerId !== userId
        // Check for null, undefined, or empty GUID (all zeros)
        const emptyGuid = '00000000-0000-0000-0000-000000000000'
        const hasNoSecondPlayer = !game.blackPlayerId || game.blackPlayerId === emptyGuid
        const isWaitingForPlayer = game.gameResult === 0 // GameResult.WaitJoin
        
        console.log(`Game ${game.id}: creator=${game.whitePlayerId}, black=${game.blackPlayerId}, result=${game.gameResult}`)
        console.log(`  - isNotCreator: ${isNotCreator}, hasNoSecondPlayer: ${hasNoSecondPlayer}, isWaiting: ${isWaitingForPlayer}`)
        
        return isNotCreator && hasNoSecondPlayer && isWaitingForPlayer
      })
      console.log('Available games count:', available.length)
      return available
    },
    userGames() {
      const userId = authService.getUserId()
      const userCreated = this.games.filter(game => game.whitePlayerId === userId)
      console.log('User games count:', userCreated.length)
      return userCreated
    }
  },
  async mounted() {
    console.log('GamesView mounted')
    console.log('Is authenticated:', authService.isAuthenticated())
    console.log('User ID:', authService.getUserId())
    console.log('Token exists:', !!authService.getToken())
    
    // Ensure authentication is ready
    if (!authService.isAuthenticated()) {
      console.log('Not authenticated, redirecting to login')
      this.$router.replace('/login')
      return
    }
    
    await this.loadGames()
    // Refresh games every 5 seconds
    this.refreshInterval = setInterval(() => {
      this.loadGames()
    }, 5000)
  },
  beforeUnmount() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval)
    }
  },
  methods: {
    async loadGames() {
      this.loadingGames = true
      this.error = ''

      try {
        const result = await gameService.getGamesWaitingForJoin()
        if (result.success) {
          console.log('Games loaded successfully:', result.games)
          console.log('Number of games:', result.games.length)
          if (result.games.length > 0) {
            console.log('First game structure:', result.games[0])
            console.log('Game properties:', Object.keys(result.games[0]))
          }
          this.games = result.games
          console.log('Available games after filter:', this.availableGames.length)
          console.log('User games after filter:', this.userGames.length)
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = this.$t('games.failedToLoadGames')
        console.error('Error loading games:', error)
      } finally {
        this.loadingGames = false
      }
    },

    async createGame() {
      console.log('CreateGame button clicked')
      this.loading = true
      this.error = ''

      try {
        console.log('Calling gameService.createGame() with timeControl:', this.selectedTimeControl)
        const result = await gameService.createGame(parseInt(this.selectedTimeControl))
        console.log('CreateGame result:', result)

        if (result.success) {
          console.log('Game created successfully, gameId:', result.gameId)
          await this.loadGames()
          // Navigate to the created game
          this.$router.push(`/game/${result.gameId}`)
        } else {
          console.error('Failed to create game:', result.error)
          this.error = result.error
        }
      } catch (error) {
        console.error('Exception in createGame:', error)
        this.error = this.$t('games.failedToCreateGame')
        console.error('Error creating game:', error)
      } finally {
        this.loading = false
      }
    },

    async joinGame(gameId) {
      this.joiningGame = gameId
      this.error = ''

      try {
        const result = await gameService.joinGame(gameId)
        if (result.success) {
          await this.loadGames()
          // Navigate to the joined game
          this.$router.push(`/game/${gameId}`)
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = this.$t('games.failedToJoinGame')
        console.error('Error joining game:', error)
      } finally {
        this.joiningGame = null
      }
    },

    playGame(gameId) {
      this.$router.push(`/game/${gameId}`)
    },

    getGameStatus(game) {
      switch (game.gameResult) {
        case 0: return this.$t('gameStatus.waitingForPlayer')
        case 1: return this.$t('gameStatus.inProgress')
        case 2: return this.$t('gameStatus.whiteWins')
        case 3: return this.$t('gameStatus.blackWins')
        case 4: return this.$t('gameStatus.draw')
        default: return this.$t('gameStatus.unknown')
      }
    },

    getPlayerCount(game) {
      let count = 1 // White player (creator)
      const emptyGuid = '00000000-0000-0000-0000-000000000000'
      if (game.blackPlayerId && game.blackPlayerId !== emptyGuid) count++
      return count
    },

    canPlayGame(game) {
      const emptyGuid = '00000000-0000-0000-0000-000000000000'
      return game.blackPlayerId && game.blackPlayerId !== emptyGuid
    }
  }
}
</script>

<style scoped>
.games-container {
  max-width: 1200px;
  margin: 0 auto;
}

.games-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid rgba(197, 212, 255, 0.2);
}

.games-header h1 {
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 700;
  font-size: 2rem;
  margin: 0;
  text-shadow: 0 0 20px rgba(122, 76, 224, 0.3);
}

.header-controls {
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
  font-size: 1rem;
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

.games-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
}

@media (max-width: 768px) {
  .games-grid {
    grid-template-columns: 1fr;
  }
}

.games-section h2 {
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
  padding: 1.5rem;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  transition: all var(--transition-smooth, 200ms);
}

.game-card:hover {
  transform: translateY(-2px);
  border-color: rgba(122, 76, 224, 0.3);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4), 0 0 30px rgba(122, 76, 224, 0.15);
}

.game-info h3 {
  margin: 0 0 0.75rem 0;
  color: var(--cosmic-figures, #F2F2F2);
  font-family: var(--font-body, 'Inter', sans-serif);
  font-size: 1.2rem;
  font-weight: 600;
}

.game-info p {
  margin: 0.4rem 0;
  color: var(--cosmic-stars, #C5D4FF);
  font-size: 0.9rem;
  font-family: var(--font-body, 'Inter', sans-serif);
}

.game-actions {
  margin-top: 1.25rem;
  text-align: right;
}

.game-actions .btn {
  min-width: 120px;
}
</style>