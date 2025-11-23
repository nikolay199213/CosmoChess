/**
 * Utility functions for calculating captured pieces and material advantage
 */

// Piece values in centipawns
const PIECE_VALUES = {
  p: 100, // pawn
  n: 320, // knight
  b: 330, // bishop
  r: 500, // rook
  q: 900, // queen
  k: 0    // king (not counted for material)
}

/**
 * Get the starting position piece counts for each side
 */
function getStartingPieces() {
  return {
    p: 8,
    n: 2,
    b: 2,
    r: 2,
    q: 1,
    k: 1
  }
}

/**
 * Count pieces on the board from FEN string
 * @param {string} fen - FEN notation string
 * @returns {Object} - Object with white and black piece counts
 */
export function countPiecesFromFen(fen) {
  const position = fen.split(' ')[0] // Get just the board position part

  const white = { p: 0, n: 0, b: 0, r: 0, q: 0, k: 0 }
  const black = { p: 0, n: 0, b: 0, r: 0, q: 0, k: 0 }

  for (const char of position) {
    if (char === '/' || /\d/.test(char)) {
      continue // Skip row separators and empty square numbers
    }

    const piece = char.toLowerCase()
    if (char === char.toUpperCase()) {
      // White piece
      white[piece] = (white[piece] || 0) + 1
    } else {
      // Black piece
      black[piece] = (black[piece] || 0) + 1
    }
  }

  return { white, black }
}

/**
 * Calculate captured pieces for both sides
 * @param {string} fen - Current FEN notation
 * @returns {Object} - Object with captured pieces for white and black
 */
export function getCapturedPieces(fen) {
  const starting = getStartingPieces()
  const current = countPiecesFromFen(fen)

  const capturedByWhite = {} // Pieces that white captured (black pieces)
  const capturedByBlack = {} // Pieces that black captured (white pieces)

  // Calculate what white captured (missing black pieces)
  for (const piece of ['p', 'n', 'b', 'r', 'q']) {
    const captured = starting[piece] - (current.black[piece] || 0)
    if (captured > 0) {
      capturedByWhite[piece] = captured
    }
  }

  // Calculate what black captured (missing white pieces)
  for (const piece of ['p', 'n', 'b', 'r', 'q']) {
    const captured = starting[piece] - (current.white[piece] || 0)
    if (captured > 0) {
      capturedByBlack[piece] = captured
    }
  }

  return {
    capturedByWhite,
    capturedByBlack
  }
}

/**
 * Calculate material value for a set of pieces
 * @param {Object} pieces - Object with piece counts
 * @returns {number} - Total material value in centipawns
 */
function calculateMaterialValue(pieces) {
  let total = 0
  for (const [piece, count] of Object.entries(pieces)) {
    total += PIECE_VALUES[piece] * count
  }
  return total
}

/**
 * Calculate material advantage
 * @param {string} fen - Current FEN notation
 * @returns {Object} - Material advantage info
 */
export function getMaterialAdvantage(fen) {
  const current = countPiecesFromFen(fen)

  const whiteValue = calculateMaterialValue(current.white)
  const blackValue = calculateMaterialValue(current.black)

  const difference = whiteValue - blackValue

  return {
    whiteValue,
    blackValue,
    advantage: Math.abs(difference),
    side: difference > 0 ? 'white' : difference < 0 ? 'black' : 'equal'
  }
}

/**
 * Get piece symbols for display (Unicode chess symbols)
 * @param {string} piece - Piece letter (lowercase)
 * @param {string} color - 'white' or 'black'
 * @returns {string} - Unicode symbol
 */
export function getPieceSymbol(piece, color) {
  const symbols = {
    white: {
      p: '♙',
      n: '♘',
      b: '♗',
      r: '♖',
      q: '♕',
      k: '♔'
    },
    black: {
      p: '♟',
      n: '♞',
      b: '♝',
      r: '♜',
      q: '♛',
      k: '♚'
    }
  }

  return symbols[color][piece] || ''
}

/**
 * Sort captured pieces by value (highest to lowest)
 * @param {Object} captured - Captured pieces object
 * @returns {Array} - Sorted array of [piece, count] entries
 */
export function sortCapturedPieces(captured) {
  return Object.entries(captured).sort((a, b) => {
    return PIECE_VALUES[b[0]] - PIECE_VALUES[a[0]]
  })
}
