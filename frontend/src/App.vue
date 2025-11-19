<template>
  <div id="app">
    <nav class="navbar">
      <div class="nav-container">
        <router-link to="/home" class="nav-title-link">
          <h1 class="nav-title">CosmoChess</h1>
        </router-link>
        <div class="nav-links" v-if="isLoggedIn">
          <router-link to="/home" class="nav-link">Home</router-link>
          <router-link to="/games" class="nav-link">Games</router-link>
          <button @click="logout" class="logout-btn">Logout</button>
        </div>
      </div>
    </nav>
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script>
import { authService } from './services/authService'

export default {
  name: 'App',
  computed: {
    isLoggedIn() {
      return authService.isLoggedIn.value
    }
  },
  methods: {
    logout() {
      authService.logout()
      this.$router.push('/login')
    }
  }
}
</script>

<style>
/* Import Cosmic Theme */
@import './assets/styles/cosmic-theme.css';

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: var(--font-body);
  background: var(--cosmic-bg-base);
  color: var(--cosmic-figures);
}

/* Cosmic Background with Stars */
body::before {
  content: '';
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(
    135deg,
    var(--cosmic-bg-base) 0%,
    var(--cosmic-deep-nebula) 50%,
    var(--cosmic-bg-base) 100%
  );
  background-attachment: fixed;
  z-index: -2;
}

body::after {
  content: '';
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image:
    radial-gradient(2px 2px at 20% 30%, rgba(197, 212, 255, 0.3), transparent),
    radial-gradient(2px 2px at 60% 70%, rgba(197, 212, 255, 0.2), transparent),
    radial-gradient(1px 1px at 50% 50%, rgba(197, 212, 255, 0.3), transparent),
    radial-gradient(1px 1px at 80% 10%, rgba(197, 212, 255, 0.2), transparent),
    radial-gradient(2px 2px at 90% 60%, rgba(197, 212, 255, 0.25), transparent),
    radial-gradient(1px 1px at 33% 80%, rgba(197, 212, 255, 0.2), transparent),
    radial-gradient(1px 1px at 15% 90%, rgba(197, 212, 255, 0.25), transparent),
    radial-gradient(2px 2px at 70% 20%, rgba(197, 212, 255, 0.2), transparent);
  background-size: 200% 200%;
  background-position: 0% 0%;
  pointer-events: none;
  z-index: -1;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  position: relative;
  z-index: 1;
}

/* Cosmic Navbar */
.navbar {
  background: linear-gradient(
    135deg,
    rgba(27, 35, 64, 0.8) 0%,
    rgba(40, 50, 86, 0.7) 100%
  );
  backdrop-filter: blur(10px);
  border-bottom: 1px solid rgba(197, 212, 255, 0.1);
  padding: 0.5rem 0;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.nav-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nav-title-link {
  text-decoration: none;
}

.nav-title {
  font-family: var(--font-heading);
  font-size: 1.3rem;
  font-weight: 700;
  color: var(--cosmic-figures);
  letter-spacing: 0.5px;
  text-shadow: 0 0 20px rgba(122, 76, 224, 0.4);
  transition: all var(--transition-smooth);
}

.nav-title-link:hover .nav-title {
  text-shadow: 0 0 30px rgba(122, 76, 224, 0.6);
}

.nav-links {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.nav-link {
  color: var(--cosmic-stars);
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  transition: all var(--transition-smooth);
  font-weight: 500;
}

.nav-link:hover {
  color: var(--cosmic-figures);
  background: rgba(122, 76, 224, 0.2);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.3);
}

.logout-btn {
  background: transparent;
  color: var(--cosmic-secondary-light);
  border: 1px solid rgba(230, 224, 255, 0.3);
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  transition: all var(--transition-smooth);
  font-family: var(--font-body);
  font-weight: 500;
}

.logout-btn:hover {
  border-color: rgba(101, 61, 137, 0.5);
  background: rgba(101, 61, 137, 0.2);
  box-shadow: 0 0 15px rgba(101, 61, 137, 0.3);
}

.main-content {
  flex: 1;
  padding: 1rem;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

@media (max-width: 768px) {
  .main-content {
    padding: 0.5rem;
  }

  .nav-title {
    font-size: 1.1rem;
  }

  .nav-container {
    padding: 0 0.5rem;
  }

  .nav-links {
    gap: 0.5rem;
  }

  .nav-link {
    padding: 0.4rem 0.8rem;
    font-size: 0.9rem;
  }

  .logout-btn {
    padding: 0.4rem 0.8rem;
    font-size: 0.9rem;
  }
}

/* Cosmic Buttons */
.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1rem;
  font-family: var(--font-body);
  font-weight: 500;
  transition: all var(--transition-smooth);
}

.btn-primary {
  background: linear-gradient(
    135deg,
    var(--cosmic-action-primary) 0%,
    var(--cosmic-nebula-glow-alt) 100%
  );
  color: var(--cosmic-figures);
  box-shadow: 0 4px 12px rgba(122, 76, 224, 0.2);
}

.btn-primary:hover {
  box-shadow: 0 6px 20px rgba(122, 76, 224, 0.4);
  transform: translateY(-2px);
  filter: brightness(1.1);
}

.btn-success {
  background: linear-gradient(
    135deg,
    var(--cosmic-action-primary) 0%,
    var(--cosmic-nebula-glow) 100%
  );
  color: var(--cosmic-figures);
  box-shadow: 0 4px 12px rgba(122, 76, 224, 0.2);
}

.btn-success:hover {
  box-shadow: 0 6px 20px rgba(122, 76, 224, 0.4);
  transform: translateY(-2px);
  filter: brightness(1.1);
}

.btn-secondary {
  background: transparent;
  color: var(--cosmic-secondary-light);
  border: 1px solid rgba(230, 224, 255, 0.3);
  box-shadow: none;
}

.btn-secondary:hover {
  border-color: var(--cosmic-action-primary);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.2);
  background: rgba(122, 76, 224, 0.1);
  transform: translateY(-2px);
}

/* Cosmic Form Groups */
.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--cosmic-stars);
  font-family: var(--font-body);
}

.form-group input {
  width: 100%;
  padding: 0.75rem;
  background: rgba(27, 35, 64, 0.4);
  border: 1px solid rgba(197, 212, 255, 0.15);
  border-radius: 8px;
  font-size: 1rem;
  color: var(--cosmic-figures);
  font-family: var(--font-body);
  transition: all var(--transition-smooth);
}

.form-group input::placeholder {
  color: rgba(197, 212, 255, 0.4);
}

.form-group input:focus {
  outline: none;
  border-color: var(--cosmic-action-primary);
  box-shadow: 0 0 15px rgba(122, 76, 224, 0.3);
  background: rgba(27, 35, 64, 0.6);
}

.error {
  color: var(--cosmic-stars);
  margin-top: 0.5rem;
  font-size: 0.9rem;
  background: rgba(101, 61, 137, 0.15);
  padding: 0.5rem;
  border-radius: 6px;
  border: 1px solid rgba(101, 61, 137, 0.3);
}
</style>