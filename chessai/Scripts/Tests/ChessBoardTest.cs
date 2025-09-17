using Godot;
using ChessAI.Core;
using ChessAI.Pieces;

namespace ChessAI.Tests
{
	/// <summary>
	/// Simple test script to verify ChessBoard functionality
	/// </summary>
	public partial class ChessBoardTest : Node
	{
		private ChessBoard _chessBoard = new ChessBoard();

		public override void _Ready()
		{
			// Create and add chess board to scene
			_chessBoard = new ChessBoard();
			AddChild(_chessBoard);
			
			// Run basic tests
			TestCoordinateConversion();
			TestBoardInitialization();
			TestSquareSelection();
			
			GD.Print("ChessBoard tests completed successfully!");
		}

		private void TestCoordinateConversion()
		{
			GD.Print("Testing coordinate conversion...");
			
			// Test algebraic to board conversion
			var e4 = _chessBoard.AlgebraicToBoard("e4");
			GD.Print($"e4 converts to: rank={e4.X}, file={e4.Y}");
			
			// Test board to algebraic conversion
			var algebraic = _chessBoard.BoardToAlgebraic(3, 4); // Should be e4
			GD.Print($"(3,4) converts to: {algebraic}");
			
			// Test screen to board conversion
			var screenPos = new Vector2(400, 400);
			var boardPos = _chessBoard.ScreenToBoard(screenPos);
			if (boardPos.HasValue)
			{
				GD.Print($"Screen position {screenPos} -> Board position ({boardPos.Value.X}, {boardPos.Value.Y})");
			}
			
			// Test board to screen conversion
			var screenResult = _chessBoard.BoardToScreen(3, 4);
			GD.Print($"Board position (3,4) -> Screen position {screenResult}");
		}

		private void TestBoardInitialization()
		{
			GD.Print("Testing board initialization...");
			
			// Check if pieces are in correct starting positions
			var whiteKing = _chessBoard.GetPieceAt(0, 4);
			var blackKing = _chessBoard.GetPieceAt(7, 4);
			var whitePawn = _chessBoard.GetPieceAt(1, 4);
			var blackPawn = _chessBoard.GetPieceAt(6, 4);
			
			GD.Print($"White king at (0,4): {whiteKing}");
			GD.Print($"Black king at (7,4): {blackKing}");
			GD.Print($"White pawn at (1,4): {whitePawn}");
			GD.Print($"Black pawn at (6,4): {blackPawn}");
			
			// Print current board state
			_chessBoard.PrintBoardState();
		}

		private void TestSquareSelection()
		{
			GD.Print("Testing square selection...");
			
			// Connect to square clicked signal
			_chessBoard.SquareClicked += OnSquareClicked;
			
			// Test selecting a square programmatically
			_chessBoard.SelectSquare(new Vector2I(1, 4)); // e2 (white pawn)
			
			var selected = _chessBoard.SelectedSquare;
			if (selected.HasValue)
			{
				GD.Print($"Selected square: {_chessBoard.BoardToAlgebraic(selected.Value.X, selected.Value.Y)}");
			}
		}

		private void OnSquareClicked(Vector2I position)
		{
			var algebraic = _chessBoard.BoardToAlgebraic(position.X, position.Y);
			var piece = _chessBoard.GetPieceAt(position);
			var pieceString = piece?.ToNotation() ?? "empty";
			GD.Print($"Square clicked: {algebraic} - Piece: {pieceString}");
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventKey keyEvent && keyEvent.Pressed)
			{
				switch (keyEvent.Keycode)
				{
					case Key.R:
						GD.Print("Resetting board...");
						_chessBoard.ResetBoard();
						break;
					case Key.P:
						GD.Print("Printing board state...");
						_chessBoard.PrintBoardState();
						break;
					case Key.T:
						GD.Print("Testing move execution...");
						TestMoveExecution();
						break;
				}
			}
		}

		private void TestMoveExecution()
		{
			GD.Print("Executing test move: e2 -> e4");
			bool success = _chessBoard.ExecuteMove("e2", "e4");
			GD.Print($"Move execution result: {success}");
			
			if (success)
			{
				GD.Print($"Current player: {_chessBoard.CurrentPlayer}");
				GD.Print($"Move history: [{string.Join(", ", _chessBoard.MoveHistory)}]");
			}
		}
	}
}
