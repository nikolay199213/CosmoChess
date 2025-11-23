package com.cosmochess.data.model

data class Game(
    val id: String,
    val whitePlayerId: String?,
    val blackPlayerId: String?,
    val whiteName: String?,
    val blackName: String?,
    val currentFen: String,
    val status: Int,
    val isBot: Boolean = false,
    val moveHistory: String? = null
)

data class CreateGameRequest(
    val creatorId: String
)

data class CreateBotGameRequest(
    val creatorId: String,
    val difficulty: Int = 3,  // 1-6 (Beginner to Master)
    val style: Int = 0,        // 0=Balanced, 1=Aggressive, 2=Solid
    val timeControl: String? = null
)

data class JoinGameRequest(
    val gameId: String,
    val playerId: String
)

data class MoveRequest(
    val gameId: String,
    val userId: String,
    val move: String,          // UCI format (e.g., "e2e4")
    val newFen: String,
    val isCheckmate: Boolean = false,
    val isStalemate: Boolean = false,
    val isDraw: Boolean = false
)

data class MoveResponse(
    val success: Boolean,
    val message: String? = null
)

enum class BotDifficulty(val value: Int, val displayName: String) {
    BEGINNER(1, "Beginner (400-600)"),
    EASY(2, "Easy (800-1000)"),
    MEDIUM(3, "Medium (1200-1400)"),
    HARD(4, "Hard (1600-1800)"),
    EXPERT(5, "Expert (2000-2200)"),
    MASTER(6, "Master (2400+)")
}

enum class BotStyle(val value: Int, val displayName: String) {
    BALANCED(0, "Balanced"),
    AGGRESSIVE(1, "Aggressive"),
    SOLID(2, "Solid")
}
