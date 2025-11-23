package com.cosmochess.data.repository

import android.content.Context
import com.cosmochess.data.model.*
import com.cosmochess.network.ApiClient
import com.google.gson.Gson

class AuthRepository(private val apiClient: ApiClient, private val context: Context) {
    private val prefs = context.getSharedPreferences("auth", Context.MODE_PRIVATE)
    private val gson = Gson()

    suspend fun login(username: String, password: String): Result<AuthResponse> {
        return try {
            val response = apiClient.apiService.login(LoginRequest(username, password))
            if (response.isSuccessful && response.body() != null) {
                val authResponse = response.body()!!
                saveAuth(authResponse)
                Result.success(authResponse)
            } else {
                Result.failure(Exception("Login failed: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun register(username: String, email: String, password: String): Result<AuthResponse> {
        return try {
            val response = apiClient.apiService.register(RegisterRequest(username, email, password))
            if (response.isSuccessful && response.body() != null) {
                val authResponse = response.body()!!
                saveAuth(authResponse)
                Result.success(authResponse)
            } else {
                Result.failure(Exception("Registration failed: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun isLoggedIn(): Boolean {
        return prefs.getString("token", null) != null
    }

    fun getCurrentUser(): User? {
        val userJson = prefs.getString("user", null) ?: return null
        return try {
            gson.fromJson(userJson, User::class.java)
        } catch (e: Exception) {
            null
        }
    }

    fun logout() {
        prefs.edit().clear().apply()
        apiClient.clearToken()
    }

    private fun saveAuth(authResponse: AuthResponse) {
        apiClient.saveToken(authResponse.token)
        prefs.edit()
            .putString("user", gson.toJson(authResponse.user))
            .apply()
    }
}
