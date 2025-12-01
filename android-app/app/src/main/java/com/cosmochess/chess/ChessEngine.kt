package com.cosmochess.chess

/**
 * Simple chess engine for move validation and FEN parsing
 * This is a lightweight implementation for Android
 */
class ChessEngine {

    companion object {
        const val STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
    }

    data class Position(val file: Int, val rank: Int) {
        fun toSquare(): String = "${'a' + file}${rank + 1}"

        companion object {
            fun fromSquare(square: String): Position? {
                if (square.length != 2) return null
                val file = square[0] - 'a'
                val rank = square[1] - '1'
                if (file !in 0..7 || rank !in 0..7) return null
                return Position(file, rank)
            }
        }
    }

    data class BoardState(
        val pieces: Array<CharArray>, // 8x8 board, 'K'=white king, 'k'=black king, etc.
        val whiteToMove: Boolean,
        val castling: String,
        val enPassant: String?,
        val halfmove: Int,
        val fullmove: Int
    ) {
        fun getPieceAt(file: Int, rank: Int): Char? {
            if (file !in 0..7 || rank !in 0..7) return null
            return pieces[rank][file].takeIf { it != ' ' }
        }

        fun isWhitePiece(piece: Char): Boolean = piece.isUpperCase()
    }

    fun parseFEN(fen: String): BoardState {
        val parts = fen.split(" ")
        val position = parts[0]
        val activeColor = parts.getOrNull(1) ?: "w"
        val castling = parts.getOrNull(2) ?: "-"
        val enPassant = parts.getOrNull(3)?.takeIf { it != "-" }
        val halfmove = parts.getOrNull(4)?.toIntOrNull() ?: 0
        val fullmove = parts.getOrNull(5)?.toIntOrNull() ?: 1

        val pieces = Array(8) { CharArray(8) { ' ' } }
        val ranks = position.split("/")

        for ((rankIndex, rankStr) in ranks.withIndex()) {
            var fileIndex = 0
            for (char in rankStr) {
                if (char.isDigit()) {
                    fileIndex += char.toString().toInt()
                } else {
                    pieces[7 - rankIndex][fileIndex] = char
                    fileIndex++
                }
            }
        }

        return BoardState(
            pieces = pieces,
            whiteToMove = activeColor == "w",
            castling = castling,
            enPassant = enPassant,
            halfmove = halfmove,
            fullmove = fullmove
        )
    }

    fun boardToFEN(board: BoardState): String {
        val position = StringBuilder()

        for (rank in 7 downTo 0) {
            var emptyCount = 0
            for (file in 0..7) {
                val piece = board.pieces[rank][file]
                if (piece == ' ') {
                    emptyCount++
                } else {
                    if (emptyCount > 0) {
                        position.append(emptyCount)
                        emptyCount = 0
                    }
                    position.append(piece)
                }
            }
            if (emptyCount > 0) {
                position.append(emptyCount)
            }
            if (rank > 0) {
                position.append('/')
            }
        }

        val activeColor = if (board.whiteToMove) "w" else "b"
        return "$position $activeColor ${board.castling} ${board.enPassant ?: "-"} ${board.halfmove} ${board.fullmove}"
    }

    fun makeMove(board: BoardState, from: String, to: String): BoardState? {
        val fromPos = Position.fromSquare(from) ?: return null
        val toPos = Position.fromSquare(to) ?: return null

        val piece = board.getPieceAt(fromPos.file, fromPos.rank) ?: return null
        val isWhite = board.isWhitePiece(piece)

        // Check if it's the correct player's turn
        if (isWhite != board.whiteToMove) return null

        // Create new board with the move
        val newPieces = board.pieces.map { it.clone() }.toTypedArray()
        newPieces[toPos.rank][toPos.file] = piece
        newPieces[fromPos.rank][fromPos.file] = ' '

        // Handle pawn promotion (simplified - always promote to queen)
        if (piece.lowercaseChar() == 'p' && (toPos.rank == 0 || toPos.rank == 7)) {
            newPieces[toPos.rank][toPos.file] = if (isWhite) 'Q' else 'q'
        }

        return BoardState(
            pieces = newPieces,
            whiteToMove = !board.whiteToMove,
            castling = board.castling, // Simplified - not updating castling rights
            enPassant = null, // Simplified
            halfmove = board.halfmove + 1,
            fullmove = if (isWhite) board.fullmove else board.fullmove + 1
        )
    }

    fun getLegalMoves(board: BoardState, from: String): List<String> {
        // Simplified implementation - returns all squares as potential moves
        // In a real implementation, this would calculate actual legal moves
        val fromPos = Position.fromSquare(from) ?: return emptyList()
        val piece = board.getPieceAt(fromPos.file, fromPos.rank) ?: return emptyList()

        val isWhite = board.isWhitePiece(piece)
        if (isWhite != board.whiteToMove) return emptyList()

        // For simplicity, return all empty squares and opponent pieces
        val moves = mutableListOf<String>()
        for (rank in 0..7) {
            for (file in 0..7) {
                val targetPiece = board.getPieceAt(file, rank)
                if (targetPiece == null || board.isWhitePiece(targetPiece) != isWhite) {
                    moves.add(Position(file, rank).toSquare())
                }
            }
        }
        return moves
    }
}
