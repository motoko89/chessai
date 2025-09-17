using Godot;
using ChessAI.Pieces;
using ChessAI.Core;

namespace ChessAI.Scripts.UI
{
    /// <summary>
    /// Main game controller that manages the overall game state and coordinates between different components.
    /// This serves as the entry point for the chess game application.
    /// </summary>
    public partial class Main : Node2D
    {
        [Export] private ChessBoard _chessBoard;
        
        // UI elements
        private Label _gameStatusLabel;
        private Button _resetButton;
        
        // Game state variables
        private bool _gameActive = true;
        private PieceColor _currentPlayer = PieceColor.White;
        
        public override void _Ready()
        {
            // Initialize the game
            InitializeGame();
            
            GD.Print("Chess AI Game started!");
            GD.Print("Current player: " + _currentPlayer);
        }
        
        /// <summary>
        /// Initializes the game components and sets up the initial game state.
        /// </summary>
        private void InitializeGame()
        {
            // Find the ChessBoard node if not assigned in the editor
            if (_chessBoard == null)
            {
                _chessBoard = GetNode<ChessBoard>("ChessBoard");
            }
            
            // Find UI elements
            _gameStatusLabel = GetNode<Label>("UI/GameUI/TopPanel/GameStatus");
            _resetButton = GetNode<Button>("UI/GameUI/BottomPanel/ResetButton");
            
            // Connect UI signals
            if (_resetButton != null)
            {
                _resetButton.Pressed += ResetGame;
            }
            
            // Connect to chess board events if they exist
            if (_chessBoard != null)
            {
                // Set up the initial chess position
                _chessBoard.ResetBoard();
                
                GD.Print("Chess board initialized successfully");
            }
            else
            {
                GD.PrintErr("ChessBoard node not found! Make sure it's added to the Main scene.");
            }
            
            // Update UI
            UpdateGameStatus();
        }
        
        /// <summary>
        /// Handles game input and events.
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (!_gameActive) return;
            
            // Handle game-level input (like escape key for menu, R for reset, etc.)
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Escape:
                        // Handle escape key (could open menu)
                        GD.Print("Escape pressed - could open game menu");
                        break;
                        
                    case Key.R:
                        // Reset game
                        ResetGame();
                        break;
                }
            }
        }
        
        /// <summary>
        /// Switches the current player turn.
        /// </summary>
        public void SwitchTurn()
        {
            _currentPlayer = _currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
            GD.Print("Turn switched to: " + _currentPlayer);
            
            UpdateGameStatus();
            
            // If it's AI's turn (Black), trigger AI move
            if (_currentPlayer == PieceColor.Black)
            {
                // TODO: Implement AI move logic
                GD.Print("AI turn - implement AI logic here");
            }
        }
        
        /// <summary>
        /// Updates the game status display.
        /// </summary>
        private void UpdateGameStatus()
        {
            if (_gameStatusLabel != null)
            {
                if (_gameActive)
                {
                    _gameStatusLabel.Text = $"{_currentPlayer}'s Turn";
                }
                else
                {
                    _gameStatusLabel.Text = "Game Over";
                }
            }
        }
        
        /// <summary>
        /// Resets the game to the initial state.
        /// </summary>
        public void ResetGame()
        {
            GD.Print("Resetting game...");
            
            _gameActive = true;
            _currentPlayer = PieceColor.White;
            
            if (_chessBoard != null)
            {
                _chessBoard.ResetBoard();
            }
            
            UpdateGameStatus();
            
            GD.Print("Game reset complete");
        }
        
        /// <summary>
        /// Ends the current game.
        /// </summary>
        /// <param name="winner">The winning player color, or null for a draw</param>
        public void EndGame(PieceColor? winner)
        {
            _gameActive = false;
            
            string statusText;
            if (winner.HasValue)
            {
                statusText = $"Game Over! {winner.Value} Wins!";
                GD.Print($"Game Over! Winner: {winner.Value}");
            }
            else
            {
                statusText = "Game Over! Draw!";
                GD.Print("Game Over! It's a draw!");
            }
            
            if (_gameStatusLabel != null)
            {
                _gameStatusLabel.Text = statusText;
            }
        }
        
        /// <summary>
        /// Gets the current active player.
        /// </summary>
        public PieceColor GetCurrentPlayer() => _currentPlayer;
        
        /// <summary>
        /// Checks if the game is currently active.
        /// </summary>
        public bool IsGameActive() => _gameActive;
        
        /// <summary>
        /// Gets the chess board instance.
        /// </summary>
        public ChessBoard GetChessBoard() => _chessBoard;
    }
}