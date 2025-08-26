import axios from 'axios'
import { authService } from './authService'

class GameService {
  async getGamesWaitingForJoin() {
    try {
      console.log('Fetching games waiting for join...')
      console.log('Auth token:', authService.getToken()?.substring(0, 20) + '...')
      console.log('User ID:', authService.getUserId())
      
      const response = await axios.get('/games/wait-join')
      console.log('Games response:', response.data)
      return { success: true, games: response.data }
    } catch (error) {
      console.error('Error fetching games:', error)
      console.error('Error response:', error.response)
      console.error('Error status:', error.response?.status)
      console.error('Error data:', error.response?.data)
      return { 
        success: false, 
        error: error.response?.data?.error || 'Failed to fetch games' 
      }
    }
  }

  async createGame() {
    try {
      const userId = authService.getUserId()
      console.log('Creating game with userId:', userId)
      
      if (!userId) {
        console.error('User not authenticated - no userId found')
        throw new Error('User not authenticated')
      }

      console.log('Sending create game request...')
      const response = await axios.post('/games/create', {
        CreatorId: userId
      })
      
      console.log('Create game response:', response.data)
      return { success: true, gameId: response.data }
    } catch (error) {
      console.error('Error creating game:', error)
      console.error('Error response:', error.response)
      return { 
        success: false, 
        error: error.response?.data?.error || error.message || 'Failed to create game' 
      }
    }
  }

  async joinGame(gameId) {
    try {
      const userId = authService.getUserId()
      if (!userId) {
        throw new Error('User not authenticated')
      }

      await axios.post('/games/join', {
        GameId: gameId,
        PlayerId: userId
      })
      
      return { success: true }
    } catch (error) {
      console.error('Error joining game:', error)
      return { 
        success: false, 
        error: error.response?.data?.error || 'Failed to join game' 
      }
    }
  }

  async makeMove(gameId, move, newFen) {
    try {
      const userId = authService.getUserId()
      if (!userId) {
        throw new Error('User not authenticated')
      }

      await axios.post('/games/move', {
        GameId: gameId,
        UserId: userId,
        Move: move,
        NewFen: newFen
      })
      
      return { success: true }
    } catch (error) {
      console.error('Error making move:', error)
      return { 
        success: false, 
        error: error.response?.data?.error || 'Failed to make move' 
      }
    }
  }

  async analyzePosition(fen, depth = 10) {
    try {
      const response = await axios.post('/games/analyze', {
        Fen: fen,
        Depth: depth
      })
      
      return { success: true, bestMove: response.data.bestMove }
    } catch (error) {
      console.error('Error analyzing position:', error)
      return { 
        success: false, 
        error: error.response?.data?.error || 'Failed to analyze position' 
      }
    }
  }
}

export const gameService = new GameService()