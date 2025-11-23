package com.cosmochess.chess

/**
 * Calculates legal moves for chess pieces
 * This is a simplified implementation focused on basic move validation
 */
class LegalMovesCalculator {

    fun getLegalMoves(board: ChessEngine.BoardState, from: ChessEngine.Position): List<ChessEngine.Position> {
        val piece = board.getPieceAt(from.file, from.rank) ?: return emptyList()
        val isWhite = board.isWhitePiece(piece)

        // Check if it's the correct player's turn
        if (isWhite != board.whiteToMove) return emptyList()

        return when (piece.lowercaseChar()) {
            'p' -> getPawnMoves(board, from, isWhite)
            'n' -> getKnightMoves(board, from, isWhite)
            'b' -> getBishopMoves(board, from, isWhite)
            'r' -> getRookMoves(board, from, isWhite)
            'q' -> getQueenMoves(board, from, isWhite)
            'k' -> getKingMoves(board, from, isWhite)
            else -> emptyList()
        }
    }

    private fun getPawnMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val direction = if (isWhite) 1 else -1
        val startRank = if (isWhite) 1 else 6

        // Forward move
        val oneStep = ChessEngine.Position(from.file, from.rank + direction)
        if (isValid(oneStep) && board.getPieceAt(oneStep.file, oneStep.rank) == null) {
            moves.add(oneStep)

            // Two steps from starting position
            if (from.rank == startRank) {
                val twoSteps = ChessEngine.Position(from.file, from.rank + 2 * direction)
                if (board.getPieceAt(twoSteps.file, twoSteps.rank) == null) {
                    moves.add(twoSteps)
                }
            }
        }

        // Captures
        for (fileDelta in listOf(-1, 1)) {
            val capturePos = ChessEngine.Position(from.file + fileDelta, from.rank + direction)
            if (isValid(capturePos)) {
                val targetPiece = board.getPieceAt(capturePos.file, capturePos.rank)
                if (targetPiece != null && board.isWhitePiece(targetPiece) != isWhite) {
                    moves.add(capturePos)
                }
            }
        }

        return moves
    }

    private fun getKnightMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val knightMoves = listOf(
            Pair(-2, -1), Pair(-2, 1), Pair(-1, -2), Pair(-1, 2),
            Pair(1, -2), Pair(1, 2), Pair(2, -1), Pair(2, 1)
        )

        for ((fileDelta, rankDelta) in knightMoves) {
            val target = ChessEngine.Position(from.file + fileDelta, from.rank + rankDelta)
            if (isValid(target)) {
                val targetPiece = board.getPieceAt(target.file, target.rank)
                if (targetPiece == null || board.isWhitePiece(targetPiece) != isWhite) {
                    moves.add(target)
                }
            }
        }

        return moves
    }

    private fun getBishopMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val directions = listOf(Pair(-1, -1), Pair(-1, 1), Pair(1, -1), Pair(1, 1))

        for ((fileDelta, rankDelta) in directions) {
            addSlidingMoves(board, from, fileDelta, rankDelta, isWhite, moves)
        }

        return moves
    }

    private fun getRookMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val directions = listOf(Pair(0, -1), Pair(0, 1), Pair(-1, 0), Pair(1, 0))

        for ((fileDelta, rankDelta) in directions) {
            addSlidingMoves(board, from, fileDelta, rankDelta, isWhite, moves)
        }

        return moves
    }

    private fun getQueenMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val directions = listOf(
            Pair(-1, -1), Pair(-1, 0), Pair(-1, 1),
            Pair(0, -1), Pair(0, 1),
            Pair(1, -1), Pair(1, 0), Pair(1, 1)
        )

        for ((fileDelta, rankDelta) in directions) {
            addSlidingMoves(board, from, fileDelta, rankDelta, isWhite, moves)
        }

        return moves
    }

    private fun getKingMoves(board: ChessEngine.BoardState, from: ChessEngine.Position, isWhite: Boolean): List<ChessEngine.Position> {
        val moves = mutableListOf<ChessEngine.Position>()
        val directions = listOf(
            Pair(-1, -1), Pair(-1, 0), Pair(-1, 1),
            Pair(0, -1), Pair(0, 1),
            Pair(1, -1), Pair(1, 0), Pair(1, 1)
        )

        for ((fileDelta, rankDelta) in directions) {
            val target = ChessEngine.Position(from.file + fileDelta, from.rank + rankDelta)
            if (isValid(target)) {
                val targetPiece = board.getPieceAt(target.file, target.rank)
                if (targetPiece == null || board.isWhitePiece(targetPiece) != isWhite) {
                    moves.add(target)
                }
            }
        }

        return moves
    }

    private fun addSlidingMoves(
        board: ChessEngine.BoardState,
        from: ChessEngine.Position,
        fileDelta: Int,
        rankDelta: Int,
        isWhite: Boolean,
        moves: MutableList<ChessEngine.Position>
    ) {
        var current = ChessEngine.Position(from.file + fileDelta, from.rank + rankDelta)

        while (isValid(current)) {
            val piece = board.getPieceAt(current.file, current.rank)
            if (piece == null) {
                moves.add(current)
            } else {
                if (board.isWhitePiece(piece) != isWhite) {
                    moves.add(current)
                }
                break
            }
            current = ChessEngine.Position(current.file + fileDelta, current.rank + rankDelta)
        }
    }

    private fun isValid(pos: ChessEngine.Position): Boolean {
        return pos.file in 0..7 && pos.rank in 0..7
    }
}
