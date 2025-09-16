using Godot;
using ChessAI.Core;
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
		private const float BOARD_OFFSET_Y = 30f; // Space for file labels (a-h)
		private const float TOTAL_BOARD_SIZE = BOARD_SIZE * SQUARE_SIZE; // 512px
		#endregion

		#region Private Fields
		private string?[,] _board;
		private string _currentPlayer = "white";
		private List<string> _moveHistory = new();
		private CastleRights _castleRights = CastleRights.Initial;
		private string? _enPassantTarget = null;
		
		// Visual components
		private ColorRect[,] _squares = new ColorRect[BOARD_SIZE, BOARD_SIZE];
		private Node2D? _squareContainer;
		private Vector2I? _selectedSquare = null;
		private List<Vector2I> _highlightedSquares = new();
		#endregion

		#region Properties
		/// <summary>
		/// Gets the current player whose turn it is
		/// </summary>
		public string CurrentPlayer => _currentPlayer;
		
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

		#region Godot Lifecycle
		public override void _Ready()
		{
			InitializeBoard();
			CreateVisualBoard();
		}
		#endregion

		#region Initialization
		/// <summary>
		/// Initializes the chess board data structure
		/// </summary>
		private void InitializeBoard()
		{
			_board = BoardStateSerializer.CreateInitialBoard();
			GD.Print("Chess board initialized with starting position");
		}

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
			square.Color = isLightSquare ? Colors.BurlyWood : Colors.SaddleBrown;
			
			// Store reference and add to scene
			_squares[rank, file] = square;
			_squareContainer.AddChild(square);

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
		/// <returns>Piece string or null if empty</returns>
		public string? GetPieceAt(int rank, int file)
		{
			if (!IsValidPosition(rank, file)) 
				return null;
			
			return _board[rank, file];
		}

		/// <summary>
		/// Gets the piece at the specified position using Vector2I
		/// </summary>
		/// <param name="position">Board position</param>
		/// <returns>Piece string or null if empty</returns>
		public string? GetPieceAt(Vector2I position)
		{
			return GetPieceAt(position.X, position.Y);
		}

		/// <summary>
		/// Sets a piece at the specified position
		/// </summary>
		/// <param name="rank">Board rank (0-7)</param>
		/// <param name="file">Board file (0-7)</param>
		/// <param name="piece">Piece string or null for empty</param>
		public void SetPieceAt(int rank, int file, string? piece)
		{
			if (!IsValidPosition(rank, file))
			{
				GD.PrintErr($"Cannot set piece at invalid position: rank={rank}, file={file}");
				return;
			}

			_board[rank, file] = piece;
		}

		/// <summary>
		/// Gets a copy of the current board state
		/// </summary>
		/// <returns>8x8 array copy of the board</returns>
		public string?[,] GetBoardCopy()
		{
			var copy = new string?[BOARD_SIZE, BOARD_SIZE];
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
				if (string.IsNullOrEmpty(piece))
				{
					GD.PrintErr($"No piece at source position: {from}");
					return false;
				}

				// Execute the move
				SetPieceAt(fromPos.X, fromPos.Y, null);
				SetPieceAt(toPos.X, toPos.Y, piece);
				
				// Add to move history
				_moveHistory.Add($"{from}{to}");
				
				// Switch turns
				_currentPlayer = _currentPlayer == "white" ? "black" : "white";
				
				// Clear selection and highlights
				ClearSelection();
				
				// Emit signals
				EmitSignal(SignalName.MoveExecuted, from, to, piece);
				EmitSignal(SignalName.GameStateChanged, _currentPlayer, false); // TODO: Check detection
				
				GD.Print($"Move executed: {from} -> {to} ({piece})");
				return true;
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"Error executing move {from} -> {to}: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Resets the board to the initial state
		/// </summary>
		public void ResetBoard()
		{
			_board = BoardStateSerializer.CreateInitialBoard();
			_currentPlayer = "white";
			_moveHistory.Clear();
			_castleRights = CastleRights.Initial;
			_enPassantTarget = null;
			
			ClearSelection();
			
			GD.Print("Chess board reset to initial position");
			EmitSignal(SignalName.GameStateChanged, _currentPlayer, false);
		}

		/// <summary>
		/// Gets the current board state for AI communication
		/// </summary>
		/// <returns>Board state dictionary</returns>
		public Dictionary<string, object> GetBoardStateForAI()
		{
			return BoardStateSerializer.SerializeBoardToJson(
				_board, 
				_currentPlayer, 
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
			return BoardStateSerializer.SerializeBoardToJsonString(
				_board, 
				_currentPlayer, 
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
			GD.Print($"To move: {_currentPlayer}");
			GD.Print($"Move history: [{string.Join(", ", _moveHistory)}]");
		}
		#endregion
	}
}
