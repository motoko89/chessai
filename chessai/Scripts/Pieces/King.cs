using Godot;
using System;
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

            // Add castling moves (without safety checks - use overloaded method for safety)
            moves.AddRange(GetCastlingMoves(board));

            return moves;
        }

        /// <summary>
        /// Gets valid moves including safe castling moves (with ChessBoard for check detection)
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="chessBoard">ChessBoard instance for check detection</param>
        /// <returns>List of valid moves including safe castling</returns>
        public List<Vector2I> GetValidMoves(PieceInfo?[,] board, ChessAI.Core.ChessBoard chessBoard)
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
                    // Additional safety check: don't move into check
                    if (!chessBoard.WouldMoveResultInCheck(BoardPosition, targetPosition, Color))
                    {
                        moves.Add(targetPosition);
                    }
                }
            }

            // Add safe castling moves
            moves.AddRange(GetCastlingMoves(board, chessBoard));

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
        /// Gets potential castling moves (kingside and queenside) with safety validation
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="chessBoard">ChessBoard instance for check detection (optional)</param>
        /// <returns>List of safe castling moves</returns>
        public List<Vector2I> GetCastlingMoves(PieceInfo?[,] board, ChessAI.Core.ChessBoard chessBoard = null)
        {
            var castlingMoves = new List<Vector2I>();

            if (!CanCastle()) return castlingMoves;

            // Safety check 1: King must not be in check currently
            if (chessBoard != null && chessBoard.IsKingInCheck(Color, board))
            {
                return castlingMoves; // Cannot castle while in check
            }

            int rank = Color == PieceColor.White ? 0 : 7;

            // Kingside castling (short castling)
            var kingsideRookPosition = new Vector2I(rank, 7); // h-file
            if (CanCastleInDirection(board, kingsideRookPosition, true))
            {
                var kingsideTarget = new Vector2I(rank, 6); // g-file
                if (IsCastlingSafe(board, kingsideTarget, true, chessBoard))
                {
                    castlingMoves.Add(kingsideTarget);
                }
            }

            // Queenside castling (long castling)
            var queensideRookPosition = new Vector2I(rank, 0); // a-file
            if (CanCastleInDirection(board, queensideRookPosition, false))
            {
                var queensideTarget = new Vector2I(rank, 2); // c-file
                if (IsCastlingSafe(board, queensideTarget, false, chessBoard))
                {
                    castlingMoves.Add(queensideTarget);
                }
            }

            return castlingMoves;
        }

        /// <summary>
        /// Checks if castling in a specific direction is safe
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="kingTargetSquare">Square where king will end up</param>
        /// <param name="isKingside">True for kingside, false for queenside</param>
        /// <param name="chessBoard">ChessBoard instance for check detection</param>
        /// <returns>True if castling is safe</returns>
        private bool IsCastlingSafe(PieceInfo?[,] board, Vector2I kingTargetSquare, bool isKingside, ChessAI.Core.ChessBoard chessBoard)
        {
            if (chessBoard == null)
            {
                // Without ChessBoard, we can't properly validate check - assume safe for compatibility
                return true;
            }

            PieceColor enemyColor = Color == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Safety check 2: King must not pass through check
            // Check the square the king passes through during castling
            int passThroughFile = isKingside ? 5 : 3; // f-file for kingside, d-file for queenside
            var passThroughSquare = new Vector2I(BoardPosition.X, passThroughFile);
            
            if (chessBoard.IsSquareAttacked(passThroughSquare, enemyColor, board))
            {
                return false; // King would pass through check
            }

            // Safety check 3: King must not end up in check
            if (chessBoard.IsSquareAttacked(kingTargetSquare, enemyColor, board))
            {
                return false; // King would end up in check
            }

            return true;
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
        /// </summary>
        /// <param name="board">Board state to check</param>
        /// <param name="chessBoard">ChessBoard instance for check detection (optional)</param>
        /// <returns>True if the king is in check</returns>
        public bool IsInCheck(PieceInfo?[,] board, ChessAI.Core.ChessBoard chessBoard = null)
        {
            if (chessBoard != null)
            {
                return chessBoard.IsKingInCheck(Color, board);
            }
            
            // Fallback implementation if no ChessBoard is provided
            // Check if any enemy piece can attack the king's position
            PieceColor enemyColor = Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = board[rank, file];
                    if (!piece.HasValue || piece.Value.Color != enemyColor)
                        continue;
                        
                    // Simple check for enemy pieces that could attack this king
                    // This is a basic implementation - the ChessBoard method is more comprehensive
                    if (CanEnemyPieceAttackKing(piece.Value, board))
                        return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Basic check if an enemy piece can attack the king (fallback method)
        /// </summary>
        private bool CanEnemyPieceAttackKing(PieceInfo enemyPiece, PieceInfo?[,] board)
        {
            // This is a simplified check - the ChessBoard method is more accurate
            var dx = Math.Abs(enemyPiece.Position.X - BoardPosition.X);
            var dy = Math.Abs(enemyPiece.Position.Y - BoardPosition.Y);
            
            return enemyPiece.Type switch
            {
                PieceType.Pawn => IsPawnAttackingKing(enemyPiece),
                PieceType.Rook => (dx == 0 || dy == 0) && IsPathClear(enemyPiece.Position, BoardPosition, board),
                PieceType.Bishop => (dx == dy) && IsPathClear(enemyPiece.Position, BoardPosition, board),
                PieceType.Queen => ((dx == 0 || dy == 0) || (dx == dy)) && IsPathClear(enemyPiece.Position, BoardPosition, board),
                PieceType.Knight => (dx == 2 && dy == 1) || (dx == 1 && dy == 2),
                PieceType.King => dx <= 1 && dy <= 1 && (dx + dy > 0),
                _ => false
            };
        }

        /// <summary>
        /// Checks if a pawn is attacking the king
        /// </summary>
        private bool IsPawnAttackingKing(PieceInfo pawn)
        {
            int direction = pawn.Color == PieceColor.White ? 1 : -1;
            var pawnPos = pawn.Position;
            
            // Pawn attacks diagonally forward
            return (BoardPosition.X == pawnPos.X + direction) && 
                   (Math.Abs(BoardPosition.Y - pawnPos.Y) == 1);
        }

        /// <summary>
        /// Checks if the path between two positions is clear
        /// </summary>
        private bool IsPathClear(Vector2I from, Vector2I to, PieceInfo?[,] board)
        {
            var dx = Math.Sign(to.X - from.X);
            var dy = Math.Sign(to.Y - from.Y);
            var x = from.X + dx;
            var y = from.Y + dy;
            
            while (x != to.X || y != to.Y)
            {
                if (board[x, y].HasValue)
                    return false;
                x += dx;
                y += dy;
            }
            
            return true;
        }
    }
}