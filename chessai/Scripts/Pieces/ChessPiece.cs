using Godot;
using System.Collections.Generic;

namespace ChessAI.Pieces
{
    /// <summary>
    /// Base class for all chess pieces
    /// </summary>
    public abstract partial class ChessPiece : Node2D
    {
        #region Properties
        public PieceType Type { get; protected set; }
        public PieceColor Color { get; protected set; }
        public Vector2I BoardPosition { get; set; }
        public bool HasMoved { get; set; } = false;
        #endregion

        #region Visual Components
        protected Sprite2D? _sprite;
        protected Area2D? _clickArea;
        protected bool _isSelected = false;
        #endregion

        #region Signals
        [Signal]
        public delegate void PieceClickedEventHandler(ChessPiece piece);
        
        [Signal]
        public delegate void PieceMoveRequestedEventHandler(ChessPiece piece, Vector2I targetPosition);
        #endregion

        protected ChessPiece(PieceType type, PieceColor color, Vector2I position)
        {
            Type = type;
            Color = color;
            BoardPosition = position;
            Name = $"{color}_{type}_{position.X}_{position.Y}";
        }

        public override void _Ready()
        {
            SetupVisualComponents();
            SetupInteraction();
        }

        #region Abstract Methods
        /// <summary>
        /// Returns all valid moves for this piece given the current board state
        /// </summary>
        public abstract List<Vector2I> GetValidMoves(PieceInfo?[,] board);

        /// <summary>
        /// Returns the sprite resource path for this piece
        /// </summary>
        protected virtual string GetSpriteResourcePath()
        {
            string colorPrefix = Color == PieceColor.White ? "white" : "blk";
            string pieceTypeName = Type.ToString().ToLower();
            return $"res://Assets/Sprites/chess_{colorPrefix}_{pieceTypeName}.png";
        }
        #endregion

        #region Visual Setup
        private void SetupVisualComponents()
        {
            // Create sprite
            _sprite = new Sprite2D();
            _sprite.Name = "PieceSprite";
            
            var texturePath = GetSpriteResourcePath();
            if (ResourceLoader.Exists(texturePath))
            {
                _sprite.Texture = GD.Load<Texture2D>(texturePath);
            }
            else
            {
                GD.PrintErr($"Sprite not found: {texturePath}");
            }
            
            _sprite.Position = Vector2.Zero;
            AddChild(_sprite);

            // Scale sprite to fit square size (assuming 64x64 squares)
            if (_sprite.Texture != null)
            {
                var textureSize = _sprite.Texture.GetSize();
                var targetSize = new Vector2(60, 60); // Slightly smaller than square for padding
                var scale = new Vector2(
                    targetSize.X / textureSize.X,
                    targetSize.Y / textureSize.Y
                );
                _sprite.Scale = scale;
            }
        }

        private void SetupInteraction()
        {
            // Create click area
            _clickArea = new Area2D();
            _clickArea.Name = "ClickArea";
            _clickArea.InputPickable = true;
            AddChild(_clickArea);

            var collision = new CollisionShape2D();
            var shape = new RectangleShape2D();
            shape.Size = new Vector2(64, 64); // Match square size
            collision.Shape = shape;
            _clickArea.AddChild(collision);

            // Connect signals
            _clickArea.InputEvent += OnInputEvent;
        }
        #endregion

        #region Interaction
        private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is InputEventMouseButton mouseEvent && 
                mouseEvent.ButtonIndex == MouseButton.Left && 
                mouseEvent.Pressed)
            {
                EmitSignal(SignalName.PieceClicked, this);
            }
        }

        /// <summary>
        /// Highlights or unhighlights this piece
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_sprite != null)
            {
                // Add visual feedback for selection
                _sprite.Modulate = selected ? new Color(1.2f, 1.2f, 0.8f) : Colors.White;
            }
        }
        #endregion

        #region Movement Validation Helpers
        /// <summary>
        /// Checks if a position is within board bounds
        /// </summary>
        protected bool IsValidPosition(Vector2I position)
        {
            return position.X >= 0 && position.X < 8 && position.Y >= 0 && position.Y < 8;
        }

        /// <summary>
        /// Checks if a square is empty
        /// </summary>
        protected bool IsSquareEmpty(PieceInfo?[,] board, Vector2I position)
        {
            return IsValidPosition(position) && !board[position.X, position.Y].HasValue;
        }

        /// <summary>
        /// Checks if a square contains an enemy piece
        /// </summary>
        protected bool IsEnemyPiece(PieceInfo?[,] board, Vector2I position)
        {
            if (!IsValidPosition(position)) return false;
            
            var piece = board[position.X, position.Y];
            if (!piece.HasValue) return false;

            // Check if piece color is different from this piece's color
            return piece.Value.Color != Color;
        }

        /// <summary>
        /// Checks if a square is empty or contains an enemy piece (valid move target)
        /// </summary>
        protected bool CanMoveTo(PieceInfo?[,] board, Vector2I position)
        {
            return IsSquareEmpty(board, position) || IsEnemyPiece(board, position);
        }

        /// <summary>
        /// Gets all moves in a direction until blocked (for sliding pieces)
        /// </summary>
        protected List<Vector2I> GetMovesInDirection(PieceInfo?[,] board, Vector2I direction)
        {
            var moves = new List<Vector2I>();
            var currentPos = BoardPosition + direction;

            while (IsValidPosition(currentPos))
            {
                if (IsSquareEmpty(board, currentPos))
                {
                    moves.Add(currentPos);
                    currentPos += direction;
                }
                else if (IsEnemyPiece(board, currentPos))
                {
                    moves.Add(currentPos); // Can capture
                    break;
                }
                else
                {
                    break; // Blocked by own piece
                }
            }

            return moves;
        }
        #endregion

        #region Position Management
        /// <summary>
        /// Updates the piece's position on the board
        /// </summary>
        public virtual void MoveTo(Vector2I newPosition)
        {
            BoardPosition = newPosition;
            HasMoved = true;
            Name = $"{Color}_{Type}_{newPosition.X}_{newPosition.Y}";
        }
        #endregion

        #region Utility
        public override string ToString()
        {
            return $"{Color} {Type} at {BoardPosition}";
        }

        /// <summary>
        /// Creates a PieceInfo struct from this piece
        /// </summary>
        public PieceInfo ToPieceInfo()
        {
            return new PieceInfo(Type, Color, BoardPosition, HasMoved);
        }
        #endregion
    }
}