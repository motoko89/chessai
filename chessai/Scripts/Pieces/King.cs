using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Represents a King chess piece
    /// </summary>
    public partial class King : ChessPiece
    {
        public King(PieceColor color, Vector2I position) : base(PieceType.King, color, position)
        {
        }

        public override List<Vector2I> GetValidMoves(PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();

            // King moves one square in any direction
            var kingMoves = new Vector2I[]
            {
                new Vector2I(1, 0),   // Up
                new Vector2I(-1, 0),  // Down
                new Vector2I(0, 1),   // Right
                new Vector2I(0, -1),  // Left
                new Vector2I(1, 1),   // Up-right
                new Vector2I(1, -1),  // Up-left
                new Vector2I(-1, 1),  // Down-right
                new Vector2I(-1, -1)  // Down-left
            };

            foreach (var move in kingMoves)
            {
                var targetPosition = BoardPosition + move;
                
                if (IsValidPosition(targetPosition) && CanMoveTo(board, targetPosition))
                {
                    moves.Add(targetPosition);
                }
            }

            // TODO: Add castling logic later
            // This requires checking:
            // 1. King hasn't moved
            // 2. Rook hasn't moved
            // 3. No pieces between king and rook
            // 4. King is not in check
            // 5. King doesn't pass through check
            // 6. King doesn't end up in check

            return moves;
        }

        /// <summary>
        /// Checks if the king can castle (hasn't moved and is on starting position)
        /// </summary>
        public bool CanCastle()
        {
            if (HasMoved) return false;

            // Check if king is on starting position
            if (Color == PieceColor.White)
            {
                return BoardPosition.X == 0 && BoardPosition.Y == 4; // e1
            }
            else
            {
                return BoardPosition.X == 7 && BoardPosition.Y == 4; // e8
            }
        }

        /// <summary>
        /// Gets potential castling moves (kingside and queenside)
        /// Note: This doesn't check for check conditions - that should be done at board level
        /// </summary>
        public List<Vector2I> GetCastlingMoves(PieceInfo?[,] board)
        {
            var castlingMoves = new List<Vector2I>();

            if (!CanCastle()) return castlingMoves;

            int rank = Color == PieceColor.White ? 0 : 7;

            // Kingside castling (short castling)
            var kingsideRookPosition = new Vector2I(rank, 7); // h-file
            if (CanCastleInDirection(board, kingsideRookPosition, true))
            {
                castlingMoves.Add(new Vector2I(rank, 6)); // g-file
            }

            // Queenside castling (long castling)
            var queensideRookPosition = new Vector2I(rank, 0); // a-file
            if (CanCastleInDirection(board, queensideRookPosition, false))
            {
                castlingMoves.Add(new Vector2I(rank, 2)); // c-file
            }

            return castlingMoves;
        }

        /// <summary>
        /// Helper method to check if castling is possible in a given direction
        /// </summary>
        private bool CanCastleInDirection(PieceInfo?[,] board, Vector2I rookPosition, bool isKingside)
        {
            // Check if rook exists and hasn't moved
            var rookPiece = board[rookPosition.X, rookPosition.Y];
            if (!rookPiece.HasValue) return false;

            // Verify it's the correct rook and hasn't moved
            if (rookPiece.Value.Type != PieceType.Rook || 
                rookPiece.Value.Color != Color || 
                rookPiece.Value.HasMoved) return false;

            // Check if squares between king and rook are empty
            int startFile = isKingside ? 5 : 1; // f-file or b-file
            int endFile = isKingside ? 6 : 3;   // g-file or d-file

            for (int file = startFile; file <= endFile; file++)
            {
                if (!IsSquareEmpty(board, new Vector2I(BoardPosition.X, file)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the king is currently in check
        /// Note: This is a placeholder - actual check detection should be done at board level
        /// </summary>
        public bool IsInCheck(string?[,] board)
        {
            // TODO: Implement check detection
            // This requires checking if any enemy piece can attack the king's current position
            return false;
        }
    }
}