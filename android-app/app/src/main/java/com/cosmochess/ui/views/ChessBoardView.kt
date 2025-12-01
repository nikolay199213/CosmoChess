package com.cosmochess.ui.views

import android.content.Context
import android.graphics.*
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.View
import com.cosmochess.R
import com.cosmochess.chess.ChessEngine
import com.cosmochess.chess.LegalMovesCalculator

class ChessBoardView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private val chessEngine = ChessEngine()
    private val legalMovesCalculator = LegalMovesCalculator()
    private var boardState: ChessEngine.BoardState = chessEngine.parseFEN(ChessEngine.STARTING_FEN)

    private val lightSquarePaint = Paint().apply {
        color = context.getColor(R.color.board_light)
        style = Paint.Style.FILL
    }

    private val darkSquarePaint = Paint().apply {
        color = context.getColor(R.color.board_dark)
        style = Paint.Style.FILL
    }

    private val highlightPaint = Paint().apply {
        color = context.getColor(R.color.board_highlight)
        style = Paint.Style.FILL
        alpha = 150
    }

    private val selectedPaint = Paint().apply {
        color = context.getColor(R.color.board_selected)
        style = Paint.Style.FILL
        alpha = 150
    }

    private val textPaint = Paint().apply {
        color = Color.BLACK
        textAlign = Paint.Align.CENTER
        isAntiAlias = true
    }

    private var squareSize = 0f
    private var selectedSquare: String? = null
    private var highlightedSquares = emptyList<String>()

    var onMoveMade: ((from: String, to: String) -> Unit)? = null

    // Unicode chess pieces
    private val pieceSymbols = mapOf(
        'K' to "♔", 'Q' to "♕", 'R' to "♖", 'B' to "♗", 'N' to "♘", 'P' to "♙",
        'k' to "♚", 'q' to "♛", 'r' to "♜", 'b' to "♝", 'n' to "♞", 'p' to "♟"
    )

    fun setFEN(fen: String) {
        boardState = chessEngine.parseFEN(fen)
        selectedSquare = null
        highlightedSquares = emptyList()
        invalidate()
    }

    fun getCurrentFEN(): String {
        return chessEngine.boardToFEN(boardState)
    }

    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
        val size = minOf(
            MeasureSpec.getSize(widthMeasureSpec),
            MeasureSpec.getSize(heightMeasureSpec)
        )
        setMeasuredDimension(size, size)
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        squareSize = minOf(w, h) / 8f
        textPaint.textSize = squareSize * 0.7f
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)

        // Draw board squares
        for (rank in 0..7) {
            for (file in 0..7) {
                val isLight = (rank + file) % 2 == 0
                val paint = if (isLight) lightSquarePaint else darkSquarePaint

                val left = file * squareSize
                val top = (7 - rank) * squareSize

                canvas.drawRect(
                    left,
                    top,
                    left + squareSize,
                    top + squareSize,
                    paint
                )

                // Draw highlight
                val square = ChessEngine.Position(file, rank).toSquare()
                if (square == selectedSquare) {
                    canvas.drawRect(left, top, left + squareSize, top + squareSize, selectedPaint)
                } else if (square in highlightedSquares) {
                    canvas.drawRect(left, top, left + squareSize, top + squareSize, highlightPaint)
                }

                // Draw piece
                val piece = boardState.getPieceAt(file, rank)
                if (piece != null) {
                    val symbol = pieceSymbols[piece] ?: "?"
                    val x = left + squareSize / 2
                    val y = top + squareSize / 2 - (textPaint.descent() + textPaint.ascent()) / 2
                    canvas.drawText(symbol, x, y, textPaint)
                }
            }
        }
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        if (event.action == MotionEvent.ACTION_DOWN) {
            val file = (event.x / squareSize).toInt()
            val rank = 7 - (event.y / squareSize).toInt()

            if (file in 0..7 && rank in 0..7) {
                val square = ChessEngine.Position(file, rank).toSquare()
                handleSquareClick(square)
                return true
            }
        }
        return super.onTouchEvent(event)
    }

    private fun handleSquareClick(square: String) {
        if (selectedSquare == null) {
            // Select piece
            val pos = ChessEngine.Position.fromSquare(square) ?: return
            val piece = boardState.getPieceAt(pos.file, pos.rank) ?: return

            val isWhite = boardState.isWhitePiece(piece)
            if (isWhite == boardState.whiteToMove) {
                selectedSquare = square
                val legalMoves = legalMovesCalculator.getLegalMoves(boardState, pos)
                highlightedSquares = legalMoves.map { it.toSquare() }
                invalidate()
            }
        } else {
            // Make move
            if (square in highlightedSquares || square == selectedSquare) {
                if (square != selectedSquare) {
                    onMoveMade?.invoke(selectedSquare!!, square)
                }
            }
            selectedSquare = null
            highlightedSquares = emptyList()
            invalidate()
        }
    }
}
