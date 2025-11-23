package com.cosmochess.network

import com.cosmochess.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface ApiService {
    @POST("api/auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    @GET("api/games/wait-join")
    suspend fun getWaitingGames(): Response<List<Game>>

    @POST("api/games/create")
    suspend fun createGame(@Body request: CreateGameRequest): Response<Game>

    @POST("api/games/vs-bot")
    suspend fun createBotGame(@Body request: CreateBotGameRequest): Response<Game>

    @POST("api/games/join")
    suspend fun joinGame(@Body request: JoinGameRequest): Response<Game>

    @POST("api/games/move")
    suspend fun makeMove(@Body request: MoveRequest): Response<MoveResponse>
}
