using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a Queen chess piece
    /// </summary>
    public partial class Queen : ChessPiece
    {
        public Queen(PieceColor color, Vector2I position) : base(PieceType.Queen, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();

            // Queen combines rook and bishop movement (horizontal, vertical, and diagonal)
            var directions = new Vector2I[]
            {
                // Rook-like movements (horizontal and vertical)
                new Vector2I(1, 0),   // Up
                new Vector2I(-1, 0),  // Down
                new Vector2I(0, 1),   // Right
                new Vector2I(0, -1),  // Left
                
                // Bishop-like movements (diagonal)
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
        /// Gets the queen's horizontal and vertical moves (rook-like)
        /// </summary>
        public List<Vector2I> GetRookLikeMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();
            var rookDirections = new Vector2I[]
            {
                new Vector2I(1, 0),   // Up
                new Vector2I(-1, 0),  // Down
                new Vector2I(0, 1),   // Right
                new Vector2I(0, -1)   // Left
            };

            foreach (var direction in rookDirections)
            {
                moves.AddRange(GetMovesInDirection(board, direction));
            }

            return moves;
        }

        /// <summary>
        /// Gets the queen's diagonal moves (bishop-like)
        /// </summary>
        public List<Vector2I> GetBishopLikeMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();
            var bishopDirections = new Vector2I[]
            {
                new Vector2I(1, 1),   // Up-right
                new Vector2I(1, -1),  // Up-left
                new Vector2I(-1, 1),  // Down-right
                new Vector2I(-1, -1)  // Down-left
            };

            foreach (var direction in bishopDirections)
            {
                moves.AddRange(GetMovesInDirection(board, direction));
            }

            return moves;
        }
    }
}