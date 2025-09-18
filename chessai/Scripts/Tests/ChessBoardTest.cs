using Godot;
using ChessAI.Core;
using ChessAI.Pieces;
using System.Linq;

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
			TestEnPassant();
			TestCheckDetection();
			TestMoveValidation();
			
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
		//	_chessBoard.SelectSquare(new Vector2I(1, 4)); // e2 (white pawn)
			
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

		private void TestEnPassant()
		{
			GD.Print("Testing en passant logic...");
			
			// Reset board for clean test
			_chessBoard.ResetBoard();
			
			// Test pawn double move setting en passant target
			GD.Print("1. Testing pawn double move (e2->e4)");
			bool move1 = _chessBoard.ExecuteMove("e2", "e4");
			GD.Print($"   Move result: {move1}");
			GD.Print($"   En passant target: {_chessBoard.EnPassantTarget ?? "None"}");
			
			// Make a move for black (to change turn)
			GD.Print("2. Black moves (a7->a6)");
			bool move2 = _chessBoard.ExecuteMove("a7", "a6");
			GD.Print($"   Move result: {move2}");
			GD.Print($"   En passant target after black move: {_chessBoard.EnPassantTarget ?? "None"}");
			
			// Test another pawn double move
			GD.Print("3. White moves pawn double (d2->d4)");
			bool move3 = _chessBoard.ExecuteMove("d2", "d4");
			GD.Print($"   Move result: {move3}");
			GD.Print($"   En passant target: {_chessBoard.EnPassantTarget ?? "None"}");
			
			// Move black pawn to enable en passant
			GD.Print("4. Black moves (e7->e5)");
			bool move4 = _chessBoard.ExecuteMove("e7", "e5");
			GD.Print($"   Move result: {move4}");
			GD.Print($"   En passant target: {_chessBoard.EnPassantTarget ?? "None"}");
			
			// Test en passant capture opportunity
			GD.Print("5. Testing en passant moves for white pawn at d4");
			var whitePawn = _chessBoard.GetPieceAt(_chessBoard.AlgebraicToBoard("d4"));
			if (whitePawn.HasValue && whitePawn.Value.Type == PieceType.Pawn)
			{
				var pawn = new Pawn(whitePawn.Value.Color, whitePawn.Value.Position);
				var boardCopy = _chessBoard.GetBoardCopy();
				var enPassantMoves = pawn.GetValidMoves(boardCopy, _chessBoard.EnPassantTarget);
				GD.Print($"   Available moves: {enPassantMoves.Count}");
				foreach (var move in enPassantMoves)
				{
					string algebraic = _chessBoard.BoardToAlgebraic(move.X, move.Y);
					GD.Print($"      -> {algebraic}");
				}
			}
			
			GD.Print("En passant test completed!");
		}

		private void TestCheckDetection()
		{
			GD.Print("Testing check detection and castling safety...");
			
			// Test check detection with a simple scenario
			_chessBoard.ResetBoard();
			
			// Test if the white king is in check initially (should be false)
			bool whiteKingInCheck = _chessBoard.IsKingInCheck(PieceColor.White);
			GD.Print($"White king is in check: {whiteKingInCheck}");
			
			// Test if the black king is in check initially (should be false)
			bool blackKingInCheck = _chessBoard.IsKingInCheck(PieceColor.Black);
			GD.Print($"Black king is in check: {blackKingInCheck}");
			
			// Test if a square is attacked
			var e4Pos = _chessBoard.AlgebraicToBoard("e4");
			bool isE4AttackedByBlack = _chessBoard.IsSquareAttacked(e4Pos, PieceColor.Black);
			bool isE4AttackedByWhite = _chessBoard.IsSquareAttacked(e4Pos, PieceColor.White);
			GD.Print($"Square e4 is attacked by black: {isE4AttackedByBlack}");
			GD.Print($"Square e4 is attacked by white: {isE4AttackedByWhite}");
			
			// Test castling availability (should be possible initially)
			var whiteKingPos = _chessBoard.AlgebraicToBoard("e1");
			var king = _chessBoard.GetPieceAt(whiteKingPos);
			if (king.HasValue && king.Value.Type == PieceType.King)
			{
				var boardCopy = _chessBoard.GetBoardCopy();
				var kingScript = new King(king.Value.Color, king.Value.Position);
				var castlingMoves = kingScript.GetCastlingMoves(boardCopy, _chessBoard);
				GD.Print($"Available castling moves for white king: {castlingMoves.Count}");
				foreach (var move in castlingMoves)
				{
					string algebraic = _chessBoard.BoardToAlgebraic(move.X, move.Y);
					GD.Print($"   Castling move available: {algebraic}");
				}
			}
			
			GD.Print("Check detection test completed!");
		}

		private void TestMoveValidation()
		{
			GD.Print("Testing move validation to prevent own king check...");
			
			// Create a specific board scenario where a piece could expose its own king
			_chessBoard.ResetBoard();
			
			// Test scenario: Move a piece that would expose the king to check
			// We'll create a simple scenario where moving a defending piece would expose the king
			
			// Get initial valid moves for a piece (e.g., the pawn in front of the king)
			var e2Pos = _chessBoard.AlgebraicToBoard("e2");
			var e2Piece = _chessBoard.GetPieceAt(e2Pos);
			
			if (e2Piece.HasValue && e2Piece.Value.Type == PieceType.Pawn)
			{
				// Get valid moves using the board's move validation
				var boardCopy = _chessBoard.GetBoardCopy();
				var pawn = new Pawn(e2Piece.Value.Color, e2Piece.Value.Position);
				var allPossibleMoves = pawn.GetValidMoves(boardCopy);
				GD.Print($"Pawn at e2 has {allPossibleMoves.Count} possible moves before validation");
				
				// Test that the validation filters out moves that would expose the king
				int validMovesAfterValidation = 0;
				foreach (var move in allPossibleMoves)
				{
					if (!_chessBoard.WouldMoveResultInCheck(e2Pos, move, e2Piece.Value.Color))
					{
						validMovesAfterValidation++;
					}
				}
				GD.Print($"Pawn at e2 has {validMovesAfterValidation} valid moves after check validation");
			}
			
			// Test with other pieces
			var knightPos = _chessBoard.AlgebraicToBoard("b1");
			var knight = _chessBoard.GetPieceAt(knightPos);
			if (knight.HasValue && knight.Value.Type == PieceType.Knight)
			{
				var boardCopy = _chessBoard.GetBoardCopy();
				var knightPiece = new Knight(knight.Value.Color, knight.Value.Position);
				var knightMoves = knightPiece.GetValidMoves(boardCopy);
				
				int safeKnightMoves = knightMoves.Count(move => 
					!_chessBoard.WouldMoveResultInCheck(knightPos, move, knight.Value.Color));
				
				GD.Print($"Knight at b1 has {knightMoves.Count} possible moves, {safeKnightMoves} safe moves");
			}
			
			GD.Print("Move validation test completed!");
		}
	}
}
