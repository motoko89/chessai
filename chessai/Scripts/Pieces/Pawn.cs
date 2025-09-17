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

            // TODO: Add en passant logic later
            // This would require additional board state information about the last move

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
    }
}