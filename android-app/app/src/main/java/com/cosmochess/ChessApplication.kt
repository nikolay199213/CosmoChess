package com.cosmochess

import android.app.Application
import com.cosmochess.data.repository.AuthRepository
import com.cosmochess.data.repository.GameRepository
import com.cosmochess.network.ApiClient

class ChessApplication : Application() {
    lateinit var apiClient: ApiClient
    lateinit var authRepository: AuthRepository
    lateinit var gameRepository: GameRepository

    override fun onCreate() {
        super.onCreate()
        instance = this

        apiClient = ApiClient(this)
        authRepository = AuthRepository(apiClient, this)
        gameRepository = GameRepository(apiClient, this)
    }

    companion object {
        lateinit var instance: ChessApplication
            private set
    }
}
