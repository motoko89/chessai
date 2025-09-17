using Godot;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Enum representing the different types of chess pieces
    /// </summary>
    public enum PieceType
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    /// <summary>
    /// Enum representing the color of chess pieces
    /// </summary>
    public enum PieceColor
    {
        White,
        Black
    }

    /// <summary>
    /// Represents a chess piece position and state
    /// </summary>
    public struct PieceInfo
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public Vector2I Position { get; set; }
        public bool HasMoved { get; set; } // Important for castling and pawn double moves
        
        public PieceInfo(PieceType type, PieceColor color, Vector2I position, bool hasMoved = false)
        {
            Type = type;
            Color = color;
            Position = position;
            HasMoved = hasMoved;
        }

        /// <summary>
        /// Converts piece to single character notation for serialization
        /// </summary>
        public string ToNotation()
        {
            string pieceChar = Type switch
            {
                PieceType.Pawn => "P",
                PieceType.Rook => "R",
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => "?"
            };
            
            return Color == PieceColor.White ? pieceChar : pieceChar.ToLower();
        }

        /// <summary>
        /// Creates PieceInfo from character notation
        /// </summary>
        public static PieceInfo? FromNotation(string notation, Vector2I position)
        {
            if (string.IsNullOrEmpty(notation)) return null;

            var color = char.IsUpper(notation[0]) ? PieceColor.White : PieceColor.Black;
            var typeChar = char.ToUpper(notation[0]);

            var type = typeChar switch
            {
                'P' => PieceType.Pawn,
                'R' => PieceType.Rook,
                'N' => PieceType.Knight,
                'B' => PieceType.Bishop,
                'Q' => PieceType.Queen,
                'K' => PieceType.King,
                _ => (PieceType?)null
            };

            if (type.HasValue)
                return new PieceInfo(type.Value, color, position);
            
            return null;
        }

        /// <summary>
        /// Returns the sprite resource path for this piece
        /// </summary>
        public string GetSpriteResourcePath()
        {
            string colorPrefix = Color == PieceColor.White ? "white" : "blk";
            string pieceTypeName = Type.ToString().ToLower();
            return $"res://Assets/Sprites/chess_{colorPrefix}_{pieceTypeName}.png";
        }

        public override string ToString()
        {
            return $"{Color} {Type} at {Position}";
        }
    }
}