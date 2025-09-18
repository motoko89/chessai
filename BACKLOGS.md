# Chess AI Game - Project Backlog

## Project Overview
A Western Chess game built with Godot .NET/Mono and Anthropic AI backend for a 4-day hackathon.

### Core Requirements
- User starts as white
- Piece selection and highlighting
- Valid move destination highlighting
- AI opponent using Anthropic API for black moves
- Check/checkmate detection and display
- Game reset functionality

## 4-Day Development Schedule

### Day 1: Core Architecture & Board Setup

#### Task 1.1: Project Structure Setup
- [ ] Create folder structure for scripts, scenes, assets
- [ ] Set up C# project references and dependencies
- [ ] Configure HTTP client for Anthropic API
- [ ] Create main folder structure:
  ```
  Scripts/
  ├── Core/
  ├── Pieces/
  ├── AI/
  └── UI/
  Scenes/
  ├── Main/
  ├── Game/
  ├── UI/
  └── Pieces/
  Assets/
  ├── Sprites/
  ├── Sounds/
  └── Fonts/
  ```

#### Task 1.2: Board & Pieces Foundation
- [x] Create ChessBoard scene and script
- [x] Design piece representation system
- [x] Create base ChessPiece class
- [x] Implement 8x8 board coordinate system
- [x] Set up algebraic notation (a1-h8) coordinate mapping

#### Task 1.3: Basic UI Framework
- [x] Create main game scene
- [x] Design board visual representation (8x8 grid)
- [x] Set up basic piece sprites/models
- [x] Implement board square highlighting system

### Day 2: Game Logic & Piece Movement

#### Task 2.1: Chess Piece Logic
- [ ] Implement movement rules for each piece type:
  - [ ] Pawn (forward movement, diagonal capture, en passant)
  - [ ] Rook (horizontal/vertical movement)
  - [ ] Knight (L-shaped movement)
  - [ ] Bishop (diagonal movement)
  - [ ] Queen (combination of rook + bishop)
  - [ ] King (one square in any direction, castling)
- [ ] Create piece-specific classes inheriting from ChessPiece
- [ ] Implement basic move validation system

#### Task 2.2: Player Interaction
- [ ] Piece selection system (click/tap detection) for user, which is white pieces only
- [ ] Highlight selected pieces visually: 4px thick golden square around the board square
- [ ] Show valid move destinations with highlighting: 4px thick golden square around the board squares
- [ ] Implement move execution: click-to-move, valid destinations only
- [ ] Add simple move animation: move the piece from start square to end square horizontally first, then vertically/feedback

### Day 3: AI Integration & Game State

#### Task 3.1: Anthropic API Integration
- [ ] Design board state schema for API communication
- [ ] Implement HTTP client for Anthropic API calls
- [ ] Create board state serialization (JSON format)
- [ ] Parse AI responses and convert to valid moves
- [ ] Add error handling for API failures
- [ ] Implement AI move validation

#### Task 3.2: Game State Management
- [ ] Turn management system (White/Black alternation)
- [ ] Move history tracking and storage
- [ ] Check detection algorithm
- [ ] Checkmate detection algorithm
- [ ] Stalemate detection
- [ ] Game over conditions and handling

### Day 4: Polish & Features

#### Task 4.1: Advanced Features
- [ ] Special moves implementation:
  - [ ] Castling (kingside/queenside)
  - [ ] En passant capture
  - [ ] Pawn promotion
- [ ] Game reset functionality
- [ ] UI improvements and user feedback
- [ ] Move sound effects
- [ ] Game status display (check, checkmate, turn indicator)

#### Task 4.2: Testing & Debugging
- [ ] Test all piece movement rules
- [ ] Verify AI integration works correctly
- [ ] Test edge cases (checkmate scenarios, stalemate)
- [ ] Performance optimization
- [ ] Bug fixes and polish
- [ ] User experience improvements
- [ ] Implement King's IsInCheck

## Technical Architecture

### Scene Organization Structure
```
Main.tscn (Main game controller)
├── UI/
│   ├── GameBoard.tscn (Visual board representation)
│   ├── GameUI.tscn (Buttons, status, etc.)
│   └── PieceSprite.tscn (Reusable piece visual)
├── Game/
│   ├── ChessGame.tscn (Game logic controller)
│   └── ChessPiece.tscn (Base piece scene)
└── Pieces/ (Individual piece scenes if needed)
    ├── Pawn.tscn
    ├── Rook.tscn
    ├── Knight.tscn
    ├── Bishop.tscn
    ├── Queen.tscn
    └── King.tscn
```

### Key Classes & Responsibilities

#### ChessGame (Singleton/Main Controller)
- Manages overall game state
- Handles turn switching
- Coordinates between UI and logic
- Manages AI integration

#### ChessBoard (Data Model)
- 8x8 array representation
- Position validation
- Move execution and history
- Board state serialization

#### ChessPiece (Base Class)
- Common properties (color, position, type)
- Abstract move validation methods
- Visual representation coordination

#### Specific Piece Classes
- Inherit from ChessPiece
- Override movement rule methods
- Handle piece-specific special cases

## Data Schemas

### Board State for Anthropic API
```json
{
  "board": [
    ["r", "n", "b", "q", "k", "b", "n", "r"],
    ["p", "p", "p", "p", "p", "p", "p", "p"],
    [null, null, null, null, null, null, null, null],
    [null, null, null, null, null, null, null, null],
    [null, null, null, null, null, null, null, null],
    [null, null, null, null, null, null, null, null],
    ["P", "P", "P", "P", "P", "P", "P", "P"],
    ["R", "N", "B", "Q", "K", "B", "N", "R"]
  ],
  "toMove": "black",
  "castleRights": {
    "whiteKingside": true,
    "whiteQueenside": true,
    "blackKingside": true,
    "blackQueenside": true
  },
  "enPassant": null,
  "moveHistory": ["e2e4", "e7e5"]
}
```

### AI Response Schema
```json
{
  "move": {
    "from": "e7",
    "to": "e5",
    "piece": "p",
    "capture": false,
    "promotion": null
  },
  "confidence": 0.85,
  "reasoning": "Responding to e4 with e5, controlling center"
}
```

## Implementation Guidelines

### Coordinate System
- Use algebraic notation (a1-h8) internally
- File: 0-7 (a-h), Rank: 0-7 (1-8)
- Convert between screen coordinates and board positions

### Piece Representation
- PieceType enum: Pawn, Rook, Knight, Bishop, Queen, King
- PieceColor enum: White, Black
- Lowercase letters for black pieces, uppercase for white

### Move Validation Pattern
1. Check piece-specific movement rules
2. Check path is clear (for sliding pieces)
3. Check destination square validity
4. Verify move doesn't put own king in check

### Error Handling
- Network failures for AI calls
- Invalid move attempts
- API response parsing errors
- Game state corruption recovery

## Stretch Goals (If Time Permits)
- [ ] Move animation improvements
- [ ] Game statistics tracking
- [ ] Multiple difficulty levels for AI
- [ ] Save/load game functionality
- [ ] Piece capture animation
- [ ] Better visual feedback and UI polish
- [ ] Mobile-friendly touch controls
- [ ] Game replay functionality

## Technical Debt & Known Issues
- Document any shortcuts taken during development
- Note areas needing refactoring
- Performance bottlenecks to address
- Code quality improvements needed

---

**Last Updated:** September 15, 2025  
**Project Duration:** 4 days  
**Target Platform:** Desktop (with potential mobile support)  
**Technology Stack:** Godot 4.4, C#/.NET, Anthropic API