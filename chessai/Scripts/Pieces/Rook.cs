using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a Rook chess piece
    /// </summary>
    public partial class Rook : ChessPiece
    {
        public Rook(PieceColor color, Vector2I position) : base(PieceType.Rook, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();

            // Rook moves horizontally and vertically
            var directions = new Vector2I[]
            {
                new Vector2I(1, 0),   // Up
                new Vector2I(-1, 0),  // Down
                new Vector2I(0, 1),   // Right
                new Vector2I(0, -1)   // Left
            };

            foreach (var direction in directions)
            {
                moves.AddRange(GetMovesInDirection(board, direction));
            }

            return moves;
        }

        /// <summary>
        /// Checks if this rook can castle (hasn't moved and is on starting position)
        /// </summary>
        public bool CanCastle()
        {
            if (HasMoved) return false;

            // Check if rook is on starting position
            if (Color == PieceColor.White)
            {
                return BoardPosition.X == 0 && (BoardPosition.Y == 0 || BoardPosition.Y == 7);
            }
            else
            {
                return BoardPosition.X == 7 && (BoardPosition.Y == 0 || BoardPosition.Y == 7);
            }
        }

        /// <summary>
        /// Determines if this is a kingside or queenside rook
        /// </summary>
        public bool IsKingsideRook()
        {
            return BoardPosition.Y == 7; // h-file
        }

        /// <summary>
        /// Determines if this is a queenside rook
        /// </summary>
        public bool IsQueensideRook()
        {
            return BoardPosition.Y == 0; // a-file
        }
    }
}