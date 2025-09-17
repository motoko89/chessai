using Godot;
using ChessAI.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessAI.AI
{
	/// <summary>
	/// Singleton service that manages AI interactions for the chess game
	/// </summary>
	public partial class AIService : Node
	{
		private static AIService? _instance;
		public static AIService? Instance => _instance;
		
		private AnthropicClient? _anthropicClient;
		private bool _isInitialized = false;

		[Signal]
		public delegate void AIMoveReceivedEventHandler(string move);
		
		[Signal]
		public delegate void AIErrorEventHandler(string error);

		public override void _Ready()
		{
			if (_instance == null)
			{
				_instance = this;
				InitializeAI();
				GD.Print("AIService singleton initialized");
			}
			else
			{
				// Prevent duplicate instances
				QueueFree();
			}
		}

		/// <summary>
		/// Initialize the AI client
		/// </summary>
		private void InitializeAI()
		{
			_anthropicClient = new AnthropicClient();
			AddChild(_anthropicClient);
			_isInitialized = true;
		}

		/// <summary>
		/// Gets the AI's best move for the current board position
		/// </summary>
		/// <param name="board">Current board state</param>
		/// <param name="moveHistory">List of previous moves</param>
		/// <param name="castleRights">Current castling rights</param>
		/// <param name="enPassant">En passant target square</param>
		/// <returns>AI's move in algebraic notation, or null if error</returns>
		public async Task<string?> GetBestMoveAsync(
			string?[,] board, 
			List<string> moveHistory,
			CastleRights? castleRights = null,
			string? enPassant = null)
		{
			if (!_isInitialized || _anthropicClient == null)
			{
				var error = "AI Service not properly initialized";
				GD.PrintErr(error);
				EmitSignal(SignalName.AIError, error);
				return null;
			}

			try
			{
				// Convert board to string format for AI
				var boardString = BoardStateSerializer.SerializeBoardToString(board);
				
				// Add game context information
				var contextInfo = BuildGameContext(castleRights, enPassant, moveHistory.Count);
				var fullBoardString = $"{boardString}\n{contextInfo}";
				
				GD.Print("Requesting AI move...");
				var aiMove = await _anthropicClient.GetAIMoveAsync(fullBoardString, moveHistory);
				
				if (!string.IsNullOrEmpty(aiMove))
				{
					EmitSignal(SignalName.AIMoveReceived, aiMove);
					return aiMove;
				}
				else
				{
					var error = "AI returned empty move";
					GD.PrintErr(error);
					EmitSignal(SignalName.AIError, error);
					return null;
				}
			}
			catch (System.Exception ex)
			{
				var error = $"Error getting AI move: {ex.Message}";
				GD.PrintErr(error);
				EmitSignal(SignalName.AIError, error);
				return null;
			}
		}

		/// <summary>
		/// Gets a quick AI move without extensive context (for faster responses)
		/// </summary>
		public async Task<string?> GetQuickMoveAsync(string?[,] board, List<string> moveHistory)
		{
			if (!_isInitialized || _anthropicClient == null)
				return null;

			var boardString = BoardStateSerializer.SerializeBoardToString(board);
			return await _anthropicClient.GetAIMoveAsync(boardString, moveHistory);
		}

		/// <summary>
		/// Builds additional game context information for the AI
		/// </summary>
		private string BuildGameContext(CastleRights? castleRights, string? enPassant, int moveCount)
		{
			var context = new List<string>();
			
			// Add move count
			context.Add($"Move #{moveCount + 1}");
			
			// Add castling information
			if (castleRights != null)
			{
				var castlingAvailable = new List<string>();
				if (castleRights.WhiteKingside) castlingAvailable.Add("White K-side");
				if (castleRights.WhiteQueenside) castlingAvailable.Add("White Q-side");
				if (castleRights.BlackKingside) castlingAvailable.Add("Black K-side");
				if (castleRights.BlackQueenside) castlingAvailable.Add("Black Q-side");
				
				if (castlingAvailable.Count > 0)
					context.Add($"Castling available: {string.Join(", ", castlingAvailable)}");
				else
					context.Add("No castling available");
			}
			
			// Add en passant information
			if (!string.IsNullOrEmpty(enPassant))
				context.Add($"En passant target: {enPassant}");
			
			return string.Join("\n", context);
		}

		/// <summary>
		/// Check if AI service is ready to make moves
		/// </summary>
		public bool IsReady()
		{
			return _isInitialized && _anthropicClient != null;
		}

		/// <summary>
		/// Get diagnostic information about the AI service
		/// </summary>
		public Dictionary<string, object> GetDiagnosticInfo()
		{
			return new Dictionary<string, object>
			{
				["initialized"] = _isInitialized,
				["anthropic_client_ready"] = _anthropicClient != null,
				["has_api_key"] = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
				["instance_ready"] = _instance != null
			};
		}

		public override void _ExitTree()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}
	}
}
