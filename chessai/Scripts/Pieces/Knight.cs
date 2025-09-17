using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a Knight chess piece
    /// </summary>
    public partial class Knight : ChessPiece
    {
        public Knight(PieceColor color, Vector2I position) : base(PieceType.Knight, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();

            // Knight moves in L-shapes: 2 squares in one direction, 1 square perpendicular
            var knightMoves = new Vector2I[]
            {
                new Vector2I(2, 1),   // 2 up, 1 right
                new Vector2I(2, -1),  // 2 up, 1 left
                new Vector2I(-2, 1),  // 2 down, 1 right
                new Vector2I(-2, -1), // 2 down, 1 left
                new Vector2I(1, 2),   // 1 up, 2 right
                new Vector2I(1, -2),  // 1 up, 2 left
                new Vector2I(-1, 2),  // 1 down, 2 right
                new Vector2I(-1, -2)  // 1 down, 2 left
            };

            foreach (var move in knightMoves)
            {
                var targetPosition = BoardPosition + move;
                
                // Check if the target position is valid and can be moved to
                if (IsValidPosition(targetPosition) && CanMoveTo(board, targetPosition))
                {
                    moves.Add(targetPosition);
                }
            }

            return moves;
        }

        /// <summary>
        /// Knights are unique in that they can jump over other pieces
        /// This method emphasizes that property
        /// </summary>
        public bool CanJumpOver()
        {
            return true; // Knights always jump over pieces
        }
    }
}