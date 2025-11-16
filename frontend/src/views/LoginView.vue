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

      <div class="divider">
        <span>OR</span>
      </div>

      <div class="google-signin-container">
        <div id="g_id_onload"
             data-client_id="YOUR_GOOGLE_CLIENT_ID"
             data-callback="handleGoogleCallback">
        </div>
        <div class="g_id_signin"
             data-type="standard"
             data-size="large"
             data-theme="filled_black"
             data-text="signin_with"
             data-shape="rectangular"
             data-logo_alignment="left"
             data-width="360">
        </div>
      </div>

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

    // Setup Google Sign-In callback
    window.handleGoogleCallback = this.handleGoogleCallback
  },
  beforeUnmount() {
    // Cleanup
    delete window.handleGoogleCallback
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
    },

    async handleGoogleCallback(response) {
      this.error = ''
      this.loading = true

      try {
        console.log('Google Sign-In response:', response)

        const result = await authService.googleAuth(response.credential)

        if (result.success) {
          await this.$nextTick()
          this.$router.replace('/games')
        } else {
          this.error = result.error
        }
      } catch (error) {
        this.error = 'Google authentication failed'
        console.error('Google auth error:', error)
      } finally {
        this.loading = false
      }
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
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.6) 0%,
    rgba(40, 50, 86, 0.4) 100%
  );
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: var(--card-radius, 12px);
  backdrop-filter: blur(10px);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 60px rgba(122, 76, 224, 0.1);
  padding: 2.5rem;
  width: 100%;
  max-width: 420px;
  transition: all var(--transition-smooth, 200ms);
}

.login-card:hover {
  border-color: rgba(122, 76, 224, 0.25);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 80px rgba(122, 76, 224, 0.15);
}

.login-title {
  text-align: center;
  margin-bottom: 2rem;
  color: var(--cosmic-figures, #F2F2F2);
  font-size: 1.8rem;
  font-family: var(--font-heading, 'Space Grotesk', sans-serif);
  font-weight: 700;
  text-shadow: 0 0 30px rgba(122, 76, 224, 0.3);
  letter-spacing: 0.5px;
}

.login-form {
  margin-bottom: 1.5rem;
}

.login-btn {
  width: 100%;
  margin-top: 1.5rem;
}

.login-btn:disabled {
  background: rgba(27, 35, 64, 0.6);
  cursor: not-allowed;
  opacity: 0.5;
  box-shadow: none;
}

.divider {
  display: flex;
  align-items: center;
  text-align: center;
  margin: 1.5rem 0;
  color: var(--cosmic-stars, #C5D4FF);
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  border-bottom: 1px solid rgba(197, 212, 255, 0.15);
}

.divider span {
  padding: 0 1rem;
  font-size: 0.875rem;
  font-weight: 500;
}

.google-signin-container {
  display: flex;
  justify-content: center;
  margin-bottom: 1.5rem;
}

.login-footer {
  text-align: center;
  padding-top: 1.5rem;
  border-top: 1px solid rgba(197, 212, 255, 0.1);
  color: var(--cosmic-stars, #C5D4FF);
}

.login-footer p {
  margin: 0;
  font-size: 0.95rem;
}

.toggle-btn {
  background: none;
  border: none;
  color: var(--cosmic-action-primary, #7A4CE0);
  cursor: pointer;
  text-decoration: none;
  font-size: inherit;
  font-weight: 500;
  margin-left: 0.5rem;
  padding: 0;
  transition: all var(--transition-smooth, 200ms);
}

.toggle-btn:hover {
  color: var(--cosmic-action-hover, #9464E8);
  text-shadow: 0 0 10px rgba(122, 76, 224, 0.5);
}

.toggle-btn:disabled {
  color: rgba(197, 212, 255, 0.3);
  cursor: not-allowed;
  text-shadow: none;
}
</style>