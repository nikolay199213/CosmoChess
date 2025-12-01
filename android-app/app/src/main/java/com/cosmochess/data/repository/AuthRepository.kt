package com.cosmochess.data.repository

import android.content.Context
import android.util.Base64
import android.util.Log
import com.cosmochess.data.model.*
import com.cosmochess.network.ApiClient
import com.google.gson.Gson
import org.json.JSONObject

class AuthRepository(private val apiClient: ApiClient, private val context: Context) {
    private val prefs = context.getSharedPreferences("auth", Context.MODE_PRIVATE)
    private val gson = Gson()

    companion object {
        private const val TAG = "AuthRepository"
    }

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
            // First, register the user
            val registerResponse = apiClient.apiService.register(RegisterRequest(username, email, password))
            if (registerResponse.isSuccessful && registerResponse.body() != null) {
                Log.d(TAG, "Registration successful, userId: ${registerResponse.body()!!.userId}")
                // After successful registration, automatically log in (like frontend does)
                return login(username, password)
            } else {
                Result.failure(Exception("Registration failed: ${registerResponse.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun isLoggedIn(): Boolean {
        return prefs.getString("token", null) != null
    }

    fun getToken(): String? {
        return prefs.getString("token", null)
    }

    fun getCurrentUser(): User? {
        val userJson = prefs.getString("user", null)
        Log.d(TAG, "getCurrentUser - userJson: $userJson")
        if (userJson == null) {
            Log.d(TAG, "getCurrentUser - userJson is null, returning null")
            return null
        }
        return try {
            val user = gson.fromJson(userJson, User::class.java)
            Log.d(TAG, "getCurrentUser - successfully parsed user: $user")
            user
        } catch (e: Exception) {
            Log.e(TAG, "getCurrentUser - error parsing user", e)
            null
        }
    }

    fun logout() {
        prefs.edit().clear().apply()
        apiClient.clearToken()
    }

    private fun saveAuth(authResponse: AuthResponse) {
        Log.d(TAG, "saveAuth - token: ${authResponse.token}")

        // Save token
        apiClient.saveToken(authResponse.token)

        // Parse userId from token
        val userId = parseUserIdFromToken(authResponse.token)
        Log.d(TAG, "saveAuth - parsed userId: $userId")

        if (userId != null) {
            // Parse username from token as well
            val username = parseUsernameFromToken(authResponse.token) ?: "User"
            Log.d(TAG, "saveAuth - parsed username: $username")

            // Create user object with available information
            val user = User(
                id = userId,
                username = username,
                email = "" // Email is not stored in JWT, so we leave it empty
            )

            val userJson = gson.toJson(user)
            Log.d(TAG, "saveAuth - userJson: $userJson")

            prefs.edit()
                .putString("user", userJson)
                .apply()

            // Verify it was saved
            val savedUserJson = prefs.getString("user", null)
            Log.d(TAG, "saveAuth - verification - savedUserJson: $savedUserJson")
        } else {
            Log.e(TAG, "saveAuth - failed to parse userId from token")
        }
    }

    private fun parseUserIdFromToken(token: String): String? {
        return try {
            Log.d(TAG, "parseUserIdFromToken - parsing token")

            // JWT format: header.payload.signature
            val parts = token.split(".")
            if (parts.size != 3) {
                Log.e(TAG, "parseUserIdFromToken - invalid token format")
                return null
            }

            // Decode payload (second part)
            val payload = String(Base64.decode(parts[1], Base64.URL_SAFE or Base64.NO_WRAP))
            Log.d(TAG, "parseUserIdFromToken - payload: $payload")

            val jsonPayload = JSONObject(payload)
            Log.d(TAG, "parseUserIdFromToken - available claims: ${jsonPayload.keys().asSequence().toList()}")

            // Check various possible user ID claim names (same as frontend)
            val possibleUserIdClaims = listOf(
                "nameid",
                "sub",
                "userId",
                "user_id",
                "id",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata"
            )

            for (claim in possibleUserIdClaims) {
                if (jsonPayload.has(claim)) {
                    val userId = jsonPayload.getString(claim)
                    Log.d(TAG, "parseUserIdFromToken - found userId in claim '$claim': $userId")
                    return userId
                }
            }

            Log.e(TAG, "parseUserIdFromToken - no userId found in any expected claims")
            null
        } catch (e: Exception) {
            Log.e(TAG, "parseUserIdFromToken - error parsing token", e)
            null
        }
    }

    private fun parseUsernameFromToken(token: String): String? {
        return try {
            val parts = token.split(".")
            if (parts.size != 3) return null

            val payload = String(Base64.decode(parts[1], Base64.URL_SAFE or Base64.NO_WRAP))
            val jsonPayload = JSONObject(payload)

            // Check for username in various claims
            val possibleUsernameClaims = listOf(
                "unique_name",
                "name",
                "username",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
            )

            for (claim in possibleUsernameClaims) {
                if (jsonPayload.has(claim)) {
                    val username = jsonPayload.getString(claim)
                    Log.d(TAG, "parseUsernameFromToken - found username in claim '$claim': $username")
                    return username
                }
            }

            null
        } catch (e: Exception) {
            Log.e(TAG, "parseUsernameFromToken - error", e)
            null
        }
    }
}
