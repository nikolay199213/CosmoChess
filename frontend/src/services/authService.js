import axios from 'axios'
import { ref } from 'vue'

class AuthService {
  constructor() {
    this.token = localStorage.getItem('authToken')
    this.userId = localStorage.getItem('userId')
    
    // Make authentication state reactive
    this.isLoggedIn = ref(!!this.token)
    
    // Set up axios interceptor to include token in all requests
    axios.defaults.baseURL = '/api'
    axios.interceptors.request.use(
      (config) => {
        console.log('Axios request interceptor - token exists:', !!this.token)
        if (this.token) {
          config.headers.Authorization = `Bearer ${this.token}`
          console.log('Adding Authorization header:', config.headers.Authorization?.substring(0, 20) + '...')
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    // Handle 401 responses
    axios.interceptors.response.use(
      (response) => response,
      (error) => {
        console.log('Axios response interceptor - error status:', error.response?.status)
        if (error.response?.status === 401) {
          console.log('401 Unauthorized - logging out')
          this.logout()
          // Don't automatically redirect, let Vue router handle it
          // window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  async login(username, password) {
    try {
      console.log('Sending login request with:', { username, password: '***' })
      
      const response = await axios.post('/auth/login', {
        Username: username,
        Password: password
      })
      
      console.log('Full login response:', response)
      console.log('Response data:', response.data)
      console.log('Response status:', response.status)
      
      if (!response.data || (!response.data.Token && !response.data.token)) {
        console.error('Invalid response structure:', response.data)
        throw new Error('No token received from server')
      }
      
      // Handle both uppercase and lowercase token field names
      this.token = response.data.Token || response.data.token
      console.log('Token stored successfully:', this.token ? 'Yes' : 'No')
      
      this.userId = this.parseUserIdFromToken(this.token)
      console.log('Parsed userId:', this.userId)
      
      localStorage.setItem('authToken', this.token)
      if (this.userId) {
        localStorage.setItem('userId', this.userId)
      }
      
      // Update reactive state
      this.isLoggedIn.value = true
      console.log('Authentication state updated')
      
      return { success: true }
    } catch (error) {
      console.error('Login error:', error)
      console.error('Error response:', error.response)
      return { 
        success: false, 
        error: error.response?.data?.error || error.message || 'Login failed' 
      }
    }
  }

  async register(username, password) {
    try {
      const response = await axios.post('/auth/register', {
        UserName: username,
        Password: password
      })

      // After successful registration, automatically log in
      return await this.login(username, password)
    } catch (error) {
      console.error('Registration error:', error)
      return {
        success: false,
        error: error.response?.data?.error || 'Registration failed'
      }
    }
  }

  async googleAuth(idToken) {
    try {
      console.log('Sending Google auth request')

      const response = await axios.post('/auth/google', {
        IdToken: idToken
      })

      console.log('Google auth response:', response)

      if (!response.data || (!response.data.Token && !response.data.token)) {
        console.error('Invalid response structure:', response.data)
        throw new Error('No token received from server')
      }

      this.token = response.data.Token || response.data.token
      console.log('Token stored successfully:', this.token ? 'Yes' : 'No')

      this.userId = this.parseUserIdFromToken(this.token)
      console.log('Parsed userId:', this.userId)

      localStorage.setItem('authToken', this.token)
      if (this.userId) {
        localStorage.setItem('userId', this.userId)
      }

      this.isLoggedIn.value = true
      console.log('Authentication state updated')

      return { success: true }
    } catch (error) {
      console.error('Google auth error:', error)
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Google authentication failed'
      }
    }
  }

  logout() {
    this.token = null
    this.userId = null
    localStorage.removeItem('authToken')
    localStorage.removeItem('userId')
    
    // Update reactive state
    this.isLoggedIn.value = false
  }

  isAuthenticated() {
    return this.isLoggedIn.value
  }

  getUserId() {
    return this.userId
  }

  getToken() {
    return this.token
  }

  parseUserIdFromToken(token) {
    try {
      if (!token) {
        console.log('No token provided for parsing')
        return null
      }
      
      const payload = JSON.parse(atob(token.split('.')[1]))
      console.log('JWT payload (full):', payload)
      console.log('Available claims:', Object.keys(payload))
      
      // Check various possible user ID claim names
      const possibleUserIdClaims = [
        'nameid', 'sub', 'userId', 'user_id', 'id',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata'
      ]
      
      let userId = null
      for (const claim of possibleUserIdClaims) {
        if (payload[claim]) {
          userId = payload[claim]
          console.log(`Found userId in claim '${claim}':`, userId)
          break
        }
      }
      
      if (!userId) {
        console.log('No userId found in any expected claims')
      }
      
      return userId
    } catch (error) {
      console.error('Error parsing token:', error)
      return null
    }
  }
}

export const authService = new AuthService()