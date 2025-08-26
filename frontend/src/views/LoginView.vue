<template>
  <div class="login-container">
    <div class="login-card">
      <h2 class="login-title">{{ isLoginMode ? 'Login' : 'Register' }} to CosmoChess</h2>
      
      <form @submit.prevent="handleSubmit" class="login-form">
        <div class="form-group">
          <label for="username">Username</label>
          <input
            id="username"
            v-model="username"
            type="text"
            placeholder="Enter your username"
            required
            :disabled="loading"
          />
        </div>

        <div class="form-group">
          <label for="password">Password</label>
          <input
            id="password"
            v-model="password"
            type="password"
            placeholder="Enter your password"
            required
            :disabled="loading"
          />
        </div>

        <div v-if="!isLoginMode" class="form-group">
          <label for="confirmPassword">Confirm Password</label>
          <input
            id="confirmPassword"
            v-model="confirmPassword"
            type="password"
            placeholder="Confirm your password"
            required
            :disabled="loading"
          />
        </div>

        <div v-if="error" class="error">
          {{ error }}
        </div>

        <button 
          type="submit" 
          class="btn btn-primary login-btn"
          :disabled="loading || !isFormValid"
        >
          {{ loading ? 'Processing...' : (isLoginMode ? 'Login' : 'Register') }}
        </button>
      </form>

      <div class="login-footer">
        <p>
          {{ isLoginMode ? "Don't have an account?" : "Already have an account?" }}
          <button 
            @click="toggleMode" 
            class="toggle-btn"
            :disabled="loading"
          >
            {{ isLoginMode ? 'Register' : 'Login' }}
          </button>
        </p>
      </div>
    </div>
  </div>
</template>

<script>
import { authService } from '../services/authService'

export default {
  name: 'LoginView',
  data() {
    return {
      isLoginMode: true,
      username: '',
      password: '',
      confirmPassword: '',
      error: '',
      loading: false
    }
  },
  computed: {
    isFormValid() {
      if (!this.username || !this.password) return false
      if (!this.isLoginMode && this.password !== this.confirmPassword) return false
      return true
    }
  },
  mounted() {
    // Redirect if already logged in
    if (authService.isAuthenticated()) {
      this.$router.push('/games')
    }
  },
  methods: {
    async handleSubmit() {
      this.error = ''
      this.loading = true

      try {
        let result
        if (this.isLoginMode) {
          result = await authService.login(this.username, this.password)
        } else {
          if (this.password !== this.confirmPassword) {
            this.error = 'Passwords do not match'
            return
          }
          result = await authService.register(this.username, this.password)
        }

        if (result.success) {
          // Wait for Vue reactivity to update
          await this.$nextTick()
          // Use replace to avoid going back to login page
          this.$router.replace('/games')
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'An unexpected error occurred'
        console.error('Authentication error:', error)
      } finally {
        this.loading = false
      }
    },

    toggleMode() {
      this.isLoginMode = !this.isLoginMode
      this.error = ''
      this.confirmPassword = ''
    }
  }
}
</script>

<style scoped>
.login-container {
  min-height: calc(100vh - 80px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.login-card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  padding: 2rem;
  width: 100%;
  max-width: 400px;
}

.login-title {
  text-align: center;
  margin-bottom: 2rem;
  color: #2c3e50;
  font-size: 1.8rem;
}

.login-form {
  margin-bottom: 1.5rem;
}

.login-btn {
  width: 100%;
  margin-top: 1rem;
}

.login-btn:disabled {
  background-color: #bdc3c7;
  cursor: not-allowed;
}

.login-footer {
  text-align: center;
  border-top: 1px solid #ecf0f1;
  padding-top: 1rem;
}

.toggle-btn {
  background: none;
  border: none;
  color: #3498db;
  cursor: pointer;
  text-decoration: underline;
  font-size: inherit;
}

.toggle-btn:hover {
  color: #2980b9;
}

.toggle-btn:disabled {
  color: #bdc3c7;
  cursor: not-allowed;
}
</style>