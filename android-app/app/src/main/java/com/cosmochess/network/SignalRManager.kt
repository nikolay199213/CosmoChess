package com.cosmochess.network

import android.util.Log
import com.google.gson.Gson
import com.google.gson.JsonObject
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import io.reactivex.rxjava3.core.Completable
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class SignalRManager(private val baseUrl: String, private val token: String?) {
    private val TAG = "SignalRManager"
    private var hubConnection: HubConnection? = null
    private val gson = Gson()

    var onMoveMade: ((gameId: String, move: String, newFen: String) -> Unit)? = null
    var onGameOver: ((gameId: String, result: String) -> Unit)? = null
    var onPlayerJoined: ((gameId: String, playerName: String) -> Unit)? = null

    fun connect(gameId: String) {
        if (hubConnection?.connectionState == HubConnectionState.CONNECTED) {
            Log.d(TAG, "Already connected")
            return
        }

        val hubUrl = "${baseUrl}api/gamehub"
        Log.d(TAG, "Connecting to SignalR hub: $hubUrl")

        val builder = HubConnectionBuilder.create(hubUrl)

        // Add authorization header if token is available
        if (token != null) {
            builder.withAccessTokenProvider(Single.just(token))
        }

        hubConnection = builder.build()

        // Subscribe to events (match backend event names)
        // MoveReceived event from backend
        hubConnection?.on("MoveReceived", { data: JsonObject ->
            try {
                val receivedGameId = data.get("gameId")?.asString ?: ""
                val move = data.get("move")?.asString ?: ""
                val newFen = data.get("newFen")?.asString ?: ""
                Log.d(TAG, "MoveReceived for game $receivedGameId: $move, FEN: $newFen")
                onMoveMade?.invoke(receivedGameId, move, newFen)
            } catch (e: Exception) {
                Log.e(TAG, "Error parsing MoveReceived event: ${e.message}", e)
            }
        }, JsonObject::class.java)

        // GameStateChanged event from backend
        hubConnection?.on("GameStateChanged", { data: JsonObject ->
            try {
                val receivedGameId = data.get("gameId")?.asString ?: ""
                val gameResult = data.get("gameResult")?.asInt ?: 0
                val endReason = data.get("endReason")?.asInt ?: 0
                Log.d(TAG, "GameStateChanged for $receivedGameId: result=$gameResult, reason=$endReason")
                onGameOver?.invoke(receivedGameId, "Result: $gameResult")
            } catch (e: Exception) {
                Log.e(TAG, "Error parsing GameStateChanged event: ${e.message}", e)
            }
        }, JsonObject::class.java)

        // PlayerJoined event from backend
        hubConnection?.on("PlayerJoined", { data: JsonObject ->
            try {
                val receivedGameId = data.get("gameId")?.asString ?: ""
                val username = data.get("username")?.asString ?: "Player"
                Log.d(TAG, "PlayerJoined game $receivedGameId: $username")
                onPlayerJoined?.invoke(receivedGameId, username)
            } catch (e: Exception) {
                Log.e(TAG, "Error parsing PlayerJoined event: ${e.message}", e)
            }
        }, JsonObject::class.java)

        // Start connection
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val connection = hubConnection
                if (connection != null) {
                    connection.start().blockingAwait()
                    Log.d(TAG, "SignalR connected successfully")

                    // Join game room
                    connection.send("JoinGame", gameId)
                    Log.d(TAG, "Joined game room: $gameId")
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error connecting to SignalR: ${e.message}", e)
            }
        }
    }

    fun disconnect() {
        try {
            val connection = hubConnection
            if (connection != null) {
                connection.stop().blockingAwait()
            }
            Log.d(TAG, "SignalR disconnected")
        } catch (e: Exception) {
            Log.e(TAG, "Error disconnecting from SignalR: ${e.message}", e)
        }
    }

    fun isConnected(): Boolean {
        return hubConnection?.connectionState == HubConnectionState.CONNECTED
    }
}