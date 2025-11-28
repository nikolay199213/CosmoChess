package com.cosmochess.data.repository

import android.content.Context
import com.cosmochess.data.model.*
import com.cosmochess.network.ApiClient

class GameRepository(private val apiClient: ApiClient, private val context: Context) {

    suspend fun getWaitingGames(): Result<List<Game>> {
        return try {
            val response = apiClient.apiService.getWaitingGames()
            if (response.isSuccessful && response.body() != null) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch games: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun createGame(creatorId: String): Result<String> {
        return try {
            val response = apiClient.apiService.createGame(CreateGameRequest(creatorId))
            if (response.isSuccessful && response.body() != null) {
                // Remove quotes from the response if present
                val gameId = response.body()!!.trim('"')
                Result.success(gameId)
            } else {
                Result.failure(Exception("Failed to create game: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun createBotGame(
        creatorId: String,
        difficulty: Int = 3,
        style: Int = 0
    ): Result<String> {
        return try {
            val response = apiClient.apiService.createBotGame(
                CreateBotGameRequest(creatorId, difficulty, style)
            )
            if (response.isSuccessful && response.body() != null) {
                // Remove quotes from the response if present
                val gameId = response.body()!!.trim('"')
                Result.success(gameId)
            } else {
                Result.failure(Exception("Failed to create bot game: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun joinGame(gameId: String, playerId: String): Result<Unit> {
        return try {
            val response = apiClient.apiService.joinGame(JoinGameRequest(gameId, playerId))
            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                Result.failure(Exception("Failed to join game: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getGameById(gameId: String): Result<Game> {
        return try {
            val response = apiClient.apiService.getGameById(gameId)
            if (response.isSuccessful && response.body() != null) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to get game: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun makeMove(
        gameId: String,
        userId: String,
        move: String,
        newFen: String,
        isCheckmate: Boolean = false,
        isStalemate: Boolean = false,
        isDraw: Boolean = false
    ): Result<Unit> {
        return try {
            val response = apiClient.apiService.makeMove(
                MoveRequest(gameId, userId, move, newFen, isCheckmate, isStalemate, isDraw)
            )
            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                Result.failure(Exception("Failed to make move: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
