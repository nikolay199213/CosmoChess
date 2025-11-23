package com.cosmochess.network

import android.util.Log
import com.google.gson.Gson
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
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
            builder.withAccessTokenProvider { token }
        }

        hubConnection = builder.build()

        // Subscribe to events
        hubConnection?.on("ReceiveMove", { receivedGameId: String, move: String, newFen: String ->
            Log.d(TAG, "Received move for game $receivedGameId: $move")
            onMoveMade?.invoke(receivedGameId, move, newFen)
        }, String::class.java, String::class.java, String::class.java)

        hubConnection?.on("GameOver", { receivedGameId: String, result: String ->
            Log.d(TAG, "Game over for $receivedGameId: $result")
            onGameOver?.invoke(receivedGameId, result)
        }, String::class.java, String::class.java)

        hubConnection?.on("PlayerJoined", { receivedGameId: String, playerName: String ->
            Log.d(TAG, "Player joined game $receivedGameId: $playerName")
            onPlayerJoined?.invoke(receivedGameId, playerName)
        }, String::class.java, String::class.java)

        // Start connection
        CoroutineScope(Dispatchers.IO).launch {
            try {
                hubConnection?.start()?.blockingAwait()
                Log.d(TAG, "SignalR connected successfully")

                // Join game room
                hubConnection?.send("JoinGame", gameId)?.blockingAwait()
                Log.d(TAG, "Joined game room: $gameId")
            } catch (e: Exception) {
                Log.e(TAG, "Error connecting to SignalR: ${e.message}", e)
            }
        }
    }

    fun disconnect() {
        try {
            hubConnection?.stop()?.blockingAwait()
            Log.d(TAG, "SignalR disconnected")
        } catch (e: Exception) {
            Log.e(TAG, "Error disconnecting from SignalR: ${e.message}", e)
        }
    }

    fun isConnected(): Boolean {
        return hubConnection?.connectionState == HubConnectionState.CONNECTED
    }
}
