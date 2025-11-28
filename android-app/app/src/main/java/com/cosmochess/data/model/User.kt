package com.cosmochess.data.model

data class User(
    val id: String,
    val username: String,
    val email: String
)

data class LoginRequest(
    val username: String,
    val password: String
)

data class RegisterRequest(
    val username: String,
    val email: String,
    val password: String
)

// Login response (only contains token, userId is parsed from JWT)
data class AuthResponse(
    val token: String
)

// Register response (only contains userId)
data class RegisterResponse(
    val userId: String
)
