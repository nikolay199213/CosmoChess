package com.cosmochess.ui.game

import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.cosmochess.chess.ChessEngine
import com.cosmochess.network.SignalRManager
import com.cosmochess.ui.views.ChessBoardView
import kotlinx.coroutines.launch

class GameFragment : Fragment() {

    private val TAG = "GameFragment"
    private lateinit var gameId: String
    private var signalRManager: SignalRManager? = null

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    private val gameRepository by lazy {
        (requireActivity().application as ChessApplication).gameRepository
    }

    private val apiClient by lazy {
        (requireActivity().application as ChessApplication).apiClient
    }

    private lateinit var chessBoardView: ChessBoardView
    private lateinit var statusText: TextView
    private lateinit var progressBar: View

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        gameId = arguments?.getString("gameId") ?: ""
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_game, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        chessBoardView = view.findViewById(R.id.chessBoardView)
        statusText = view.findViewById(R.id.statusText)
        progressBar = view.findViewById(R.id.progressBar)

        setupChessBoard()
        connectToSignalR()
    }

    private fun setupChessBoard() {
        chessBoardView.onMoveMade = { from, to ->
            makeMove(from, to)
        }
    }

    private fun connectToSignalR() {
        val user = authRepository.getCurrentUser() ?: return
        val token = apiClient.getBaseUrl()

        signalRManager = SignalRManager(apiClient.getBaseUrl(), null).apply {
            onMoveMade = { receivedGameId, move, newFen ->
                if (receivedGameId == gameId) {
                    requireActivity().runOnUiThread {
                        Log.d(TAG, "Received move: $move, FEN: $newFen")
                        chessBoardView.setFEN(newFen)
                        updateStatus(newFen)
                    }
                }
            }

            onGameOver = { receivedGameId, result ->
                if (receivedGameId == gameId) {
                    requireActivity().runOnUiThread {
                        statusText.text = "Game Over: $result"
                        Toast.makeText(context, "Game Over: $result", Toast.LENGTH_LONG).show()
                    }
                }
            }

            onPlayerJoined = { receivedGameId, playerName ->
                if (receivedGameId == gameId) {
                    requireActivity().runOnUiThread {
                        Toast.makeText(context, "$playerName joined!", Toast.LENGTH_SHORT).show()
                    }
                }
            }

            connect(gameId)
        }
    }

    private fun makeMove(from: String, to: String) {
        val user = authRepository.getCurrentUser() ?: return

        progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            try {
                // Create new board state with the move
                val chessEngine = ChessEngine()
                val currentBoard = chessEngine.parseFEN(chessBoardView.getCurrentFEN())
                val newBoard = chessEngine.makeMove(currentBoard, from, to)

                if (newBoard == null) {
                    requireActivity().runOnUiThread {
                        Toast.makeText(context, "Invalid move", Toast.LENGTH_SHORT).show()
                        progressBar.visibility = View.GONE
                    }
                    return@launch
                }

                val newFen = chessEngine.boardToFEN(newBoard)
                val uciMove = "$from$to"

                // Send move to server
                val result = gameRepository.makeMove(
                    gameId = gameId,
                    userId = user.id,
                    move = uciMove,
                    newFen = newFen,
                    isCheckmate = false,
                    isStalemate = false,
                    isDraw = false
                )

                requireActivity().runOnUiThread {
                    progressBar.visibility = View.GONE

                    if (result.isSuccess) {
                        chessBoardView.setFEN(newFen)
                        updateStatus(newFen)
                    } else {
                        Toast.makeText(
                            context,
                            result.exceptionOrNull()?.message ?: getString(R.string.error_make_move),
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error making move", e)
                requireActivity().runOnUiThread {
                    progressBar.visibility = View.GONE
                    Toast.makeText(context, "Error: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    private fun updateStatus(fen: String) {
        val chessEngine = ChessEngine()
        val board = chessEngine.parseFEN(fen)
        statusText.text = if (board.whiteToMove) {
            getString(R.string.white_turn)
        } else {
            getString(R.string.black_turn)
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        signalRManager?.disconnect()
    }
}
