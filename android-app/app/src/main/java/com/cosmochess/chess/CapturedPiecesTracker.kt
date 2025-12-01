package com.cosmochess.chess

/**
 * Tracks captured pieces from FEN position changes
 */
class CapturedPiecesTracker {

    data class CapturedPieces(
        val whitePieces: List<Char> = emptyList(),
        val blackPieces: List<Char> = emptyList()
    )

    private val startingPieces = mapOf(
        'p' to 8, 'n' to 2, 'b' to 2, 'r' to 2, 'q' to 1, 'k' to 1,
        'P' to 8, 'N' to 2, 'B' to 2, 'R' to 2, 'Q' to 1, 'K' to 1
    )

    fun getCapturedPieces(currentFen: String): CapturedPieces {
        val currentPieces = countPiecesFromFen(currentFen)
        val whiteCaptured = mutableListOf<Char>()
        val blackCaptured = mutableListOf<Char>()

        // Count captured black pieces (lowercase)
        for (piece in "pnbrq") {
            val starting = startingPieces[piece] ?: 0
            val current = currentPieces[piece] ?: 0
            val captured = starting - current
            repeat(captured) {
                blackCaptured.add(piece)
            }
        }

        // Count captured white pieces (uppercase)
        for (piece in "PNBRQ") {
            val starting = startingPieces[piece] ?: 0
            val current = currentPieces[piece] ?: 0
            val captured = starting - current
            repeat(captured) {
                whiteCaptured.add(piece)
            }
        }

        return CapturedPieces(whiteCaptured, blackCaptured)
    }

    private fun countPiecesFromFen(fen: String): Map<Char, Int> {
        val position = fen.split(" ")[0]
        val counts = mutableMapOf<Char, Int>()

        for (char in position) {
            if (char.isLetter()) {
                counts[char] = counts.getOrDefault(char, 0) + 1
            }
        }

        return counts
    }

    fun formatCapturedPieces(pieces: List<Char>): String {
        val pieceSymbols = mapOf(
            'K' to "♔", 'Q' to "♕", 'R' to "♖", 'B' to "♗", 'N' to "♘", 'P' to "♙",
            'k' to "♚", 'q' to "♛", 'r' to "♜", 'b' to "♝", 'n' to "♞", 'p' to "♟"
        )

        return pieces.sortedByDescending { getPieceValue(it) }
            .joinToString(" ") { pieceSymbols[it] ?: it.toString() }
    }

    private fun getPieceValue(piece: Char): Int {
        return when (piece.lowercaseChar()) {
            'q' -> 9
            'r' -> 5
            'b' -> 3
            'n' -> 3
            'p' -> 1
            else -> 0
        }
    }

    fun calculateMaterialAdvantage(captured: CapturedPieces): Int {
        val whiteValue = captured.whitePieces.sumOf { getPieceValue(it) }
        val blackValue = captured.blackPieces.sumOf { getPieceValue(it) }
        return whiteValue - blackValue
    }
}
