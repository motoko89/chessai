using ChessAI.Pieces;
using Godot;
using System.Collections.Generic;

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
		private const float BOARD_OFFSET_Y = 30f; // Space for file labels (a-h)
		#endregion

		#region Private Fields
		private PieceInfo?[,] _board = new PieceInfo?[BOARD_SIZE, BOARD_SIZE];
		private PieceColor _currentPlayer = PieceColor.White;
		private List<string> _moveHistory = new();
		private CastleRights _castleRights = CastleRights.Initial;
		private string? _enPassantTarget = null;
		
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
			var area = new Area2D();
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
			};
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

			// Set piece position on screen
			piece.Position = BoardToScreen(pieceInfo.Position.X, pieceInfo.Position.Y);
			piece.HasMoved = pieceInfo.HasMoved;

			// Connect piece signals
			piece.PieceClicked += OnPieceClicked;

			return piece;
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
						var pieceNode = CreatePieceNode(pieceInfo.Value);
						_pieceNodes[rank, file] = pieceNode;
						_pieceContainer.AddChild(pieceNode);
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
		/// <returns>Screen position in pixels</returns>
		public Vector2 BoardToScreen(int rank, int file)
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

		/// <summary>
		/// Gets a copy of the current board state as strings (for backward compatibility)
		/// </summary>
		/// <returns>8x8 array copy of the board as strings</returns>
		public string?[,] GetStringBoardCopy()
		{
			return BoardStateSerializer.ConvertToStringBoard(_board);
		}
		#endregion

		#region Input Handling
		/// <summary>
		/// Handles square click events
		/// </summary>
		private void OnSquareClicked(InputEvent @event, Vector2I position)
		{
			if (@event is InputEventMouseButton mouseEvent && 
				mouseEvent.ButtonIndex == MouseButton.Left && 
				mouseEvent.Pressed)
			{
				GD.Print($"Square clicked: {BoardToAlgebraic(position.X, position.Y)} ({position.X}, {position.Y})");
				EmitSignal(SignalName.SquareClicked, position);
				SelectSquare(position);
			}
		}

		/// <summary>
		/// Selects a square and updates visual highlighting
		/// </summary>
		/// <param name="position">Position to select</param>
		public void SelectSquare(Vector2I position)
		{
			if (!IsValidPosition(position.X, position.Y))
				return;

			// Clear previous selection
			ClearHighlights();

			_selectedSquare = position;
			HighlightSquare(position, Colors.Yellow);
			
			GD.Print($"Selected square: {BoardToAlgebraic(position.X, position.Y)}");
		}

		/// <summary>
		/// Clears the current selection
		/// </summary>
		public void ClearSelection()
		{
			if (_selectedPiece != null)
			{
				_selectedPiece.SetSelected(false);
				_selectedPiece = null;
			}
			_selectedSquare = null;
			ClearHighlights();
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

				// Update piece position
				var updatedPiece = piece.Value;
				updatedPiece.Position = toPos;
				updatedPiece.HasMoved = true;

				// Execute the move
				SetPieceAt(fromPos.X, fromPos.Y, null);
				SetPieceAt(toPos.X, toPos.Y, updatedPiece);
				
				// Update visual pieces
				MovePieceNode(fromPos, toPos);
				
				// Add to move history
				_moveHistory.Add($"{from}{to}");
				
				// Switch turns
				_currentPlayer = _currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
				
				// Clear selection and highlights
				ClearSelection();
				
				// Emit signals
				EmitSignal(SignalName.MoveExecuted, from, to, piece.Value.ToNotation());
				EmitSignal(SignalName.GameStateChanged, CurrentPlayer.ToString(), false); // TODO: Check detection
				
				GD.Print($"Move executed: {from} -> {to} ({piece.Value.ToNotation()})");
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
				// Update piece position
				pieceNode.Position = BoardToScreen(to.X, to.Y);
				pieceNode.BoardPosition = to;
				pieceNode.HasMoved = true;
				
				// Move in the array
				_pieceNodes[to.X, to.Y] = pieceNode;
				_pieceNodes[from.X, from.Y] = null;
			}
		}

		/// <summary>
		/// Handles piece click events
		/// </summary>
		private void OnPieceClicked(ChessPiece piece)
		{
			GD.Print($"Piece clicked: {piece}");
			
			// If no piece is selected, select this piece
			if (_selectedPiece == null)
			{
				SelectPiece(piece);
			}
			// If clicking the same piece, deselect
			else if (_selectedPiece == piece)
			{
				ClearSelection();
			}
			// If clicking a different piece, try to move or select new piece
			else
			{
				if (TryExecuteMove(_selectedPiece.BoardPosition, piece.BoardPosition))
				{
					ClearSelection();
				}
				else
				{
					// If move failed, select the new piece instead
					ClearSelection();
					SelectPiece(piece);
				}
			}
		}

		/// <summary>
		/// Selects a piece and shows its valid moves
		/// </summary>
		private void SelectPiece(ChessPiece piece)
		{
			// Only allow selecting pieces of the current player
			if (piece.Color != _currentPlayer)
			{
				return;
			}

			_selectedPiece = piece;
			_selectedSquare = piece.BoardPosition;
			
			// Highlight the selected piece
			piece.SetSelected(true);
			HighlightSquare(piece.BoardPosition, Colors.Yellow);
			
			// Show valid moves
			var validMoves = piece.GetValidMoves(_board);
			HighlightSquares(validMoves, Colors.LightGreen);
			
			EmitSignal(SignalName.SquareClicked, piece.BoardPosition);
		}

		/// <summary>
		/// Tries to execute a move between two positions
		/// </summary>
		private bool TryExecuteMove(Vector2I from, Vector2I to)
		{
			var fromAlgebraic = BoardToAlgebraic(from.X, from.Y);
			var toAlgebraic = BoardToAlgebraic(to.X, to.Y);
			
			// TODO: Add move validation here
			// For now, just execute any move
			return ExecuteMove(fromAlgebraic, toAlgebraic);
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
		#endregion
	}
}
