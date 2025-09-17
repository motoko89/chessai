using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a Bishop chess piece
    /// </summary>
    public partial class Bishop : ChessPiece
    {
        public Bishop(PieceColor color, Vector2I position) : base(PieceType.Bishop, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(string?[,] board)
        {
            var moves = new List<Vector2I>();

            // Bishop moves diagonally
            var directions = new Vector2I[]
            {
                new Vector2I(1, 1),   // Up-right
                new Vector2I(1, -1),  // Up-left
                new Vector2I(-1, 1),  // Down-right
                new Vector2I(-1, -1)  // Down-left
            };

            foreach (var direction in directions)
            {
                moves.AddRange(GetMovesInDirection(board, direction));
            }

            return moves;
        }

        /// <summary>
        /// Determines if this bishop is on light or dark squares
        /// </summary>
        public bool IsOnLightSquare()
        {
            // A square is light if the sum of its coordinates is even
            return (BoardPosition.X + BoardPosition.Y) % 2 == 0;
        }

        /// <summary>
        /// Determines if this bishop is on dark squares
        /// </summary>
        public bool IsOnDarkSquare()
        {
            return !IsOnLightSquare();
        }
    }
}