using Godot;
using Flurl.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ChessAI.Models;

namespace ChessAI.AI
{
    /// <summary>
    /// HTTP client for communicating with Anthropic's Claude API
    /// </summary>
    public partial class AnthropicClient : Node
    {
        private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";
        private string? _apiKey;
        
        public override void _Ready()
        {
            // Get API key from keys.json file
            _apiKey = LoadApiKeyFromJson();
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                GD.PrintErr("Anthropic API key not found. Please check keys.json file in the project root.");
            }
            else
            {
                GD.Print("Anthropic API client initialized successfully.");
            }
        }

        /// <summary>
        /// Loads the Anthropic API key from keys.json file
        /// </summary>
        /// <returns>API key string or empty string if not found</returns>
        private string LoadApiKeyFromJson()
        {
            try
            {
                var keysPath = "res://keys.json";
                
                if (!FileAccess.FileExists(keysPath))
                {
                    GD.PrintErr($"Keys file not found at {keysPath}");
                    return string.Empty;
                }

                using var file = FileAccess.Open(keysPath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"Failed to open keys file at {keysPath}");
                    return string.Empty;
                }

                var jsonContent = file.GetAsText();
                var keysModel = JsonConvert.DeserializeObject<KeysModel>(jsonContent);
                
                if (keysModel == null)
                {
                    GD.PrintErr("Failed to parse keys.json file");
                    return string.Empty;
                }

                return keysModel.Anthropics;
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error loading API key from keys.json: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the AI's next move based on current board state
        /// </summary>
        /// <param name="boardState">String representation of the chess board</param>
        /// <param name="moveHistory">List of previous moves in algebraic notation</param>
        /// <returns>AI's move in algebraic notation, or null if error occurred</returns>
        public async Task<string?> GetAIMoveAsync(string boardState, List<string> moveHistory)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                GD.PrintErr("Cannot make API call: API key not configured");
                return null;
            }

            try
            {
                var prompt = BuildChessPrompt(boardState, moveHistory);
                
                var response = await ANTHROPIC_API_URL
                    .WithHeader("x-api-key", _apiKey)
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("anthropic-version", "2023-06-01")
                    .PostJsonAsync(new
                    {
                        model = "claude-3-haiku-20240307",
                        max_tokens = 150,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = prompt
                            }
                        }
                    });

                var responseData = await response.GetJsonAsync<AnthropicResponse>();
                var aiMove = ParseMoveFromResponse(responseData.Content[0].Text);
                
                GD.Print($"AI suggests move: {aiMove}");
                return aiMove;
            }
            catch (FlurlHttpException ex)
            {
                GD.PrintErr($"HTTP Error calling Anthropic API: {ex.Message}");
                if (ex.Call?.Response != null)
                {
                    var errorContent = await ex.Call.Response.GetStringAsync();
                    GD.PrintErr($"API Error Response: {errorContent}");
                }
                return null;
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error calling Anthropic API: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds the prompt for the chess AI
        /// </summary>
        private string BuildChessPrompt(string boardState, List<string> moveHistory)
        {
            var historyString = moveHistory.Count > 0 
                ? string.Join(", ", moveHistory) 
                : "Game start";

            return $@"You are playing chess as Black. Here is the current board state:

{boardState}

Move history: {historyString}

Respond with only your next move in algebraic notation (examples: 'e7e5', 'Nf6', 'O-O', 'Qd8xd1'). 
Choose a strong, tactical move. Consider:
- Controlling the center
- Developing pieces
- King safety
- Tactical opportunities (checks, captures, threats)

Respond with ONLY the move in algebraic notation, nothing else.";
        }

        /// <summary>
        /// Extracts the chess move from AI response text
        /// </summary>
        private string ParseMoveFromResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            // Clean up the response and extract the move
            var lines = responseText.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Look for valid chess move patterns (2-6 characters typically)
                if (trimmed.Length >= 2 && trimmed.Length <= 7)
                {
                    // Remove any quotes or extra characters
                    trimmed = trimmed.Trim('"', '\'', '.', '!', '?');
                    
                    // Basic validation for chess move format
                    if (IsValidMoveFormat(trimmed))
                    {
                        return trimmed;
                    }
                }
            }
            
            // Fallback: return the first non-empty line
            var fallback = responseText.Trim().Split('\n')[0].Trim();
            return fallback.Trim('"', '\'', '.', '!', '?');
        }

        /// <summary>
        /// Basic validation for chess move format
        /// </summary>
        private bool IsValidMoveFormat(string move)
        {
            if (string.IsNullOrEmpty(move)) return false;
            
            // Special moves
            if (move == "O-O" || move == "O-O-O") return true;
            
            // Basic algebraic notation patterns
            if (move.Length >= 2)
            {
                // Simple validation - contains file/rank characters
                return move.Contains('a') || move.Contains('b') || move.Contains('c') || 
                       move.Contains('d') || move.Contains('e') || move.Contains('f') || 
                       move.Contains('g') || move.Contains('h') ||
                       move.Contains('1') || move.Contains('2') || move.Contains('3') ||
                       move.Contains('4') || move.Contains('5') || move.Contains('6') ||
                       move.Contains('7') || move.Contains('8');
            }
            
            return false;
        }
    }

    /// <summary>
    /// Data classes for Anthropic API response
    /// </summary>
    public class AnthropicResponse
    {
        [JsonProperty("content")]
        public AnthropicContent[] Content { get; set; } = [];
    }

    public class AnthropicContent
    {
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }
}