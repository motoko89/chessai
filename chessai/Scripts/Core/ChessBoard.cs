using ChessAI.Pieces;
using ChessAI.AI;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessAI.Core
{
    /// <summary>
    /// Main ChessBoard class that manages the chess board data model and visual representation
    /// </summary>
    public partial class ChessBoard : Node2D
    {
        #region Constants
        private const int BOARD_SIZE = 8;
        private const float SQUARE_SIZE = 64f; // 64px per square for 512px total board
        private const float BOARD_OFFSET_X = 30f; // Space for rank labels (1-8)
        private const float BOARD_OFFSET_Y = 50f; // Space for file labels (a-h)
        #endregion

        #region Private Fields
        private PieceInfo?[,] _board = new PieceInfo?[BOARD_SIZE, BOARD_SIZE];
        private PieceColor _currentPlayer = PieceColor.White;
        private List<string> _moveHistory = new();
        private CastleRights _castleRights = CastleRights.Initial;
        private string? _enPassantTarget = null;
        
        // AI Service reference
        private AIService? _aiService;
        private bool _isWaitingForAI = false;
        
        // Visual components
        private ColorRect[,] _squares = new ColorRect[BOARD_SIZE, BOARD_SIZE];
        private ChessPiece?[,] _pieceNodes = new ChessPiece?[BOARD_SIZE, BOARD_SIZE];
        private Node2D? _squareContainer;
        private Node2D? _pieceContainer;
        private Vector2I? _selectedSquare = null;
        private ChessPiece? _selectedPiece = null;
        private List<Vector2I> _highlightedSquares = new();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the current player whose turn it is
        /// </summary>
        public PieceColor CurrentPlayer => _currentPlayer;
        
        /// <summary>
        /// Gets a copy of the move history
        /// </summary>
        public List<string> MoveHistory => new(_moveHistory);
        
        /// <summary>
        /// Gets the current castling rights
        /// </summary>
        public CastleRights CastleRights => _castleRights;
        
        /// <summary>
        /// Gets the en passant target square
        /// </summary>
        public string? EnPassantTarget => _enPassantTarget;
        
        /// <summary>
        /// Gets the currently selected square
        /// </summary>
        public Vector2I? SelectedSquare => _selectedSquare;
        
        /// <summary>
        /// Gets the currently selected piece
        /// </summary>
        public ChessPiece? SelectedPiece => _selectedPiece;
        #endregion

        #region Signals
        /// <summary>
        /// Emitted when a square is clicked
        /// </summary>
        [Signal]
        public delegate void SquareClickedEventHandler(Vector2I position);
        
        /// <summary>
        /// Emitted when a move is executed
        /// </summary>
        [Signal]
        public delegate void MoveExecutedEventHandler(string from, string to, string piece);
        
        /// <summary>
        /// Emitted when the game state changes
        /// </summary>
        [Signal]
        public delegate void GameStateChangedEventHandler(string currentPlayer, bool inCheck);
        #endregion

        public ChessBoard()
        {
            InitializeBoard();
            GD.Print("Chess board initialized with starting position");
        }

        #region Godot Lifecycle
        public override void _Ready()
        {
            CreateVisualBoard();
            CreatePieceNodes();
            
            // Initialize AI Service connection
            InitializeAIService();

            // Debug: Print scene tree structure
            GD.Print("=== Scene Tree Structure ===");
            PrintSceneTree(this, 0);
        }

        private void PrintSceneTree(Node node, int depth)
        {
            string indent = new string(' ', depth * 2);
            GD.Print($"{indent}{node.Name} ({node.GetType().Name})");

            foreach (Node child in node.GetChildren())
            {
                PrintSceneTree(child, depth + 1);
            }
        }

        /// <summary>
        /// Initializes the connection to the AI Service
        /// </summary>
        private void InitializeAIService()
        {
            // Get the AIService singleton instance
            _aiService = AIService.Instance;
            
            if (_aiService != null)
            {
                // Connect to AI signals
                _aiService.AIMoveReceived += OnAIMoveReceived;
                _aiService.AIError += OnAIError;
                GD.Print("ChessBoard connected to AIService");
            }
            else
            {
                GD.PrintErr("AIService instance not found - AI moves will not work");
            }
        }
        #endregion

        #region Initialization

        /// <summary>
        /// Creates the visual representation of the chess board
        /// </summary>
        private void CreateVisualBoard()
        {
            // Create container for squares
            _squareContainer = new Node2D();
            _squareContainer.Name = "SquareContainer";
            AddChild(_squareContainer);

            // Create 8x8 grid of squares
            for (int rank = 0; rank < BOARD_SIZE; rank++)
            {
                for (int file = 0; file < BOARD_SIZE; file++)
                {
                    CreateSquare(rank, file);
                }
            }

            GD.Print("Visual chess board created");
        }

        /// <summary>
        /// Creates a single square on the chess board
        /// </summary>
        private void CreateSquare(int rank, int file)
        {
            var square = new ColorRect();
            square.MouseFilter = Control.MouseFilterEnum.Ignore;
            square.Name = $"Square_{rank}_{file}";
            square.Size = new Vector2(SQUARE_SIZE, SQUARE_SIZE);
            square.Position = BoardToScreen(rank, file);
            
            // Set square color (alternating pattern)
            bool isLightSquare = (rank + file) % 2 == 0;
            square.Color = isLightSquare ? Colors.Burlywood : Colors.SaddleBrown;
            
            // Store reference and add to scene
            _squares[rank, file] = square;
            _squareContainer?.AddChild(square);

            // Add click detection
            /*var area = new Area2D();
            area.Name = $"ClickArea_{rank}_{file}";
            square.AddChild(area);
            
            var collision = new CollisionShape2D();
            var shape = new RectangleShape2D();
            shape.Size = new Vector2(SQUARE_SIZE, SQUARE_SIZE);
            collision.Shape = shape;
            area.AddChild(collision);
            
            // Connect click signal
            area.InputEvent += (Node viewport, InputEvent @event, long shapeIdx) => {
                OnSquareClicked(@event, new Vector2I(rank, file));
            };*/
        }

        /// <summary>
        /// Initializes the chess board with starting position
        /// </summary>
        private void InitializeBoard()
        {
            // Clear the board
            _board = new PieceInfo?[BOARD_SIZE, BOARD_SIZE];
            
            // Set up white pieces (bottom of board, rank 0 and 1)
            SetupPiecesForColor(PieceColor.White, 0, 1);
            
            // Set up black pieces (top of board, rank 6 and 7) 
            SetupPiecesForColor(PieceColor.Black, 7, 6);
            
            _currentPlayer = PieceColor.White;
        }

        /// <summary>
        /// Sets up pieces for a specific color
        /// </summary>
        private void SetupPiecesForColor(PieceColor color, int backRank, int pawnRank)
        {
            // Back row pieces (rank 0 for white, rank 7 for black)
            _board[backRank, 0] = new PieceInfo(PieceType.Rook, color, new Vector2I(backRank, 0));
            _board[backRank, 1] = new PieceInfo(PieceType.Knight, color, new Vector2I(backRank, 1));
            _board[backRank, 2] = new PieceInfo(PieceType.Bishop, color, new Vector2I(backRank, 2));
            _board[backRank, 3] = new PieceInfo(PieceType.Queen, color, new Vector2I(backRank, 3));
            _board[backRank, 4] = new PieceInfo(PieceType.King, color, new Vector2I(backRank, 4));
            _board[backRank, 5] = new PieceInfo(PieceType.Bishop, color, new Vector2I(backRank, 5));
            _board[backRank, 6] = new PieceInfo(PieceType.Knight, color, new Vector2I(backRank, 6));
            _board[backRank, 7] = new PieceInfo(PieceType.Rook, color, new Vector2I(backRank, 7));

            // Pawns (rank 1 for white, rank 6 for black)
            for (int file = 0; file < BOARD_SIZE; file++)
            {
                _board[pawnRank, file] = new PieceInfo(PieceType.Pawn, color, new Vector2I(pawnRank, file));
            }
        }

        /// <summary>
        /// Creates a visual ChessPiece node from PieceInfo
        /// </summary>
        private ChessPiece CreatePieceNode(PieceInfo pieceInfo)
        {
            ChessPiece piece = pieceInfo.Type switch
            {
                PieceType.Pawn => new Pawn(pieceInfo.Color, pieceInfo.Position),
                PieceType.Rook => new Rook(pieceInfo.Color, pieceInfo.Position),
                PieceType.Knight => new Knight(pieceInfo.Color, pieceInfo.Position),
                PieceType.Bishop => new Bishop(pieceInfo.Color, pieceInfo.Position),
                PieceType.Queen => new Queen(pieceInfo.Color, pieceInfo.Position),
                PieceType.King => new King(pieceInfo.Color, pieceInfo.Position),
                _ => throw new System.ArgumentException($"Unknown piece type: {pieceInfo.Type}")
            };

            // Set piece position on screen (centered in square)
            piece.Position = BoardToScreen(pieceInfo.Position.X, pieceInfo.Position.Y, centerInSquare: true);
            piece.HasMoved = pieceInfo.HasMoved;

            // Connect piece signals
            piece.PieceClicked += OnPieceClicked;

            return piece;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && 
                mouseEvent.ButtonIndex == MouseButton.Left && 
                mouseEvent.Pressed)
            {
                var boardPos = ScreenToBoard(mouseEvent.Position);
                if (boardPos.HasValue)
                {
                    GD.Print($"[_Input] ChessBoard handling click at {boardPos.Value} - consuming event");
                    HandleSquareClick(boardPos.Value);
                    // Consume the event to prevent pieces from also handling it
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        /// <summary>
        /// Gets valid moves for the currently selected piece
        /// </summary>
        /// <returns>List of valid moves for the selected piece, or empty list if no piece selected</returns>
        private List<Vector2I> GetValidMovesForSelectedPiece()
        {
            if (_selectedPiece == null)
                return new List<Vector2I>();
            
            List<Vector2I> validMoves;
            if (_selectedPiece.Type == PieceType.Pawn && _selectedPiece is Pawn pawn)
            {
                validMoves = pawn.GetValidMoves(_board, _enPassantTarget);
            }
            else if (_selectedPiece.Type == PieceType.King && _selectedPiece is King king)
            {
                validMoves = king.GetValidMoves(_board, this);
            }
            else
            {
                validMoves = _selectedPiece.GetValidMoves(_board);
            }
            
            // Filter out moves that would put own king in check
            return validMoves.Where(move => 
                !WouldMoveResultInCheck(_selectedPiece.BoardPosition, move, _selectedPiece.Color)).ToList();
        }

        /// <summary>
        /// Handles clicks on board squares implementing the game's click logic
        /// </summary>
        /// <param name="position">The board position that was clicked</param>
        private void HandleSquareClick(Vector2I position)
        {
            GD.Print($"[HandleSquareClick] Clicked position: {BoardToAlgebraic(position.X, position.Y)} ({position.X}, {position.Y})");
            GD.Print($"[HandleSquareClick] Current selection: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
            
            var pieceAtSquare = GetPieceAt(position);
            GD.Print($"[HandleSquareClick] Piece at clicked square: {(pieceAtSquare.HasValue ? pieceAtSquare.Value.ToString() : "Empty")}");
            
            if (_selectedPiece == null)
            {
                GD.Print("[HandleSquareClick] No piece currently selected");
                // No piece currently selected
                if (pieceAtSquare.HasValue && pieceAtSquare.Value.Color == PieceColor.White)
                {
                    GD.Print("[HandleSquareClick] Clicking on white piece - selecting it");
                    // Clicking on white piece - select it
                    var pieceNode = _pieceNodes[position.X, position.Y];
                    if (pieceNode != null)
                    {
                        GD.Print($"[HandleSquareClick] Piece node found: {pieceNode}");
                        SelectPiece(pieceNode);
                    }
                    else
                    {
                        GD.Print("[HandleSquareClick] ERROR: No piece node found at position");
                    }
                }
                else
                {
                    GD.Print("[HandleSquareClick] No-op: clicking empty square or enemy piece when nothing selected");
                }
            }
            else
            {
                GD.Print("[HandleSquareClick] A piece is currently selected");
                // A piece is currently selected
                if (position == _selectedPiece.BoardPosition)
                {
                    GD.Print("[HandleSquareClick] Clicking on the square of the selected piece - doing nothing");
                    // Clicking on the square of the selected piece - do nothing
                    return;
                }
                
                // Check if this is a valid move square
                var validMoves = GetValidMovesForSelectedPiece();
                GD.Print($"[HandleSquareClick] Valid moves for selected piece: {string.Join(", ", validMoves.Select(m => BoardToAlgebraic(m.X, m.Y)))}");
                
                if (validMoves.Contains(position))
                {
                    GD.Print("[HandleSquareClick] Valid move - executing it");
                    // Valid move - execute it
                    if (TryExecuteMove(_selectedPiece.BoardPosition, position))
                    {
                        GD.Print("[HandleSquareClick] Move executed successfully");
                        // Note: ClearSelection is called within ExecuteMove, but we ensure it here as well for safety
                        if (_selectedPiece != null)
                        {
                            GD.Print("[HandleSquareClick] Selection still exists after move, clearing it manually");
                            ClearSelection();
                        }
                        else
                        {
                            GD.Print("[HandleSquareClick] Selection already cleared by move execution");
                        }
                    }
                    else
                    {
                        GD.Print("[HandleSquareClick] Move execution failed");
                    }
                }
                else if (pieceAtSquare.HasValue && pieceAtSquare.Value.Color == PieceColor.White)
                {
                    GD.Print("[HandleSquareClick] Clicking on another white piece - switching selection");
                    // Clicking on another white piece - switch selection
                    var pieceNode = _pieceNodes[position.X, position.Y];
                    if (pieceNode != null)
                    {
                        GD.Print($"[HandleSquareClick] Switching to piece: {pieceNode}");
                        ClearSelection();
                        SelectPiece(pieceNode);
                    }
                    else
                    {
                        GD.Print("[HandleSquareClick] ERROR: No piece node found at white piece position");
                    }
                }
                else
                {
                    GD.Print("[HandleSquareClick] Clicking on empty square or enemy piece that's not a valid move - unselecting");
                    // Clicking on empty square or enemy piece that's not a valid move - unselect
                    ClearSelection();
                }
            }
            
            GD.Print($"[HandleSquareClick] End - Current selection: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
        }

        /// <summary>
        /// Creates and displays all piece nodes on the board
        /// </summary>
        private void CreatePieceNodes()
        {
            // Create container for pieces
            if (_pieceContainer == null)
            {
                _pieceContainer = new Node2D();
                _pieceContainer.Name = "PieceContainer";
                _pieceContainer.ZIndex = 10;
                AddChild(_pieceContainer);
            }

            // Clear existing pieces
            foreach (Node child in _pieceContainer.GetChildren())
            {
                child.QueueFree();
            }

            // Reset piece node array
            _pieceNodes = new ChessPiece?[BOARD_SIZE, BOARD_SIZE];

            // Create nodes for all pieces on board
            for (int rank = 0; rank < BOARD_SIZE; rank++)
            {
                for (int file = 0; file < BOARD_SIZE; file++)
                {
                    var pieceInfo = _board[rank, file];
                    if (pieceInfo.HasValue)
                    {
                        GD.Print($"Creating piece node for {pieceInfo.Value} at ({rank}, {file})");
                        var pieceNode = CreatePieceNode(pieceInfo.Value);
                        _pieceNodes[rank, file] = pieceNode;
                        _pieceContainer.AddChild(pieceNode);
                        GD.Print($"Added piece {pieceNode} to scene tree");
                    }
                }
            }
        }
        #endregion

        #region Coordinate Conversion
        /// <summary>
        /// Converts board coordinates to screen position
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <param name="centerInSquare">If true, returns center of square instead of top-left corner</param>
        /// <returns>Screen position in pixels</returns>
        public Vector2 BoardToScreen(int rank, int file, bool centerInSquare = false)
        {
            if (!IsValidPosition(rank, file))
            {
                GD.PrintErr($"Invalid board position: rank={rank}, file={file}");
                return Vector2.Zero;
            }

            // Convert to screen coordinates
            // Note: We flip the rank because chess board rank 0 should appear at bottom
            float x = BOARD_OFFSET_X + file * SQUARE_SIZE;
            float y = BOARD_OFFSET_Y + (BOARD_SIZE - 1 - rank) * SQUARE_SIZE;
            
            // If centering is requested, add half square size to both coordinates
            if (centerInSquare)
            {
                x += SQUARE_SIZE / 2;
                y += SQUARE_SIZE / 2;
            }
            
            return new Vector2(x, y);
        }

        /// <summary>
        /// Converts screen position to board coordinates
        /// </summary>
        /// <param name="screenPos">Screen position in pixels</param>
        /// <returns>Board coordinates as Vector2I, or null if outside board</returns>
        public Vector2I? ScreenToBoard(Vector2 screenPos)
        {
            // Check if position is within board bounds
            float relativeX = screenPos.X - BOARD_OFFSET_X;
            float relativeY = screenPos.Y - BOARD_OFFSET_Y;
            
            if (relativeX < 0 || relativeY < 0 || 
                relativeX >= BOARD_SIZE * SQUARE_SIZE || 
                relativeY >= BOARD_SIZE * SQUARE_SIZE)
            {
                return null;
            }

            // Convert to board coordinates
            int file = (int)(relativeX / SQUARE_SIZE);
            int rank = BOARD_SIZE - 1 - (int)(relativeY / SQUARE_SIZE); // Flip Y coordinate
            
            return new Vector2I(rank, file);
        }

        /// <summary>
        /// Converts board coordinates to algebraic notation
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <returns>Algebraic notation (e.g., "e4")</returns>
        public string BoardToAlgebraic(int rank, int file)
        {
            return BoardStateSerializer.IndicesToAlgebraic(rank, file);
        }

        /// <summary>
        /// Converts algebraic notation to board coordinates
        /// </summary>
        /// <param name="algebraic">Algebraic notation (e.g., "e4")</param>
        /// <returns>Board coordinates as Vector2I</returns>
        public Vector2I AlgebraicToBoard(string algebraic)
        {
            var (rank, file) = BoardStateSerializer.AlgebraicToIndices(algebraic);
            return new Vector2I(rank, file);
        }
        #endregion

        #region Board State Access
        /// <summary>
        /// Validates if the given position is within board bounds
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <returns>True if position is valid</returns>
        public bool IsValidPosition(int rank, int file)
        {
            return rank >= 0 && rank < BOARD_SIZE && file >= 0 && file < BOARD_SIZE;
        }

        /// <summary>
        /// Gets the piece at the specified position
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <returns>PieceInfo or null if empty</returns>
        public PieceInfo? GetPieceAt(int rank, int file)
        {
            if (!IsValidPosition(rank, file)) 
                return null;
            
            return _board[rank, file];
        }

        /// <summary>
        /// Gets the piece at the specified position using Vector2I
        /// </summary>
        /// <param name="position">Board position</param>
        /// <returns>PieceInfo or null if empty</returns>
        public PieceInfo? GetPieceAt(Vector2I position)
        {
            return GetPieceAt(position.X, position.Y);
        }

        /// <summary>
        /// Gets the piece string at the specified position (for backward compatibility)
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <returns>Piece string or null if empty</returns>
        public string? GetPieceStringAt(int rank, int file)
        {
            var piece = GetPieceAt(rank, file);
            return piece?.ToNotation();
        }

        /// <summary>
        /// Sets a piece at the specified position
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <param name="piece">PieceInfo or null for empty</param>
        public void SetPieceAt(int rank, int file, PieceInfo? piece)
        {
            if (!IsValidPosition(rank, file))
            {
                GD.PrintErr($"Cannot set piece at invalid position: rank={rank}, file={file}");
                return;
            }

            _board[rank, file] = piece;
        }

        /// <summary>
        /// Sets a piece using string notation (for backward compatibility)
        /// </summary>
        /// <param name="rank">Board rank (0-7)</param>
        /// <param name="file">Board file (0-7)</param>
        /// <param name="pieceString">Piece string or null for empty</param>
        public void SetPieceStringAt(int rank, int file, string? pieceString)
        {
            var piece = string.IsNullOrEmpty(pieceString) ? null : PieceInfo.FromNotation(pieceString, new Vector2I(rank, file));
            SetPieceAt(rank, file, piece);
        }

        /// <summary>
        /// Gets a copy of the current board state as PieceInfo
        /// </summary>
        /// <returns>8x8 array copy of the board</returns>
        public PieceInfo?[,] GetBoardCopy()
        {
            var copy = new PieceInfo?[BOARD_SIZE, BOARD_SIZE];
            for (int rank = 0; rank < BOARD_SIZE; rank++)
            {
                for (int file = 0; file < BOARD_SIZE; file++)
                {
                    copy[rank, file] = _board[rank, file];
                }
            }
            return copy;
        }
        #endregion

        #region Check Detection
        /// <summary>
        /// Checks if a square is under attack by any piece of the specified color
        /// </summary>
        /// <param name="square">Square to check</param>
        /// <param name="byColor">Color of attacking pieces</param>
        /// <param name="board">Board state to check (optional, uses current board if null)</param>
        /// <returns>True if the square is under attack</returns>
        public bool IsSquareAttacked(Vector2I square, PieceColor byColor, PieceInfo?[,] board = null)
        {
            board ??= _board;
            
            // Check all squares on the board for pieces of the attacking color
            for (int rank = 0; rank < BOARD_SIZE; rank++)
            {
                for (int file = 0; file < BOARD_SIZE; file++)
                {
                    var piece = board[rank, file];
                    if (!piece.HasValue || piece.Value.Color != byColor)
                        continue;
                        
                    // Get the piece's valid moves and check if the target square is attacked
                    var validMoves = GetPieceValidMoves(piece.Value, board);
                    if (validMoves.Contains(square))
                        return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets valid moves for a specific piece (helper for attack detection)
        /// </summary>
        /// <param name="pieceInfo">Piece to get moves for</param>
        /// <param name="board">Board state</param>
        /// <returns>List of valid moves</returns>
        private List<Vector2I> GetPieceValidMoves(PieceInfo pieceInfo, PieceInfo?[,] board)
        {
            // Create a temporary piece node to get valid moves
            ChessPiece tempPiece = pieceInfo.Type switch
            {
                PieceType.Pawn => new Pawn(pieceInfo.Color, pieceInfo.Position),
                PieceType.Rook => new Rook(pieceInfo.Color, pieceInfo.Position),
                PieceType.Knight => new Knight(pieceInfo.Color, pieceInfo.Position),
                PieceType.Bishop => new Bishop(pieceInfo.Color, pieceInfo.Position),
                PieceType.Queen => new Queen(pieceInfo.Color, pieceInfo.Position),
                PieceType.King => new King(pieceInfo.Color, pieceInfo.Position),
                _ => throw new System.ArgumentException($"Unknown piece type: {pieceInfo.Type}")
            };
            
            tempPiece.HasMoved = pieceInfo.HasMoved;
            
            // For pawns, we need to use attack moves specifically (not regular moves)
            if (pieceInfo.Type == PieceType.Pawn)
            {
                return GetPawnAttackMoves(tempPiece as Pawn, board);
            }
            
            // For kings, we only want basic moves (not castling) to avoid infinite recursion
            if (pieceInfo.Type == PieceType.King)
            {
                return GetKingBasicMoves(tempPiece as King, board);
            }
            
            return tempPiece.GetValidMoves(board);
        }

        /// <summary>
        /// Gets only the attack moves for a pawn (diagonal captures)
        /// </summary>
        /// <param name="pawn">Pawn piece</param>
        /// <param name="board">Board state</param>
        /// <returns>List of attack squares</returns>
        private List<Vector2I> GetPawnAttackMoves(Pawn pawn, PieceInfo?[,] board)
        {
            var attackMoves = new List<Vector2I>();
            int direction = pawn.Color == PieceColor.White ? 1 : -1;
            
            // Check diagonal attack squares (regardless of whether there's a piece there)
            var leftAttack = new Vector2I(pawn.BoardPosition.X + direction, pawn.BoardPosition.Y - 1);
            var rightAttack = new Vector2I(pawn.BoardPosition.X + direction, pawn.BoardPosition.Y + 1);
            
            if (IsValidPosition(leftAttack.X, leftAttack.Y))
                attackMoves.Add(leftAttack);
                
            if (IsValidPosition(rightAttack.X, rightAttack.Y))
                attackMoves.Add(rightAttack);
                
            return attackMoves;
        }

        /// <summary>
        /// Gets only the basic moves for a king (no castling to avoid recursion)
        /// </summary>
        /// <param name="king">King piece</param>
        /// <param name="board">Board state</param>
        /// <returns>List of basic king moves</returns>
        private List<Vector2I> GetKingBasicMoves(King king, PieceInfo?[,] board)
        {
            var moves = new List<Vector2I>();
            
            var kingMoves = new Vector2I[]
            {
                new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1),
                new Vector2I(1, 1), new Vector2I(1, -1), new Vector2I(-1, 1), new Vector2I(-1, -1)
            };

            foreach (var move in kingMoves)
            {
                var targetPosition = king.BoardPosition + move;
                if (IsValidPosition(targetPosition.X, targetPosition.Y))
                {
                    var targetPiece = board[targetPosition.X, targetPosition.Y];
                    // Can move to empty square or capture enemy piece
                    if (!targetPiece.HasValue || targetPiece.Value.Color != king.Color)
                    {
                        moves.Add(targetPosition);
                    }
                }
            }
            
            return moves;
        }

        /// <summary>
        /// Checks if the king of the specified color is currently in check
        /// </summary>
        /// <param name="kingColor">Color of the king to check</param>
        /// <param name="board">Board state to check (optional, uses current board if null)</param>
        /// <returns>True if the king is in check</returns>
        public bool IsKingInCheck(PieceColor kingColor, PieceInfo?[,] board = null)
        {
            board ??= _board;
            
            // Find the king of the specified color
            Vector2I? kingPosition = null;
            for (int rank = 0; rank < BOARD_SIZE; rank++)
            {
                for (int file = 0; file < BOARD_SIZE; file++)
                {
                    var piece = board[rank, file];
                    if (piece.HasValue && piece.Value.Type == PieceType.King && piece.Value.Color == kingColor)
                    {
                        kingPosition = new Vector2I(rank, file);
                        break;
                    }
                }
                if (kingPosition.HasValue) break;
            }
            
            if (!kingPosition.HasValue)
            {
                GD.PrintErr($"Could not find {kingColor} king on the board!");
                return false;
            }
            
            // Check if the king's position is attacked by the enemy
            PieceColor enemyColor = kingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            return IsSquareAttacked(kingPosition.Value, enemyColor, board);
        }

        /// <summary>
        /// Checks if a move would put the moving player's king in check
        /// </summary>
        /// <param name="from">Source position</param>
        /// <param name="to">Destination position</param>
        /// <param name="playerColor">Color of the moving player</param>
        /// <returns>True if the move would result in check</returns>
        public bool WouldMoveResultInCheck(Vector2I from, Vector2I to, PieceColor playerColor)
        {
            // Create a copy of the board to simulate the move
            var boardCopy = GetBoardCopy();
            var piece = boardCopy[from.X, from.Y];
            
            if (!piece.HasValue)
                return false;
                
            // Simulate the move
            boardCopy[to.X, to.Y] = piece.Value;
            boardCopy[from.X, from.Y] = null;
            
            // Check if the king would be in check after this move
            return IsKingInCheck(playerColor, boardCopy);
        }
        #endregion

        #region Input Handling
 
        /// <summary>
        /// Clears the current selection
        /// </summary>
        public void ClearSelection()
        {
            GD.Print($"[ClearSelection] Called - Current selection: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
            
            if (_selectedPiece != null)
            {
                _selectedPiece.SetSelected(false);
                GD.Print($"[ClearSelection] Deselected piece: {_selectedPiece}");
                _selectedPiece = null;
            }
            _selectedSquare = null;
            ClearHighlights();
            
            GD.Print("[ClearSelection] Selection cleared and highlights removed");
        }
        #endregion

        #region Visual Highlighting
        /// <summary>
        /// Highlights a square with the specified color
        /// </summary>
        /// <param name="position">Position to highlight</param>
        /// <param name="color">Highlight color</param>
        public void HighlightSquare(Vector2I position, Color color)
        {
            GD.Print("HighlightSquare");
            if (!IsValidPosition(position.X, position.Y))
                return;

            var square = _squares[position.X, position.Y];
            square.Modulate = color;
            
            if (!_highlightedSquares.Contains(position))
                _highlightedSquares.Add(position);
        }

        /// <summary>
        /// Highlights multiple squares
        /// </summary>
        /// <param name="positions">Positions to highlight</param>
        /// <param name="color">Highlight color</param>
        public void HighlightSquares(List<Vector2I> positions, Color color)
        {
            foreach (var position in positions)
            {
                HighlightSquare(position, color);
            }
        }

        /// <summary>
        /// Clears all square highlights
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var position in _highlightedSquares)
            {
                if (IsValidPosition(position.X, position.Y))
                {
                    _squares[position.X, position.Y].Modulate = Colors.White;
                }
            }
            _highlightedSquares.Clear();
        }
        #endregion

        #region Game State Management
        /// <summary>
        /// Executes a move on the board
        /// </summary>
        /// <param name="from">Source position in algebraic notation</param>
        /// <param name="to">Destination position in algebraic notation</param>
        /// <returns>True if move was executed successfully</returns>
        public bool ExecuteMove(string from, string to)
        {
            try
            {
                var fromPos = AlgebraicToBoard(from);
                var toPos = AlgebraicToBoard(to);
                
                var piece = GetPieceAt(fromPos);
                if (!piece.HasValue)
                {
                    GD.PrintErr($"No piece at source position: {from}");
                    return false;
                }

                // Check for castling move
                bool isCastlingMove = false;
                Vector2I? rookFromPos = null;
                Vector2I? rookToPos = null;
                
                if (piece.Value.Type == PieceType.King && !piece.Value.HasMoved)
                {
                    int moveDistance = Math.Abs(toPos.Y - fromPos.Y);
                    if (moveDistance == 2) // King moved 2 squares horizontally = castling
                    {
                        isCastlingMove = true;
                        bool isKingside = toPos.Y > fromPos.Y; // Moving right = kingside
                        
                        if (isKingside)
                        {
                            // Kingside castling: rook moves from h-file to f-file
                            rookFromPos = new Vector2I(fromPos.X, 7);
                            rookToPos = new Vector2I(fromPos.X, 5);
                        }
                        else
                        {
                            // Queenside castling: rook moves from a-file to d-file
                            rookFromPos = new Vector2I(fromPos.X, 0);
                            rookToPos = new Vector2I(fromPos.X, 3);
                        }
                        
                        // Validate the rook exists and can castle
                        var rook = GetPieceAt(rookFromPos.Value);
                        if (!rook.HasValue || rook.Value.Type != PieceType.Rook || rook.Value.HasMoved)
                        {
                            GD.PrintErr($"Invalid castling move: rook not available at {BoardToAlgebraic(rookFromPos.Value.X, rookFromPos.Value.Y)}");
                            return false;
                        }
                    }
                }

                // Check for en passant capture
                bool isEnPassantCapture = false;
                Vector2I? enPassantCapturePos = null;
                
                if (piece.Value.Type == PieceType.Pawn && !string.IsNullOrEmpty(_enPassantTarget))
                {
                    var enPassantTargetPos = AlgebraicToBoard(_enPassantTarget);
                    if (toPos == enPassantTargetPos)
                    {
                        // This is an en passant capture
                        isEnPassantCapture = true;
                        // The captured pawn is on the same rank as the capturing pawn
                        enPassantCapturePos = new Vector2I(fromPos.X, toPos.Y);
                    }
                }

                // Update piece position
                var updatedPiece = piece.Value;
                updatedPiece.Position = toPos;
                updatedPiece.HasMoved = true;

                // Execute the move
                SetPieceAt(fromPos.X, fromPos.Y, null);
                SetPieceAt(toPos.X, toPos.Y, updatedPiece);
                
                // Handle castling rook movement
                if (isCastlingMove && rookFromPos.HasValue && rookToPos.HasValue)
                {
                    var rookPiece = GetPieceAt(rookFromPos.Value);
                    if (rookPiece.HasValue)
                    {
                        var updatedRook = rookPiece.Value;
                        updatedRook.Position = rookToPos.Value;
                        updatedRook.HasMoved = true;
                        
                        // Move the rook
                        SetPieceAt(rookFromPos.Value.X, rookFromPos.Value.Y, null);
                        SetPieceAt(rookToPos.Value.X, rookToPos.Value.Y, updatedRook);
                        
                        // Move the visual rook node
                        MovePieceNode(rookFromPos.Value, rookToPos.Value);
                    }
                }
                
                // Handle en passant capture (remove the captured pawn)
                if (isEnPassantCapture && enPassantCapturePos.HasValue)
                {
                    SetPieceAt(enPassantCapturePos.Value.X, enPassantCapturePos.Value.Y, null);
                    // Remove the visual piece node
                    var capturedPieceNode = _pieceNodes[enPassantCapturePos.Value.X, enPassantCapturePos.Value.Y];
                    if (capturedPieceNode != null)
                    {
                        capturedPieceNode.QueueFree();
                        _pieceNodes[enPassantCapturePos.Value.X, enPassantCapturePos.Value.Y] = null;
                    }
                }
                
                // Update visual pieces
                MovePieceNode(fromPos, toPos);
                
                // Update en passant target for next turn
                UpdateEnPassantTarget(piece.Value, fromPos, toPos);
                
                // Add to move history
                _moveHistory.Add($"{from}{to}");
                
                // Switch turns
                _currentPlayer = _currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                
                // Clear selection and highlights
                GD.Print($"[ExecuteMove] Clearing selection after successful move: {from} -> {to}");
                ClearSelection();
                
                // Emit signals
                EmitSignal(SignalName.MoveExecuted, from, to, piece.Value.ToNotation());
                EmitSignal(SignalName.GameStateChanged, CurrentPlayer.ToString(), false); // TODO: Check detection
                
                // Check if it's now Black's turn and trigger AI move
                if (_currentPlayer == PieceColor.Black && !_isWaitingForAI)
                {
                    GD.Print("White move completed - requesting AI move for Black");
                    RequestAIMove();
                }
                
                GD.Print($"[ExecuteMove] Move executed successfully: {from} -> {to} ({piece.Value.ToNotation()})");
                return true;
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error executing move {from} -> {to}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Moves a piece node visually from one square to another
        /// </summary>
        private void MovePieceNode(Vector2I from, Vector2I to)
        {
            var pieceNode = _pieceNodes[from.X, from.Y];
            if (pieceNode != null)
            {
                // Update piece position (centered in square)
                pieceNode.Position = BoardToScreen(to.X, to.Y, centerInSquare: true);
                pieceNode.BoardPosition = to;
                pieceNode.HasMoved = true;
                
                // Move in the array
                _pieceNodes[to.X, to.Y] = pieceNode;
                _pieceNodes[from.X, from.Y] = null;
            }
        }

        /// <summary>
        /// Updates the en passant target square based on the move
        /// </summary>
        /// <param name="piece">The piece that was moved</param>
        /// <param name="from">Source position</param>
        /// <param name="to">Destination position</param>
        private void UpdateEnPassantTarget(PieceInfo piece, Vector2I from, Vector2I to)
        {
            // Clear en passant target by default
            _enPassantTarget = null;
            
            // Set en passant target if a pawn made a double move
            if (piece.Type == PieceType.Pawn)
            {
                int moveDistance = System.Math.Abs(to.X - from.X);
                if (moveDistance == 2) // Double move
                {
                    // The en passant target is the square the pawn passed over
                    int targetRank = (from.X + to.X) / 2;
                    _enPassantTarget = BoardToAlgebraic(targetRank, to.Y);
                }
            }
        }

        /// <summary>
        /// Handles piece click events from the PieceClicked signal
        /// </summary>
        private void OnPieceClicked(ChessPiece piece)
        {
            GD.Print($"[OnPieceClicked] Piece clicked: {piece}");
            
            // Check if input was already handled by the ChessBoard's _Input method
            if (GetViewport().IsInputHandled())
            {
                GD.Print($"[OnPieceClicked] Input already handled by ChessBoard - skipping");
                return;
            }
            
            // Delegate to the square click handler for consistency
            GD.Print($"[OnPieceClicked] Delegating to HandleSquareClick for {piece.BoardPosition}");
            HandleSquareClick(piece.BoardPosition);
        }

        /// <summary>
        /// Selects a piece and shows its valid moves
        /// </summary>
        private void SelectPiece(ChessPiece piece)
        {
            GD.Print($"[SelectPiece] Selecting piece: {piece}");
            GD.Print($"[SelectPiece] Previous selection: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
            
            // Block selection if waiting for AI
            if (_isWaitingForAI)
            {
                GD.Print("[SelectPiece] Cannot select piece while waiting for AI move");
                return;
            }
            
            // Block selection if it's not White's turn
            if (_currentPlayer != PieceColor.White)
            {
                GD.Print("[SelectPiece] Cannot select piece - it's not White's turn");
                return;
            }
            
            // Only allow selecting white pieces (per requirements)
            if (piece.Color != PieceColor.White)
            {
                GD.Print("[SelectPiece] Cannot select non-white piece");
                return;
            }

            _selectedPiece = piece;
            _selectedSquare = piece.BoardPosition;
            
            // Highlight the selected piece
            piece.SetSelected(true);
            HighlightSquare(piece.BoardPosition, Colors.Yellow);
            
            // Show valid moves using the helper method
            var validMoves = GetValidMovesForSelectedPiece();
            GD.Print($"[SelectPiece] Showing {validMoves.Count} valid moves");
            HighlightSquares(validMoves, Colors.LightGreen);
            
            EmitSignal(SignalName.SquareClicked, piece.BoardPosition);
            GD.Print($"[SelectPiece] Piece selected successfully: {piece}");
        }

        /// <summary>
        /// Tries to execute a move between two positions
        /// </summary>
        private bool TryExecuteMove(Vector2I from, Vector2I to)
        {
            var fromAlgebraic = BoardToAlgebraic(from.X, from.Y);
            var toAlgebraic = BoardToAlgebraic(to.X, to.Y);
            
            GD.Print($"[TryExecuteMove] Attempting move: {fromAlgebraic} -> {toAlgebraic}");
            GD.Print($"[TryExecuteMove] Current selection before move: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
            
            // TODO: Add move validation here
            // For now, just execute any move
            bool result = ExecuteMove(fromAlgebraic, toAlgebraic);
            
            GD.Print($"[TryExecuteMove] Move result: {result}");
            GD.Print($"[TryExecuteMove] Current selection after move: {(_selectedPiece != null ? _selectedPiece.ToString() : "None")}");
            
            return result;
        }

        /// <summary>
        /// Resets the board to the initial state
        /// </summary>
        public void ResetBoard()
        {
            InitializeBoard();
            _moveHistory.Clear();
            _castleRights = CastleRights.Initial;
            _enPassantTarget = null;
            
            ClearSelection();
            CreatePieceNodes(); // Recreate visual pieces
            
            GD.Print("Chess board reset to initial position");
            EmitSignal(SignalName.GameStateChanged, CurrentPlayer.ToString(), false);
        }

        /// <summary>
        /// Gets the current board state for AI communication
        /// </summary>
        /// <returns>Board state dictionary</returns>
        public Dictionary<string, object> GetBoardStateForAI()
        {
            var stringBoard = BoardStateSerializer.ConvertToStringBoard(_board);
            return BoardStateSerializer.SerializeBoardToJson(
                stringBoard, 
                CurrentPlayer.ToString(), 
                _moveHistory, 
                _castleRights, 
                _enPassantTarget
            );
        }

        /// <summary>
        /// Gets the current board state as JSON string
        /// </summary>
        /// <returns>Board state as JSON</returns>
        public string GetBoardStateAsJson()
        {
            var stringBoard = BoardStateSerializer.ConvertToStringBoard(_board);
            return BoardStateSerializer.SerializeBoardToJsonString(
                stringBoard, 
                CurrentPlayer.ToString(), 
                _moveHistory, 
                _castleRights, 
                _enPassantTarget
            );
        }

        /// <summary>
        /// Prints the current board state to console
        /// </summary>
        public void PrintBoardState()
        {
            var boardString = BoardStateSerializer.SerializeBoardToString(_board);
            GD.Print("Current board state:");
            GD.Print(boardString);
            GD.Print($"To move: {CurrentPlayer}");
            GD.Print($"Move history: [{string.Join(", ", _moveHistory)}]");
        }

        #region AI Integration

        /// <summary>
        /// Handles AI move responses
        /// </summary>
        private void OnAIMoveReceived(string move)
        {
            GD.Print($"[OnAIMoveReceived] AI suggested move: {move}");
            
            if (!_isWaitingForAI)
            {
                GD.PrintErr("Received AI move when not waiting for AI");
                return;
            }

            _isWaitingForAI = false;
            
            // Parse and execute the AI move
            ProcessAIMove(move);
        }

        /// <summary>
        /// Handles AI errors
        /// </summary>
        private void OnAIError(string error)
        {
            GD.PrintErr($"[OnAIError] AI error: {error}");
            _isWaitingForAI = false;
            
            // For now, just log the error. Could implement retry logic or fallback moves
            GD.Print("AI failed to make a move - waiting for user input");
        }

        /// <summary>
        /// Requests an AI move for Black
        /// </summary>
        private async void RequestAIMove()
        {
            if (_isWaitingForAI)
            {
                GD.Print("Already waiting for AI move");
                return;
            }

            if (_aiService == null || !_aiService.IsReady())
            {
                GD.PrintErr("AI Service not ready");
                return;
            }

            if (_currentPlayer != PieceColor.Black)
            {
                GD.PrintErr("Requested AI move but it's not Black's turn");
                return;
            }

            _isWaitingForAI = true;
            GD.Print("Requesting AI move for Black...");
            
            try
            {
                var aiMove = await _aiService.GetBestMoveAsync(
                    _board, 
                    _moveHistory, 
                    _castleRights, 
                    _enPassantTarget
                );
                
                // The response will be handled by OnAIMoveReceived signal
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error requesting AI move: {ex.Message}");
                _isWaitingForAI = false;
            }
        }

        /// <summary>
        /// Processes and executes an AI move
        /// </summary>
        private void ProcessAIMove(string aiMove)
        {
            if (string.IsNullOrEmpty(aiMove))
            {
                GD.PrintErr("AI returned empty move - game may be over");
                HandleGameEnd("AI has no valid moves");
                return;
            }

            // Parse the AI move notation (could be various formats)
            var moveCoordinates = ParseAIMove(aiMove);
            
            if (moveCoordinates == null)
            {
                GD.PrintErr($"Failed to parse AI move: {aiMove}");
                return;
            }

            var (from, to) = moveCoordinates.Value;
            
            // Validate that this is a legal move
            if (!IsValidAIMove(from, to))
            {
                GD.PrintErr($"AI suggested invalid move: {aiMove} ({from} -> {to})");
                return;
            }

            // Execute the move
            var fromAlgebraic = BoardToAlgebraic(from.X, from.Y);
            var toAlgebraic = BoardToAlgebraic(to.X, to.Y);
            
            GD.Print($"Executing AI move: {fromAlgebraic} -> {toAlgebraic}");
            
            if (ExecuteMove(fromAlgebraic, toAlgebraic))
            {
                GD.Print("AI move executed successfully");
            }
            else
            {
                GD.PrintErr("Failed to execute AI move");
            }
        }

        /// <summary>
        /// Parses AI move notation to board coordinates
        /// </summary>
        private (Vector2I from, Vector2I to)? ParseAIMove(string move)
        {
            // Handle common chess move notations
            move = move.Trim();
            
            // Try to parse coordinate moves like "e7e5" or "e7-e5"
            var cleanMove = move.Replace("-", "").Replace(" ", "");
            
            if (cleanMove.Length >= 4)
            {
                try
                {
                    var fromStr = cleanMove.Substring(0, 2);
                    var toStr = cleanMove.Substring(2, 2);
                    
                    var from = AlgebraicToBoard(fromStr);
                    var to = AlgebraicToBoard(toStr);
                    
                    return (from, to);
                }
                catch (System.Exception)
                {
                    // Try other parsing methods if needed
                }
            }
            
            // TODO: Add support for standard algebraic notation (Nf3, Bxc4, etc.)
            // For now, return null if we can't parse
            GD.PrintErr($"Unable to parse move format: {move}");
            return null;
        }

        /// <summary>
        /// Validates that an AI move is legal
        /// </summary>
        private bool IsValidAIMove(Vector2I from, Vector2I to)
        {
            // Basic bounds checking
            if (!IsValidPosition(from.X, from.Y) || !IsValidPosition(to.X, to.Y))
                return false;
            
            // Check that there's a Black piece at the source
            var piece = GetPieceAt(from);
            if (!piece.HasValue || piece.Value.Color != PieceColor.Black)
                return false;
            
            // TODO: Add more comprehensive move validation
            // For now, we'll rely on the AI to suggest valid moves
            return true;
        }

        /// <summary>
        /// Handles game end conditions
        /// </summary>
        private void HandleGameEnd(string reason)
        {
            GD.Print($"Game ended: {reason}");
            // TODO: Implement proper game end handling (UI updates, etc.)
        }

        #endregion
        #endregion
    }
}
