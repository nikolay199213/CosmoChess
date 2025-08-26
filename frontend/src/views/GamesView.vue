<template>
  <div class="games-container">
    <div class="games-header">
      <h1>Chess Games</h1>
      <button @click="createGame" class="btn btn-success" :disabled="loading">
        {{ loading ? 'Creating...' : 'Create New Game' }}
      </button>
    </div>

    <div v-if="error" class="error">
      {{ error }}
    </div>

    <div class="games-grid">
      <div class="games-section">
        <h2>Available Games</h2>
        <div v-if="loadingGames" class="loading">
          Loading games...
        </div>
        
        <div v-else-if="availableGames.length === 0" class="no-games">
          No games available. Create a new game to start playing!
        </div>
        
        <div v-else class="games-list">
          <div 
            v-for="game in availableGames" 
            :key="game.id"
            class="game-card"
          >
            <div class="game-info">
              <h3>Game #{{ game.id.substring(0, 8) }}</h3>
              <p>Created by: Player</p>
              <p>Status: {{ getGameStatus(game) }}</p>
              <p>FEN: {{ game.currentFen.substring(0, 30) }}...</p>
            </div>
            <div class="game-actions">
              <button 
                @click="joinGame(game.id)"
                class="btn btn-primary"
                :disabled="joiningGame === game.id"
              >
                {{ joiningGame === game.id ? 'Joining...' : 'Join Game' }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="games-section">
        <h2>Your Games</h2>
        <div v-if="userGames.length === 0" class="no-games">
          You haven't created any games yet.
        </div>
        
        <div v-else class="games-list">
          <div 
            v-for="game in userGames" 
            :key="game.id"
            class="game-card"
          >
            <div class="game-info">
              <h3>Game #{{ game.id.substring(0, 8) }}</h3>
              <p>Status: {{ getGameStatus(game) }}</p>
              <p>Players: {{ getPlayerCount(game) }}/2</p>
            </div>
            <div class="game-actions">
              <button 
                @click="playGame(game.id)"
                class="btn btn-primary"
                :disabled="!canPlayGame(game)"
              >
                {{ canPlayGame(game) ? 'Play' : 'Waiting for player' }}
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
      refreshInterval: null
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
        this.error = 'Failed to load games'
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
        console.log('Calling gameService.createGame()')
        const result = await gameService.createGame()
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
        this.error = 'Failed to create game'
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
        this.error = 'Failed to join game'
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
        case 0: return 'Waiting for player'
        case 1: return 'In progress'
        case 2: return 'White wins'
        case 3: return 'Black wins'
        case 4: return 'Draw'
        default: return 'Unknown'
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
  border-bottom: 2px solid #ecf0f1;
}

.games-header h1 {
  color: #2c3e50;
  margin: 0;
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
  color: #34495e;
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #ecf0f1;
}

.loading, .no-games {
  text-align: center;
  color: #7f8c8d;
  padding: 2rem;
  background: #f8f9fa;
  border-radius: 8px;
  font-style: italic;
}

.games-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.game-card {
  background: white;
  border: 1px solid #ecf0f1;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s, box-shadow 0.2s;
}

.game-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.game-info h3 {
  margin: 0 0 0.5rem 0;
  color: #2c3e50;
  font-size: 1.2rem;
}

.game-info p {
  margin: 0.25rem 0;
  color: #7f8c8d;
  font-size: 0.9rem;
}

.game-actions {
  margin-top: 1rem;
  text-align: right;
}

.game-actions .btn {
  min-width: 100px;
}
</style>