using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChessAI.Core
{
    /// <summary>
    /// Utility class for converting chess board state to various formats for AI consumption
    /// </summary>
    public static class BoardStateSerializer
    {
        /// <summary>
        /// Converts a 2D board array to a human-readable string representation
        /// </summary>
        /// <param name="board">8x8 array representing the chess board</param>
        /// <returns>String representation of the board with coordinates</returns>
        public static string SerializeBoardToString(string?[,] board)
        {
            var sb = new StringBuilder();
            sb.AppendLine("   a b c d e f g h");
            sb.AppendLine();
            
            // Print ranks from 8 to 1 (chess convention)
            for (int rank = 7; rank >= 0; rank--)
            {
                sb.Append($"{rank + 1}  ");
                for (int file = 0; file < 8; file++)
                {
                    var piece = board[rank, file];
                    sb.Append(string.IsNullOrEmpty(piece) ? ". " : $"{piece} ");
                }
                sb.AppendLine($" {rank + 1}");
            }
            
            sb.AppendLine();
            sb.AppendLine("   a b c d e f g h");
            return sb.ToString();
        }

        /// <summary>
        /// Converts a 2D board array to JSON format for API communication
        /// </summary>
        /// <param name="board">8x8 array representing the chess board</param>
        /// <param name="toMove">Whose turn it is to move ('white' or 'black')</param>
        /// <param name="moveHistory">List of previous moves</param>
        /// <param name="castleRights">Castling rights for both sides</param>
        /// <param name="enPassant">En passant target square, if any</param>
        /// <returns>Dictionary representing the board state</returns>
        public static Dictionary<string, object> SerializeBoardToJson(
            string?[,] board, 
            string toMove, 
            List<string> moveHistory,
            CastleRights? castleRights = null,
            string? enPassant = null)
        {
            // Convert 2D array to array of arrays (rank 8 to rank 1)
            var boardArray = new string?[8][];
            for (int i = 0; i < 8; i++)
            {
                boardArray[i] = new string?[8];
                for (int j = 0; j < 8; j++)
                {
                    // Flip rank order for standard FEN representation (rank 8 first)
                    boardArray[i][j] = board[7 - i, j];
                }
            }

            var result = new Dictionary<string, object>
            {
                ["board"] = boardArray,
                ["toMove"] = toMove.ToLower(),
                ["moveHistory"] = moveHistory
            };

            if (castleRights != null)
            {
                result["castleRights"] = castleRights;
            }

            if (!string.IsNullOrEmpty(enPassant))
            {
                result["enPassant"] = enPassant;
            }

            return result;
        }

        /// <summary>
        /// Converts board state to JSON string
        /// </summary>
        public static string SerializeBoardToJsonString(
            string?[,] board, 
            string toMove, 
            List<string> moveHistory,
            CastleRights? castleRights = null,
            string? enPassant = null)
        {
            var boardDict = SerializeBoardToJson(board, toMove, moveHistory, castleRights, enPassant);
            return JsonConvert.SerializeObject(boardDict, Formatting.Indented);
        }

        /// <summary>
        /// Creates an initial chess board setup
        /// </summary>
        /// <returns>8x8 board array with starting positions</returns>
        public static string?[,] CreateInitialBoard()
        {
            var board = new string?[8, 8];
            
            // Set up initial piece positions
            // Rank 1 (white pieces)
            board[0, 0] = "R"; board[0, 1] = "N"; board[0, 2] = "B"; board[0, 3] = "Q";
            board[0, 4] = "K"; board[0, 5] = "B"; board[0, 6] = "N"; board[0, 7] = "R";
            
            // Rank 2 (white pawns)
            for (int file = 0; file < 8; file++)
                board[1, file] = "P";
            
            // Empty ranks 3-6
            for (int rank = 2; rank < 6; rank++)
                for (int file = 0; file < 8; file++)
                    board[rank, file] = null;
            
            // Rank 7 (black pawns)
            for (int file = 0; file < 8; file++)
                board[6, file] = "p";
            
            // Rank 8 (black pieces)
            board[7, 0] = "r"; board[7, 1] = "n"; board[7, 2] = "b"; board[7, 3] = "q";
            board[7, 4] = "k"; board[7, 5] = "b"; board[7, 6] = "n"; board[7, 7] = "r";
            
            return board;
        }

        /// <summary>
        /// Converts algebraic notation (e.g., "e4") to array indices
        /// </summary>
        /// <param name="algebraic">Position in algebraic notation</param>
        /// <returns>Tuple of (rank, file) indices</returns>
        public static (int rank, int file) AlgebraicToIndices(string algebraic)
        {
            if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2)
                throw new System.ArgumentException("Invalid algebraic notation");

            var file = algebraic[0] - 'a'; // a=0, b=1, ..., h=7
            var rank = algebraic[1] - '1'; // 1=0, 2=1, ..., 8=7
            
            if (file < 0 || file > 7 || rank < 0 || rank > 7)
                throw new System.ArgumentException("Position out of bounds");

            return (rank, file);
        }

        /// <summary>
        /// Converts array indices to algebraic notation
        /// </summary>
        /// <param name="rank">Rank index (0-7)</param>
        /// <param name="file">File index (0-7)</param>
        /// <returns>Position in algebraic notation</returns>
        public static string IndicesToAlgebraic(int rank, int file)
        {
            if (rank < 0 || rank > 7 || file < 0 || file > 7)
                throw new System.ArgumentException("Indices out of bounds");

            var fileChar = (char)('a' + file);
            var rankChar = (char)('1' + rank);
            
            return $"{fileChar}{rankChar}";
        }
    }

    /// <summary>
    /// Represents castling rights for both sides
    /// </summary>
    public class CastleRights
    {
        [JsonProperty("whiteKingside")]
        public bool WhiteKingside { get; set; } = true;

        [JsonProperty("whiteQueenside")]
        public bool WhiteQueenside { get; set; } = true;

        [JsonProperty("blackKingside")]
        public bool BlackKingside { get; set; } = true;

        [JsonProperty("blackQueenside")]
        public bool BlackQueenside { get; set; } = true;

        /// <summary>
        /// Creates initial castling rights (all allowed)
        /// </summary>
        public static CastleRights Initial => new CastleRights();
    }
}