package com.cosmochess.ui.game

import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.cosmochess.chess.CapturedPiecesTracker
import com.cosmochess.chess.ChessEngine
import com.cosmochess.chess.MoveHistory
import com.cosmochess.network.SignalRManager
import com.cosmochess.ui.views.ChessBoardView
import kotlinx.coroutines.launch

class GameFragment : Fragment() {

    private val TAG = "GameFragment"
    private lateinit var gameId: String
    private var signalRManager: SignalRManager? = null
    private val capturedPiecesTracker = CapturedPiecesTracker()
    private val moveHistory = MoveHistory()

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
    private lateinit var topPlayerName: TextView
    private lateinit var topPlayerCaptured: TextView
    private lateinit var bottomPlayerName: TextView
    private lateinit var bottomPlayerCaptured: TextView
    private lateinit var moveHistoryText: TextView
    private lateinit var progressBar: ProgressBar

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
        topPlayerName = view.findViewById(R.id.topPlayerName)
        topPlayerCaptured = view.findViewById(R.id.topPlayerCaptured)
        bottomPlayerName = view.findViewById(R.id.bottomPlayerName)
        bottomPlayerCaptured = view.findViewById(R.id.bottomPlayerCaptured)
        moveHistoryText = view.findViewById(R.id.moveHistoryText)
        progressBar = view.findViewById(R.id.progressBar)

        val user = authRepository.getCurrentUser()
        bottomPlayerName.text = user?.username ?: "You"
        topPlayerName.text = "Opponent"

        setupChessBoard()
        connectToSignalR()
    }

    private fun setupChessBoard() {
        chessBoardView.onMoveMade = { from, to ->
            makeMove(from, to)
        }
        updateCapturedPieces()
    }

    private fun connectToSignalR() {
        signalRManager = SignalRManager(apiClient.getBaseUrl(), null).apply {
            onMoveMade = { receivedGameId, move, newFen ->
                if (receivedGameId == gameId) {
                    requireActivity().runOnUiThread {
                        Log.d(TAG, "Received move: $move, FEN: $newFen")
                        moveHistory.addMove(move)
                        chessBoardView.setFEN(newFen)
                        updateStatus(newFen)
                        updateCapturedPieces()
                        updateMoveHistory()
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
                        topPlayerName.text = playerName
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
                        moveHistory.addMove(uciMove)
                        chessBoardView.setFEN(newFen)
                        updateStatus(newFen)
                        updateCapturedPieces()
                        updateMoveHistory()
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

    private fun updateCapturedPieces() {
        val fen = chessBoardView.getCurrentFEN()
        val captured = capturedPiecesTracker.getCapturedPieces(fen)

        // Show pieces captured by white (black pieces that were captured)
        topPlayerCaptured.text = capturedPiecesTracker.formatCapturedPieces(captured.blackPieces)

        // Show pieces captured by black (white pieces that were captured)
        bottomPlayerCaptured.text = capturedPiecesTracker.formatCapturedPieces(captured.whitePieces)

        // Optionally show material advantage
        val advantage = capturedPiecesTracker.calculateMaterialAdvantage(captured)
        if (advantage > 0) {
            topPlayerCaptured.text = "${topPlayerCaptured.text} (+$advantage)"
        } else if (advantage < 0) {
            bottomPlayerCaptured.text = "${bottomPlayerCaptured.text} (+${-advantage})"
        }
    }

    private fun updateMoveHistory() {
        moveHistoryText.text = moveHistory.getFormattedHistory()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        signalRManager?.disconnect()
    }
}
