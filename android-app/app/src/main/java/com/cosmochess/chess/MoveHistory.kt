package com.cosmochess.chess

/**
 * Tracks and formats move history
 */
class MoveHistory {
    private val moves = mutableListOf<String>()

    fun addMove(move: String) {
        moves.add(move)
    }

    fun clear() {
        moves.clear()
    }

    fun getFormattedHistory(): String {
        if (moves.isEmpty()) {
            return "No moves yet"
        }

        val sb = StringBuilder()
        for (i in moves.indices step 2) {
            val moveNumber = (i / 2) + 1
            val whiteMove = moves[i]
            val blackMove = if (i + 1 < moves.size) moves[i + 1] else ""

            sb.append(String.format("%2d. %-8s", moveNumber, formatMove(whiteMove)))
            if (blackMove.isNotEmpty()) {
                sb.append(formatMove(blackMove))
            }
            sb.append("\n")
        }

        return sb.toString()
    }

    private fun formatMove(uciMove: String): String {
        // Convert UCI format (e2e4) to algebraic notation (simplified)
        if (uciMove.length < 4) return uciMove

        val from = uciMove.substring(0, 2)
        val to = uciMove.substring(2, 4)

        // Simplified - just show from-to
        // In a real implementation, this would convert to proper algebraic notation
        return "$from-$to"
    }

    fun size(): Int = moves.size
}
