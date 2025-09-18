using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a Pawn chess piece
    /// </summary>
    public partial class Pawn : ChessPiece
    {
        public Pawn(PieceColor color, Vector2I position) : base(PieceType.Pawn, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();
            int direction = Color == PieceColor.White ? 1 : -1; // White moves up (increasing rank), Black moves down
            
            // Forward movement
            var oneSquareForward = new Vector2I(BoardPosition.X + direction, BoardPosition.Y);
            if (IsSquareEmpty(board, oneSquareForward))
            {
                moves.Add(oneSquareForward);
                
                // Two squares forward on first move
                if (!HasMoved)
                {
                    var twoSquaresForward = new Vector2I(BoardPosition.X + 2 * direction, BoardPosition.Y);
                    if (IsSquareEmpty(board, twoSquaresForward))
                    {
                        moves.Add(twoSquaresForward);
                    }
                }
            }

            // Diagonal captures
            var leftCapture = new Vector2I(BoardPosition.X + direction, BoardPosition.Y - 1);
            var rightCapture = new Vector2I(BoardPosition.X + direction, BoardPosition.Y + 1);
            
            if (IsEnemyPiece(board, leftCapture))
                moves.Add(leftCapture);
                
            if (IsEnemyPiece(board, rightCapture))
                moves.Add(rightCapture);

            return moves;
        }

        /// <summary>
        /// Gets valid moves including en passant opportunities
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="enPassantTarget">En passant target square in algebraic notation (e.g., "e3")</param>
        /// <returns>List of valid move positions</returns>
        public List<Vector2I> GetValidMoves(PieceInfo?[,] board, string? enPassantTarget)
        {
            var moves = GetValidMoves(board); // Get regular moves first
            
            // Add en passant captures if available
            if (!string.IsNullOrEmpty(enPassantTarget))
            {
                var enPassantTargetPos = AlgebraicToBoard(enPassantTarget);
                int direction = Color == PieceColor.White ? 1 : -1;
                
                // Check if this pawn can capture en passant
                // En passant is possible if:
                // 1. The pawn is on the correct rank (5th rank for white, 4th rank for black)
                // 2. There's an enemy pawn adjacent to this pawn
                // 3. The en passant target is diagonally forward from this pawn
                
                if (IsOnEnPassantRank())
                {
                    // Check left and right for en passant captures
                    var leftEnPassant = new Vector2I(BoardPosition.X + direction, BoardPosition.Y - 1);
                    var rightEnPassant = new Vector2I(BoardPosition.X + direction, BoardPosition.Y + 1);
                    
                    if (enPassantTargetPos == leftEnPassant && CanCaptureEnPassant(board, BoardPosition.Y - 1))
                    {
                        moves.Add(leftEnPassant);
                    }
                    
                    if (enPassantTargetPos == rightEnPassant && CanCaptureEnPassant(board, BoardPosition.Y + 1))
                    {
                        moves.Add(rightEnPassant);
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// Checks if this pawn can be promoted (reached the opposite end of the board)
        /// </summary>
        public bool CanPromote()
        {
            return (Color == PieceColor.White && BoardPosition.X == 7) ||
                   (Color == PieceColor.Black && BoardPosition.X == 0);
        }

        /// <summary>
        /// Checks if this pawn is on its starting rank
        /// </summary>
        public bool IsOnStartingRank()
        {
            return (Color == PieceColor.White && BoardPosition.X == 1) ||
                   (Color == PieceColor.Black && BoardPosition.X == 6);
        }

        /// <summary>
        /// Converts algebraic notation to board coordinates
        /// </summary>
        /// <param name="algebraic">Algebraic notation (e.g., "e4")</param>
        /// <returns>Board coordinates</returns>
        private Vector2I AlgebraicToBoard(string algebraic)
        {
            if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2)
                throw new System.ArgumentException("Invalid algebraic notation");
                
            int file = algebraic[0] - 'a'; // Convert 'a'-'h' to 0-7
            int rank = algebraic[1] - '1'; // Convert '1'-'8' to 0-7
            
            return new Vector2I(rank, file);
        }

        /// <summary>
        /// Checks if this pawn is on the correct rank for en passant captures
        /// </summary>
        private bool IsOnEnPassantRank()
        {
            return (Color == PieceColor.White && BoardPosition.X == 4) ||  // 5th rank (0-indexed)
                   (Color == PieceColor.Black && BoardPosition.X == 3);    // 4th rank (0-indexed)
        }

        /// <summary>
        /// Checks if this pawn can capture en passant at the specified file
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="targetFile">File where the enemy pawn should be</param>
        /// <returns>True if en passant capture is possible</returns>
        private bool CanCaptureEnPassant(PieceInfo?[,] board, int targetFile)
        {
            if (!IsValidPosition(new Vector2I(BoardPosition.X, targetFile)))
                return false;
                
            var adjacentPiece = board[BoardPosition.X, targetFile];
            
            // There must be an enemy pawn adjacent to this pawn
            return adjacentPiece.HasValue &&
                   adjacentPiece.Value.Type == PieceType.Pawn &&
                   adjacentPiece.Value.Color != Color;
        }
    }
}