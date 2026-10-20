// ============================================================================
// USING DIRECTIVES - Required namespaces for the Sudoku game
// ============================================================================
using Microsoft.Data.SqlClient;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using SudokuProject3.Aestetics;
using SudokuProject3.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SudokuProject3
{
    // ============================================================================
    // MAIN GAME CLASS - Core game logic and state management
    // ============================================================================
    public class Game1 : Game
    {
        // ============================================================================
        // DATABASE CONNECTION
        // ============================================================================
        // Connection string for local SQL Server database storing user accounts, settings, best times, and leaderboard data
        private const string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=SudokuDatabase;Integrated Security=True";

        // ============================================================================
        // GRAPHICS AND RENDERING
        // ============================================================================
        private GraphicsDeviceManager _graphics;      // Manages graphics device and settings
        private SpriteBatch _spriteBatch;             // Batch renderer for 2D sprites
        private Color _backgroundColor = Color.White; // Background color for all screens

        // ============================================================================
        // GAME GRID DATA STRUCTURES
        // ============================================================================
        // 9x9 grid of Cell objects representing the Sudoku board
        private Cell[,] Cell = new Cell[9, 9];

        // 9x9x3x3 array storing note objects for each cell
        // Each cell can have up to 9 notes arranged in a 3x3 grid
        private Note[,,,] Notes = new Note[9, 9, 3, 3];

        // Tracks which cells were revealed using the hint feature
        // Used for undo functionality and hint limit tracking
        private bool[,] _hintRevealedCells = new bool[9, 9];

        // ============================================================================
        // UI COMPONENT LISTS - Each scene has its own list of drawable/updatable components
        // ============================================================================
        private List<Component> _gameComponents;          // Main game screen components
        private List<Component> _solverComponents;        // Sudoku solver screen components
        private List<Component> _difficultyComponents;    // Difficulty selection screen components
        private List<Component> _mainMenuScreenComponents; // Main menu components
        private List<Component> _winnerComponents;        // Victory screen components
        private List<Component> _leaderboardComponents;   // Leaderboard screen components
        private List<Component> _createaccountComponents; // Account creation screen components
        private List<Component> _signinaccountComponents; // Sign-in screen components
        private List<Component> _informationComponents;   // Information/help screen components
        private List<Component> _pauseGameComponents;     // Pause menu (game) components
        private List<Component> _pauseSolverComponents;   // Pause menu (solver) components
        private List<Component> _settingsComponents;      // Settings screen components
        private List<Component> _youLostComponents;       // Game over screen components

        // ============================================================================
        // GAME STATE VARIABLES
        // ============================================================================
        private string[] _difficulties = new string[5] { "Easy", "Medium", "Hard", "Expert", "Master" };
        private Cell _selectedCell = null;        // Currently selected cell on the grid
        private Random _random = new Random();    // Random number generator for puzzle generation
        private int _difficulty;                  // Current difficulty level (1-5)
        private Scenes _currentScene;             // Current active scene/screen
        private double elapsedTime;               // Time elapsed in current game (seconds)
        private Color _textColour = new Color(0x87, 0xB2, 0xAB); // Standard text color (teal)
        private int _cellsCorrect = 0;            // Number of correctly filled cells
        private bool _notesMode = false;          // Whether notes mode is currently active
        private int DifficultyIndex = 0;          // Index for difficulty selection carousel
        private int FinalTime = 0;                // Final completion time for completed game
        private TextInput currentTextBox;         // Currently focused text input box

        // ============================================================================
        // USER ACCOUNT VARIABLES
        // ============================================================================
        private string UserID = "";               // Logged-in username
        private string Password = "";             // User's password (hashed when stored)

        // ============================================================================
        // UI STATE VARIABLES
        // ============================================================================
        private int LeaderboardDifficulty = 1;    // Currently displayed leaderboard difficulty
        private int InformationIndex = 1;         // Currently displayed information tab
        private int SettingsIndex = 1;            // Currently displayed settings tab

        // ============================================================================
        // INTERACTIVE UI BUTTONS - Stored as fields for dynamic updates
        // ============================================================================
        private Button ButtonNote;                // Toggle notes mode button
        private bool _previousNotesMode = false;  // Previous state of notes mode (for texture updates)
        private bool _SideBarOn = true;           // Whether sidebar is expanded or collapsed
        private Button ButtonSideBarArrowLeft;    // Collapse sidebar button
        private Button ButtonSideBarArrowRight;   // Expand sidebar button
        private Button ButtonHint;                // Hint button
        private Button ButtonEraser;              // Eraser button
        private Button ButtonUndo;                // Undo button
        private Button ButtonRedo;                // Redo button
        private NonMoving SideBar;                // Sidebar visual element

        // ============================================================================
        // UNDO/REDO SYSTEM
        // ============================================================================
        // Stacks storing cell states for undo/redo functionality
        private Stack<CellState> _undoStack = new Stack<CellState>();
        private Stack<CellState> _redoStack = new Stack<CellState>();

        // ============================================================================
        // SCENE MANAGEMENT
        // ============================================================================
        private Scenes _previousScene;            // Previous scene (for back navigation)

        // ============================================================================
        // GAME SETTINGS - Toggleable features
        // ============================================================================
        private bool HintLimit = false;           // Limit hints to 3 per game
        private bool LivesLimit = false;          // Enable 3-strikes game over
        private bool ShowTimer = true;            // Display game timer
        private bool MistakeHighlighter = false;  // Highlight incorrect entries in red
        private bool MusicOn = true;              // Background music enabled
        private bool SoundEffectsOn = true;       // Sound effects enabled

        // ============================================================================
        // GAME STATISTICS
        // ============================================================================
        private int _hintsUsed = 0;               // Hints used in current game
        private int _mistakesMade = 0;            // Mistakes made in current game (for lives system)

        // ============================================================================
        // SETTINGS UI SWITCHES - Stored as fields for texture updates
        // ============================================================================
        Button hintLimitSwitch = null;
        Button livesLimitSwitch = null;
        Button mistakeHighlighterSwitch = null;
        Button showTimerSwitch = null;
        Button musicSwitch = null;
        Button soundEffectsSwitch = null;

        // Texture paths for switch states
        private string hintSwitchTexture = "Controls/SettingsScreen/OffSwitch";
        private string livesSwitchTexture = "Controls/SettingsScreen/OffSwitch";
        private string mistakeSwitchTexture = "Controls/SettingsScreen/OffSwitch";
        private string timerSwitchTexture = "Controls/SettingsScreen/OffSwitch";
        private string musicSwitchTexture = "Controls/SettingsScreen/OffSwitch";
        private string soundSwitchTexture = "Controls/SettingsScreen/OffSwitch";

        // ============================================================================
        // USER BEST TIMES - Display strings for personal records
        // ============================================================================
        private string UserBestTimeEasy = "--:--:--";
        private string UserBestTimeMedium = "--:--:--";
        private string UserBestTimeHard = "--:--:--";
        private string UserBestTimeExpert = "--:--:--";
        private string UserBestTimeMaster = "--:--:--";

        // ============================================================================
        // ERROR MESSAGING SYSTEM
        // ============================================================================
        private string ErrorMessage = "";         // Current error message to display
        private NonMoving ErrorBox;               // Error dialog box visual
        private Button OkButton;                  // OK button for dismissing errors

        // ============================================================================
        // AUDIO ASSETS
        // ============================================================================
        private SoundEffect clickSound;           // UI button click sound
        private SoundEffect bubbleSound;          // Bubble/soft interaction sound
        private SoundEffect winSound;             // Victory sound
        private SoundEffect errorSound;           // Error/mistake sound

        // ============================================================================
        // CONSTRUCTOR - Initializes graphics settings and content directory
        // ============================================================================
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920;  // Set window width
            _graphics.PreferredBackBufferHeight = 1080; // Set window height
            _graphics.IsFullScreen = false;             // Windowed mode
            Content.RootDirectory = "Content";          // Content folder location
            IsMouseVisible = true;                      // Show mouse cursor
            _currentScene = Scenes.MainMenuScreen;      // Start at main menu
        }

        // ============================================================================
        // INITIALIZE - Called once at game start, sets up text input handler
        // ============================================================================
        protected override void Initialize()
        {
            IsMouseVisible = true;
            base.Initialize();
            // Register handler for keyboard text input (for text boxes)
            Window.TextInput += TextInputHandler;
        }

        // ============================================================================
        // LOAD CONTENT - Loads all game assets and initializes UI components
        // This is the largest method and sets up the entire game UI
        // ============================================================================
        protected override void LoadContent()
        {
            // ============================================================================
            // LOAD FONTS
            // ============================================================================
            SpriteFont ErrorFont = Content.Load<SpriteFont>("Fonts/BubbleFont");

            // ============================================================================
            // LOAD AND START BACKGROUND MUSIC
            // ============================================================================
            Song song;
            song = Content.Load<Song>("Aestetics/LofiGirlMusic");
            clickSound = Content.Load<SoundEffect>("Aestetics/ButtonClick");
            errorSound = Content.Load<SoundEffect>("Aestetics/ErrorSound");
            bubbleSound = Content.Load<SoundEffect>("Aestetics/BubbleSound");
            winSound = Content.Load<SoundEffect>("Aestetics/WinSound");

            // Start music if enabled
            if (MusicOn)
            {
                MediaPlayer.Play(song);
                MediaPlayer.IsRepeating = true;
            }

            // ============================================================================
            // CREATE TRANSPARENT BUTTON TEXTURES - Used for invisible clickable areas
            // ============================================================================

            // Leaderboard difficulty tab buttons
            Texture2D LeaderboardClearButton = new Texture2D(GraphicsDevice, 324, 127);
            Color[] leaderboarddata = new Color[324 * 127];
            for (int i = 0; i < leaderboarddata.Length; ++i)
            {
                leaderboarddata[i] = new Color(0, 0, 0, 0); // Fully transparent
            }
            LeaderboardClearButton.SetData(leaderboarddata);

            // Information screen tab buttons
            Texture2D InformationClearButton = new Texture2D(GraphicsDevice, 324, 93);
            Color[] informationdata = new Color[324 * 93];
            for (int i = 0; i < informationdata.Length; ++i)
            {
                informationdata[i] = new Color(0, 0, 0, 0);
            }
            InformationClearButton.SetData(informationdata);

            // Side menu buttons
            Texture2D SideMenuClearButton = new Texture2D(GraphicsDevice, 571, 126);
            Color[] sidemenudata = new Color[571 * 126];
            for (int i = 0; i < sidemenudata.Length; ++i)
            {
                sidemenudata[i] = new Color(0, 0, 0, 0);
            }
            SideMenuClearButton.SetData(sidemenudata);

            // ============================================================================
            // INITIALIZE SPRITE BATCH AND COMPONENT LISTS
            // ============================================================================
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameComponents = new List<Component>() { };
            _solverComponents = new List<Component>() { };
            _mainMenuScreenComponents = new List<Component>() { };
            _difficultyComponents = new List<Component>() { };
            _winnerComponents = new List<Component>() { };
            _leaderboardComponents = new List<Component>() { };
            _createaccountComponents = new List<Component>() { };
            _signinaccountComponents = new List<Component>() { };
            _informationComponents = new List<Component>() { };
            _pauseGameComponents = new List<Component>() { };
            _pauseSolverComponents = new List<Component>() { };
            _settingsComponents = new List<Component>() { };
            _youLostComponents = new List<Component>() { };

            // ============================================================================
            // BACKGROUND IMAGES - Create background NonMoving components for each screen
            // ============================================================================

            var MainScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/MainScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _mainMenuScreenComponents.Add(MainScreenBackground);

            var DifficultyScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/DifficultyScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _difficultyComponents.Add(DifficultyScreenBackground);

            var MainGameScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/MainGameScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _gameComponents.Add(MainGameScreenBackground);
            _solverComponents.Add(MainGameScreenBackground);

            var PauseScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/PauseScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _pauseGameComponents.Add(PauseScreenBackground);

            var PauseSolverScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/PauseSolverScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _pauseSolverComponents.Add(PauseSolverScreenBackground);

            var WinnerScreenBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/WinnerScreenBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _winnerComponents.Add(WinnerScreenBackground);

            var SignInBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/SignInBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _signinaccountComponents.Add(SignInBackground);

            var CreateAccountBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/CreateAccountBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _createaccountComponents.Add(CreateAccountBackground);

            // Leaderboard backgrounds for each difficulty
            var LeaderBoardEasyBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/LeaderBoardEasyBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _leaderboardComponents.Add(LeaderBoardEasyBackground);

            var LeaderBoardMediumBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/LeaderBoardMediumBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _leaderboardComponents.Add(LeaderBoardMediumBackground);

            var LeaderBoardHardBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/LeaderBoardHardBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _leaderboardComponents.Add(LeaderBoardHardBackground);

            var LeaderBoardExpertBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/LeaderBoardExpertBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _leaderboardComponents.Add(LeaderBoardExpertBackground);

            var LeaderBoardMasterBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/LeaderBoardMasterBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _leaderboardComponents.Add(LeaderBoardMasterBackground);

            // Information screen backgrounds
            var HowToPlayBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/HowToPlayBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _informationComponents.Add(HowToPlayBackground);

            var TipsTricksBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/Tips&TricksBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _informationComponents.Add(TipsTricksBackground);

            var ControlsBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/ControlsBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _informationComponents.Add(ControlsBackground);

            // Settings screen backgrounds
            var SettingsGameBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/SettingsGameBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _settingsComponents.Add(SettingsGameBackground);

            var SettingsAccountBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/SettingsAccountBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _settingsComponents.Add(SettingsAccountBackground);

            var YouLostBackground = new NonMoving(Content.Load<Texture2D>("Aestetics/YouLostBackground"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(0, 0)
            };
            _youLostComponents.Add(YouLostBackground);

            // ============================================================================
            // SIDEBAR - Shared between game and solver screens
            // ============================================================================
            SideBar = new NonMoving(Content.Load<Texture2D>("Aestetics/SideBar"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(1025, 108)
            };
            _solverComponents.Add(SideBar);
            _gameComponents.Add(SideBar);

            // ============================================================================
            // SIDE MENU BUTTON - Opens pause menu
            // ============================================================================
            var SideMenuButton = new Button(Content.Load<Texture2D>("Controls/GameScreen/SideMenuButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1791, 154),
            };
            SideMenuButton.Click += (sender, e) =>
            {
                _previousScene = _currentScene;
                // Navigate to appropriate pause screen based on current scene
                if (_currentScene == Scenes.GameScreen)
                {
                    _currentScene = Scenes.PauseGameScreen;
                }
                else
                {
                    _currentScene = Scenes.PauseSolverScreen;
                }

            };
            _gameComponents.Add(SideMenuButton);
            _solverComponents.Add(SideMenuButton);

            // ============================================================================
            // PAUSE MENU BACK BUTTON - Returns to previous screen
            // ============================================================================
            var SideMenuArrowButton = new Button(Content.Load<Texture2D>("Controls/PauseScreen/SideMenuArrowButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1138, 39),
            };
            SideMenuArrowButton.Click += (sender, e) =>
            {
                _currentScene = _previousScene;
            };
            _pauseGameComponents.Add(SideMenuArrowButton);
            _pauseSolverComponents.Add(SideMenuArrowButton);

            // ============================================================================
            // NUMBER INPUT BUTTONS (1-9) - Used for entering numbers in cells
            // Each button is positioned in a 3x3 grid layout on the sidebar
            // ============================================================================

            var NumberButton1 = new Button(Content.Load<Texture2D>("Controls/Button1"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1269, 278),
                Value = 1
            };
            NumberButton1.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton1);
            _solverComponents.Add(NumberButton1);

            var NumberButton2 = new Button(Content.Load<Texture2D>("Controls/Button2"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1488, 278),
                Value = 2
            };
            NumberButton2.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton2);
            _solverComponents.Add(NumberButton2);

            var NumberButton3 = new Button(Content.Load<Texture2D>("Controls/Button3"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1706, 278),
                Value = 3
            };
            NumberButton3.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton3);
            _solverComponents.Add(NumberButton3);

            var NumberButton4 = new Button(Content.Load<Texture2D>("Controls/Button4"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1269, 495),
                Value = 4
            };
            NumberButton4.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton4);
            _solverComponents.Add(NumberButton4);

            var NumberButton5 = new Button(Content.Load<Texture2D>("Controls/Button5"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1488, 495),
                Value = 5
            };
            NumberButton5.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton5);
            _solverComponents.Add(NumberButton5);


            var NumberButton6 = new Button(Content.Load<Texture2D>("Controls/Button6"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1706, 495),
                Value = 6
            };
            NumberButton6.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton6);
            _solverComponents.Add(NumberButton6);

            var NumberButton7 = new Button(Content.Load<Texture2D>("Controls/Button7"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1274, 711),
                Value = 7
            };
            NumberButton7.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton7);
            _solverComponents.Add(NumberButton7);

            var NumberButton8 = new Button(Content.Load<Texture2D>("Controls/Button8"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1488, 711),
                Value = 8
            };
            NumberButton8.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton8);
            _solverComponents.Add(NumberButton8);

            var NumberButton9 = new Button(Content.Load<Texture2D>("Controls/Button9"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1706, 711),
                Value = 9
            };
            NumberButton9.Click += NumberButton_Click;
            _gameComponents.Add(NumberButton9);
            _solverComponents.Add(NumberButton9);

            // ============================================================================
            // ERASER BUTTON - Clears the selected cell's value
            // ============================================================================
            ButtonEraser = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonEraser"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1039, 720)
            };
            ButtonEraser.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                if (_selectedCell != null)
                {
                    // Save current note state before clearing
                    bool[,] currentNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column);

                    // Save state for undo functionality (now includes note clearing)
                    CellState state = new CellState
                    {
                        Row = _selectedCell._row,
                        Column = _selectedCell._column,
                        PreviousInput = _selectedCell.Input,
                        NewInput = 0,
                        PreviousNotes = currentNotes,
                        NewNotes = new bool[3, 3] 
                    };
                    _undoStack.Push(state);
                    _redoStack.Clear();

                    // Decrement correct cells counter if erasing a correct value
                    if (_selectedCell.Input == _selectedCell.Value)
                    {
                        _cellsCorrect--;
                    }

                    // Clear the cell input
                    _selectedCell.Input = 0;
                    _selectedCell.IsIncorrect = false;

                    // Clear all notes in the cell
                    for (int nrow = 0; nrow < 3; nrow++)
                    {
                        for (int ncol = 0; ncol < 3; ncol++)
                        {
                            Notes[_selectedCell._row, _selectedCell._column, nrow, ncol].NoteVisible = false;
                        }
                    }
                }
            };

            // ============================================================================
            // UNDO BUTTON - Reverts the last action
            // ============================================================================
            ButtonUndo = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonUndo"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1039, 355)
            };
            ButtonUndo.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                if (_undoStack.Count > 0)
                {
                    // Pop state from undo stack
                    CellState state = _undoStack.Pop();

                    // Create corresponding redo state
                    CellState redoState = new CellState
                    {
                        Row = state.Row,
                        Column = state.Column,
                        PreviousInput = state.NewInput,
                        NewInput = state.PreviousInput,
                        PreviousNotes = state.NewNotes,
                        NewNotes = state.PreviousNotes
                    };
                    _redoStack.Push(redoState);

                    // Update cell correct counter
                    if (Cell[state.Row, state.Column].Input == Cell[state.Row, state.Column].Value &&
                        Cell[state.Row, state.Column].Input != 0)
                    {
                        _cellsCorrect--;
                    }

                    // Restore previous state
                    Cell[state.Row, state.Column].Input = state.PreviousInput;
                    SetCellNoteState(state.Row, state.Column, state.PreviousNotes);

                    // Handle hint revealed cells
                    if (_hintRevealedCells[state.Row, state.Column] && state.NewInput == Cell[state.Row, state.Column].Value)
                    {
                        _hintRevealedCells[state.Row, state.Column] = false;
                        Cell[state.Row, state.Column].CellVisible = false;
                        if (HintLimit)
                        {
                            _hintsUsed--;
                        }
                    }

                    // Update mistake highlighting
                    if (MistakeHighlighter && state.PreviousInput != 0 &&
                        state.PreviousInput != Cell[state.Row, state.Column].Value &&
                        !Cell[state.Row, state.Column].CellVisible)
                    {
                        Cell[state.Row, state.Column].IsIncorrect = true;
                    }
                    else
                    {
                        Cell[state.Row, state.Column].IsIncorrect = false;
                    }

                    // Update correct counter
                    if (Cell[state.Row, state.Column].Input == Cell[state.Row, state.Column].Value &&
                        Cell[state.Row, state.Column].Input != 0)
                    {
                        _cellsCorrect++;
                    }
                }
            };

            // ============================================================================
            // REDO BUTTON - Reapplies an undone action
            // ============================================================================
            ButtonRedo = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonRedo"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1039, 534)
            };
            ButtonRedo.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                if (_redoStack.Count > 0)
                {
                    CellState state = _redoStack.Pop();
                    // Create corresponding undo state
                    CellState undoState = new CellState
                    {
                        Row = state.Row,
                        Column = state.Column,
                        PreviousInput = state.NewInput,
                        NewInput = state.PreviousInput,
                        PreviousNotes = state.NewNotes,
                        NewNotes = state.PreviousNotes
                    };
                    _undoStack.Push(undoState);
                    // Update correct cell counter if current input was correct
                    if (Cell[state.Row, state.Column].Input == Cell[state.Row, state.Column].Value &&
                        Cell[state.Row, state.Column].Input != 0)
                    {
                        _cellsCorrect--;
                    }
                    // Apply redone state
                    Cell[state.Row, state.Column].Input = state.PreviousInput;
                    SetCellNoteState(state.Row, state.Column, state.PreviousNotes);
                    // Handle hint tracking - only increment if:
                    // 1. The redone input matches the correct value
                    // 2. The previous state (before redo) was different
                    // 3. There are no notes in the new state (confirming it was a hint, not a correct guess)
                    // 4. The cell was hint-revealed (tracked in _hintRevealedCells)
                    if (state.PreviousInput == Cell[state.Row, state.Column].Value &&
                        state.NewInput != state.PreviousInput &&
                        state.PreviousNotes.Cast<bool>().All(previous => !previous) &&
                        !_hintRevealedCells[state.Row, state.Column]) // Only increment if not already tracked
                    {
                        _hintRevealedCells[state.Row, state.Column] = true;
                        Cell[state.Row, state.Column].CellVisible = true;
                        if (HintLimit)
                        {
                            _hintsUsed++;
                        }
                    }
                    else if (_hintRevealedCells[state.Row, state.Column] &&
                             state.PreviousInput != Cell[state.Row, state.Column].Value)
                    {
                        _hintRevealedCells[state.Row, state.Column] = false;
                        Cell[state.Row, state.Column].CellVisible = false;
                        if (HintLimit && _hintsUsed > 0)
                        {
                        }
                    }
                    // Update mistake highlighting
                    if (MistakeHighlighter && state.PreviousInput != 0 &&
                        state.PreviousInput != Cell[state.Row, state.Column].Value &&
                        !Cell[state.Row, state.Column].CellVisible)
                    {
                        Cell[state.Row, state.Column].IsIncorrect = true;
                    }
                    else
                    {
                        Cell[state.Row, state.Column].IsIncorrect = false;
                    }
                    // Update correct cell counter with new input
                    if (Cell[state.Row, state.Column].Input == Cell[state.Row, state.Column].Value &&
                        Cell[state.Row, state.Column].Input != 0)
                    {
                        _cellsCorrect++;
                    }
                }
            };

            // ============================================================================
            // HINT BUTTON - Reveals the correct value for selected cell
            // ============================================================================
            ButtonHint = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonHint"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1039, 176)
            };
            ButtonHint.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                if (_selectedCell != null)
                {
                    // GAME SCREEN HINT LOGIC
                    if (_currentScene == Scenes.GameScreen)
                    {
                        // Check hint limit
                        if (HintLimit && _hintsUsed >= 3)
                        {
                            return;
                        }

                        // Don't hint on pre-filled cells
                        if (_selectedCell.CellVisible && !_hintRevealedCells[_selectedCell._row, _selectedCell._column])
                        {
                            return;
                        }

                        // Don't hint on already correct cells
                        if (_selectedCell.Input == _selectedCell.Value && _selectedCell.Input != 0)
                        {
                            return;
                        }

                        // Save state for undo
                        CellState state = new CellState
                        {
                            Row = _selectedCell._row,
                            Column = _selectedCell._column,
                            PreviousInput = _selectedCell.Input,
                            NewInput = _selectedCell.Value,
                            PreviousNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column),
                            NewNotes = new bool[3, 3]
                        };
                        _undoStack.Push(state);
                        _redoStack.Clear();

                        // Clear all notes in the cell
                        for (int value = 1; value <= 9; value++)
                        {
                            int row = (value - 1) / 3;
                            int col = (value - 1) % 3;
                            Notes[_selectedCell._row, _selectedCell._column, row, col].NoteVisible = false;
                        }

                        // Update correct counter
                        if (_selectedCell.Input == _selectedCell.Value && _selectedCell.Input != 0)
                        {
                            _cellsCorrect--;
                        }

                        // Fill in correct value
                        _selectedCell.Input = _selectedCell.Value;
                        _selectedCell.CellVisible = true;
                        _hintRevealedCells[_selectedCell._row, _selectedCell._column] = true;
                        _selectedCell.IsIncorrect = false;
                        _cellsCorrect++;

                        if (HintLimit)
                        {
                            _hintsUsed++;
                        }
                    }
                    // SOLVER SCREEN HINT LOGIC
                    else if (_currentScene == Scenes.SolverScreen)
                    {
                        _selectedCell.CellVisible = false;

                        // Copy current inputs to values for solving
                        for (int row = 0; row < 9; row++)
                        {
                            for (int column = 0; column < 9; column++)
                            {
                                bool wasVisible = Cell[row, column].CellVisible;
                                Cell[row, column].CellVisible = false;
                                Cell[row, column].Value = Cell[row, column].Input;
                                Cell[row, column].CellVisible = wasVisible;
                            }
                        }

                        _selectedCell.Value = 0;

                        // Solve and fill in the selected cell if valid
                        if (IsSudokuValid() && SolveSudoku())
                        {
                            _selectedCell.Input = _selectedCell.Value;
                            _selectedCell.CellVisible = true;
                            if (HintLimit)
                            {
                                _hintsUsed++;
                            }
                            IsCorrect();
                        }
                    }
                }
            };

            // ============================================================================
            // SIDEBAR COLLAPSE/EXPAND BUTTONS
            // ============================================================================
            ButtonSideBarArrowLeft = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonSideBarArrowLeft"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1183, 496)
            };
            ButtonSideBarArrowLeft.Click += (sender, e) =>
            {
                _SideBarOn = false; // Collapse sidebar
            };

            ButtonSideBarArrowRight = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonSideBarArrowRight"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1060, 496)
            };
            ButtonSideBarArrowRight.Click += (sender, e) =>
            {
                _SideBarOn = true; // Expand sidebar
            };

            // ============================================================================
            // NOTES MODE TOGGLE BUTTON
            // ============================================================================
            ButtonNote = new Button(Content.Load<Texture2D>("Controls/GameScreen/ButtonNotes"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1263, 88)
            };
            ButtonNote.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                // Toggle notes mode
                if (_notesMode == true)
                {
                    _notesMode = false;
                }
                else
                {
                    _notesMode = true;
                }
            };
            _gameComponents.Add(ButtonNote);

            // ============================================================================
            // CELL GRID CREATION - Creates all 81 cells and their notes
            // ============================================================================
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    // Create cell with appropriate spacing (98 pixels between cells)
                    Cell[row, col] = new Cell(Content.Load<Texture2D>("Controls/Cell"), Content.Load<SpriteFont>("Fonts/BubbleFont"))
                    {
                        Position = new Vector2(105 + row * 98, 99 + col * 98)
                    };
                    _gameComponents.Add(Cell[row, col]);
                    _solverComponents.Add(Cell[row, col]);
                    Cell[row, col].Click += Cell_Click;
                    Cell[row, col]._row = row;
                    Cell[row, col]._column = col;
                    // Calculate which 3x3 square this cell belongs to (1-9)
                    Cell[row, col]._square = (row / 3) * 3 + (col / 3) + 1;
                }
            }

            // ============================================================================
            // MAIN MENU SCREEN BUTTONS
            // ============================================================================

            // Exit game button
            var ExitButton = new Button(Content.Load<Texture2D>("Controls/MainScreen/ExitButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1772, 26)
            };
            ExitButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                this.Exit(); // Close the game
            };
            _mainMenuScreenComponents.Add(ExitButton);

            // ============================================================================
            // BACK BUTTON - Used on multiple screens to return to previous screen
            // ============================================================================
            var BackButton = new Button(Content.Load<Texture2D>("Controls/BackButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(16, 16)
            };
            BackButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                // Special handling for certain screens
                if (_currentScene == Scenes.CreateAccountScreen || _currentScene == Scenes.SignInScreen || _currentScene == Scenes.DifficultyScreen || _currentScene == Scenes.LeaderBoardScreen)
                {
                    _currentScene = Scenes.MainMenuScreen;
                }
                // Save settings when leaving settings screen while logged in
                else if (_currentScene == Scenes.SettingsScreen && UserID != "")
                {
                    SaveUserSettings(UserID);
                    _currentScene = _previousScene;
                }
                else
                {
                    _currentScene = _previousScene;
                }
            };
            _createaccountComponents.Add(BackButton);
            _signinaccountComponents.Add(BackButton);
            _leaderboardComponents.Add(BackButton);
            _informationComponents.Add(BackButton);
            _difficultyComponents.Add(BackButton);
            _settingsComponents.Add(BackButton);

            // ============================================================================
            // GRID LINES - Overlay for 3x3 box borders
            // ============================================================================
            var GridLine = new NonMoving(Content.Load<Texture2D>("Aestetics/GridLines"), Content.Load<SpriteFont>("Fonts/Font"), Color.White)
            {
                Position = new Vector2(105, 100)
            };
            _gameComponents.Add(GridLine);
            _solverComponents.Add(GridLine);

            // ============================================================================
            // SETTINGS BUTTON - Main menu
            // ============================================================================
            var ButtonSettings = new Button(Content.Load<Texture2D>("Controls/MainScreen/ButtonSettings"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(610, 803)
            };
            ButtonSettings.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.SettingsScreen;
            };
            _mainMenuScreenComponents.Add(ButtonSettings);

            // ============================================================================
            // LEADERBOARD BUTTON - Main menu
            // ============================================================================
            var LeaderBoardButton = new Button(Content.Load<Texture2D>("Controls/MainScreen/LeaderBoardButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(0, 200)
            };
            LeaderBoardButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.LeaderBoardScreen;
            };
            _mainMenuScreenComponents.Add(LeaderBoardButton);

            // ============================================================================
            // SOLVER BUTTON - Main menu
            // ============================================================================
            var ButtonSolver = new Button(Content.Load<Texture2D>("Controls/MainScreen/ButtonSolver"), Content.Load<SpriteFont>("Fonts/Font"))
            {

                Position = new Vector2(610, 610)
            };
            ButtonSolver.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.SolverScreen;
            };
            _mainMenuScreenComponents.Add(ButtonSolver);

            // ============================================================================
            // INFORMATION BUTTON - Main menu (how to play, tips, controls)
            // ============================================================================
            var ButtonInformation = new Button(Content.Load<Texture2D>("Controls/MainScreen/ButtonInformation"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(970, 803)
            };
            ButtonInformation.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.InformationScreen;
            };
            _mainMenuScreenComponents.Add(ButtonInformation);

            // ============================================================================
            // PLAY GAME BUTTON - Main menu (goes to difficulty selection)
            // ============================================================================
            var ButtonMainGame = new Button(Content.Load<Texture2D>("Controls/MainScreen/ButtonMainGame"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(610, 418)
            };
            ButtonMainGame.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.DifficultyScreen;
            };
            _mainMenuScreenComponents.Add(ButtonMainGame);

            // ============================================================================
            // SOLVER COMPLETE BUTTON - Solves the entered puzzle
            // ============================================================================
            var ButtonComplete = new Button(Content.Load<Texture2D>("Controls/SolverScreen/ButtonComplete"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1274, 910)
            };
            ButtonComplete.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }

                // Copy current grid to array for solution counting
                int[,] inputtedGrid = new int[9, 9];
                for (int row = 0; row < 9; row++)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        inputtedGrid[row, column] = Cell[row, column].Input;
                    }
                }

                // Check for duplicate numbers
                if (!IsSudokuValid())
                {
                    ErrorMessage = "This puzzle has duplicate numbers";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                    return;
                }

                // Count number of solutions
                int solutionCount = CountAllSolutions(inputtedGrid, 0, 0);

                if (solutionCount == -1)
                {
                    ErrorMessage = "This sudoku has 1000+ solutions";
                }
                else if (solutionCount == 0)
                {
                    ErrorMessage = "This puzzle has no solutions";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                    return;
                }
                else if (solutionCount > 1)
                {
                    ErrorMessage = "This sudoku has " + solutionCount + " solutions";
                }

                // Copy inputs to values for solving
                for (int row = 0; row < 9; row++)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        Cell[row, column].Value = Cell[row, column].Input;
                    }
                }

                // Solve and display solution
                if (SolveSudoku())
                {
                    foreach (var cell in _solverComponents.OfType<Cell>())
                    {
                        cell.Input = cell.Value;
                        cell.CellVisible = true;
                    }
                }
            };
            _solverComponents.Add(ButtonComplete);

            // ============================================================================
            // SOLVER CLEAR BUTTON - Clears all cells in solver
            // ============================================================================
            var ButtonClear = new Button(Content.Load<Texture2D>("Controls/SolverScreen/ButtonClear"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1429, 48)
            };
            ButtonClear.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                foreach (var cell in _solverComponents.OfType<Cell>())
                {
                    cell.Value = 0;
                    cell.Input = 0;
                    cell.CellVisible = false;
                }
            };
            _solverComponents.Add(ButtonClear);

            // ============================================================================
            // WINNER SCREEN BUTTONS
            // ============================================================================

            // Main menu button on winner/loser screen
            var ButtonMainMenuOnWinner = new Button(Content.Load<Texture2D>("Controls/WinnerScreen/ButtonMainMenuOnWinner"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(353, 745)
            };
            ButtonMainMenuOnWinner.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.MainMenuScreen;
            };
            _winnerComponents.Add(ButtonMainMenuOnWinner);
            _youLostComponents.Add(ButtonMainMenuOnWinner);

            // New game button on winner/loser screen
            var ButtonNewGameOnWinner = new Button(Content.Load<Texture2D>("Controls/WinnerScreen/ButtonNewGameOnWinner"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(971, 745)
            };
            ButtonNewGameOnWinner.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                // Reset game state
                _cellsCorrect = 0;
                FinalTime = 0;
                elapsedTime = 0;
                _currentScene = Scenes.DifficultyScreen;
            };
            _winnerComponents.Add(ButtonNewGameOnWinner);
            _youLostComponents.Add(ButtonNewGameOnWinner);

            // ============================================================================
            // GENERATE NOTES FOR ALL CELLS - Creates 3x3 note grids for each cell
            // ============================================================================
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    GenerateNotes(Cell[row, col], row, col, ref _gameComponents);
                }
            }

            // ============================================================================
            // DIFFICULTY SELECTION BUBBLES
            // Creates visual bubble indicators showing adjacent difficulty levels
            // ============================================================================

            // Small bubbles (far left and right)
            var LeftSmallBubble = new NonMoving(Content.Load<Texture2D>("Controls/DifficultyScreen/SmallBubble"), Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), Color.White)
            {
                Position = new Vector2(197, 382),
                _textcolour = new Color(0x73, 0x99, 0x92)
            };
            _difficultyComponents.Add(LeftSmallBubble);

            var RightSmallBubble = new NonMoving(Content.Load<Texture2D>("Controls/DifficultyScreen/SmallBubble"), Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), Color.White)
            {
                Position = new Vector2(1368, 382),
                _textcolour = new Color(0x73, 0x99, 0x92),
            };
            _difficultyComponents.Add(RightSmallBubble);

            // Medium bubbles (adjacent to center)
            var LeftMediumBubble = new NonMoving(Content.Load<Texture2D>("Controls/DifficultyScreen/MediumBubble"), Content.Load<SpriteFont>("Fonts/BubbleFont"), Color.White)
            {
                Position = new Vector2(375, 343),
                _textcolour = new Color(0x73, 0x99, 0x92)
            };
            _difficultyComponents.Add(LeftMediumBubble);

            var RightMediumBubble = new NonMoving(Content.Load<Texture2D>("Controls/DifficultyScreen/MediumBubble"), Content.Load<SpriteFont>("Fonts/BubbleFont"), Color.White)
            {
                Position = new Vector2(1053, 343),
                _textcolour = new Color(0x73, 0x99, 0x92)
            };
            _difficultyComponents.Add(RightMediumBubble);

            // Big bubble (center - currently selected difficulty)
            var BigBubble = new Button(Content.Load<Texture2D>("Controls/DifficultyScreen/BigBubble"), Content.Load<SpriteFont>("Fonts/BigBubbleFont"))
            {
                Position = new Vector2(593, 223),
                PenColour = new Color(0x73, 0x99, 0x92),
            };
            BigBubble.Click += (sender, e) =>
            {
                if (SoundEffectsOn)
                {
                    bubbleSound.Play();
                }
                // Set difficulty and start game
                _difficulty = (DifficultyIndex % 5) + 1;
                _currentScene = Scenes.GameScreen;
            };
            _difficultyComponents.Add(BigBubble);

            // ============================================================================
            // DIFFICULTY SELECTION ARROWS
            // ============================================================================
            var LeftTriangle = new Button(Content.Load<Texture2D>("Controls/DifficultyScreen/LeftTriangleArrow"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(460, 543)
            };
            LeftTriangle.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                DifficultyIndex--;
                // Wrap around to Master if going below Easy
                if (DifficultyIndex == -1)
                {
                    DifficultyIndex = 4;
                }
            };
            _difficultyComponents.Add(LeftTriangle);

            var RightTriangle = new Button(Content.Load<Texture2D>("Controls/DifficultyScreen/RightTriangleArrow"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1346, 543)
            };
            RightTriangle.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                DifficultyIndex++;
            };
            _difficultyComponents.Add(RightTriangle);

            // ============================================================================
            // TEXT INPUT BOXES - Account creation and sign-in
            // ============================================================================

            // Sign-in screen username
            var UserNameSignInText = new TextInput(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"))
            {
                Position = new Vector2(600, 324)
            };
            UserNameSignInText.Click += (sender, e) =>
            {
                currentTextBox = UserNameSignInText;
            };
            _signinaccountComponents.Add(UserNameSignInText);

            // Create account screen username
            var UserNameCreateAccountText = new TextInput(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"))
            {
                Position = new Vector2(600, 257)
            };
            UserNameCreateAccountText.Click += (sender, e) =>
            {
                currentTextBox = UserNameCreateAccountText;
            };
            _createaccountComponents.Add(UserNameCreateAccountText);

            // Sign-in screen password
            var PasswordSignInText = new TextInput(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"))
            {
                Position = new Vector2(600, 528)
            };
            PasswordSignInText.Click += (sender, e) =>
            {
                currentTextBox = PasswordSignInText;
            };
            _signinaccountComponents.Add(PasswordSignInText);

            // Create account screen password
            var PasswordCreateAccountText = new TextInput(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"))
            {
                Position = new Vector2(600, 461)
            };
            PasswordCreateAccountText.Click += (sender, e) =>
            {
                currentTextBox = PasswordCreateAccountText;
            };
            _createaccountComponents.Add(PasswordCreateAccountText);

            // Create account screen password confirmation
            var PasswordConfirmCreateAccountText = new TextInput(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"))
            {
                Position = new Vector2(600, 662)
            };
            PasswordConfirmCreateAccountText.Click += (sender, e) =>
            {
                currentTextBox = PasswordConfirmCreateAccountText;
            };
            _createaccountComponents.Add(PasswordConfirmCreateAccountText);

            // ============================================================================
            // ACCOUNT BUTTON - Main menu (goes to sign-in screen)
            // ============================================================================
            var AccountButton = new Button(Content.Load<Texture2D>("Controls/MainScreen/AccountButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(0, 50)
            };
            AccountButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.SignInScreen;
                // Clear all text fields
                UserNameSignInText.Text = "";
                PasswordSignInText.Text = "";
                UserNameCreateAccountText.Text = "";
                PasswordCreateAccountText.Text = "";
                PasswordConfirmCreateAccountText.Text = "";
            };
            _mainMenuScreenComponents.Add(AccountButton);

            // ============================================================================
            // CREATE ACCOUNT BUTTON - Validates and creates new account
            // ============================================================================
            var CreateAccountButton = new Button(Content.Load<Texture2D>("Controls/AccountScreen/CreateAccountButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(600, 767)
            };
            CreateAccountButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }

                // Validation: Username cannot be empty
                if (string.IsNullOrWhiteSpace(UserNameCreateAccountText.Text))
                {
                    ErrorMessage = "Username cannot be empty";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                    return;
                }

                // Validation: Password cannot be empty
                if (string.IsNullOrWhiteSpace(PasswordCreateAccountText.Text))
                {
                    ErrorMessage = "Password cannot be empty";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                    return;
                }

                // Validation: Passwords must match
                if (PasswordCreateAccountText.Text != PasswordConfirmCreateAccountText.Text)
                {
                    ErrorMessage = "Passwords do not match";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                    return;
                }

                // Attempt to create account
                if (CreateAccount(UserNameCreateAccountText.Text, PasswordCreateAccountText.Text))
                {
                    UserID = UserNameCreateAccountText.Text;
                    Password = PasswordCreateAccountText.Text;
                    _currentScene = Scenes.MainMenuScreen;
                }
                else
                {
                    ErrorMessage = "Username already exists";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                }
            };
            _createaccountComponents.Add(CreateAccountButton);

            // ============================================================================
            // SIGN IN BUTTON - Validates credentials and logs in user
            // ============================================================================
            var SignInButton = new Button(Content.Load<Texture2D>("Controls/AccountScreen/SignInButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(600, 767)
            };
            SignInButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }

                // Check if account exists with given credentials
                if (AccountExists(UserNameSignInText.Text, PasswordSignInText.Text))
                {
                    UserID = UserNameSignInText.Text;
                    Password = PasswordSignInText.Text;
                    LoadUserSettings(UserID);

                    // Resume music if it should be playing
                    if (MusicOn == true)
                    {
                        Song song = Content.Load<Song>("Aestetics/LofiGirlMusic");
                        MediaPlayer.Play(song);
                        MediaPlayer.IsRepeating = true;
                    }
                    else
                    {
                        MediaPlayer.Stop();
                    }

                    // Clear text fields
                    UserNameSignInText.Text = "";
                    PasswordSignInText.Text = "";
                    _currentScene = Scenes.MainMenuScreen;

                }
                else
                {
                    ErrorMessage = "Incorrect username or password";
                    if (SoundEffectsOn)
                    {
                        errorSound.Play();
                    }
                }
            };

            _signinaccountComponents.Add(SignInButton);

            // ============================================================================
            // ACCOUNT SCREEN NAVIGATION BUTTONS
            // ============================================================================

            // Switch from create account to sign in
            var SignInInsteadButton = new Button(Content.Load<Texture2D>("Controls/AccountScreen/SignInInsteadButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(600, 925)
            };
            SignInInsteadButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.SignInScreen;
            };
            _createaccountComponents.Add(SignInInsteadButton);

            // Switch from sign in to create account
            var CreateAccountInsteadButton = new Button(Content.Load<Texture2D>("Controls/AccountScreen/CreateAccountInsteadButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(600, 925)
            };
            CreateAccountInsteadButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                _currentScene = Scenes.CreateAccountScreen;
            };
            _signinaccountComponents.Add(CreateAccountInsteadButton);

            // ============================================================================
            // PAUSE MENU RESTART BUTTON - Restarts current puzzle or clears solver
            // ============================================================================
            var ClearRestart = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 59)
            };

            ClearRestart.Click += (sender, e) =>
            {
                // SOLVER SCREEN RESTART - Clear entire grid
                if (_previousScene == Scenes.SolverScreen)
                {
                    for (int row = 0; row < 9; row++)
                    {
                        for (int column = 0; column < 9; column++)
                        {
                            Cell[row, column].Value = 0;
                            Cell[row, column].Input = 0;
                            Cell[row, column].CellVisible = false;
                            Cell[row, column].IsIncorrect = false;
                            _hintRevealedCells[row, column] = false;

                            // Clear all notes
                            for (int nrow = 0; nrow < 3; nrow++)
                            {
                                for (int ncolumn = 0; ncolumn < 3; ncolumn++)
                                {
                                    Notes[row, column, nrow, ncolumn].NoteVisible = false;
                                }
                            }
                        }
                    }
                    _cellsCorrect = 0;
                    _hintsUsed = 0;
                    _mistakesMade = 0;
                    _currentScene = Scenes.SolverScreen;
                }
                // GAME SCREEN RESTART - Keep initial clues, reset player inputs
                else if (_previousScene == Scenes.GameScreen)
                {
                    _cellsCorrect = 0;
                    _hintsUsed = 0;
                    _mistakesMade = 0;

                    for (int row = 0; row < 9; row++)
                    {
                        for (int column = 0; column < 9; column++)
                        {
                            // Remove hint-revealed cells
                            if (_hintRevealedCells[row, column])
                            {
                                Cell[row, column].CellVisible = false;
                                Cell[row, column].Input = 0;
                                _hintRevealedCells[row, column] = false;
                            }
                            // Count pre-filled cells
                            else if (Cell[row, column].CellVisible)
                            {
                                _cellsCorrect++;
                            }
                            // Clear player inputs
                            else
                            {
                                Cell[row, column].Input = 0;
                            }
                            Cell[row, column].IsIncorrect = false;

                            // Clear all notes
                            for (int nrow = 0; nrow < 3; nrow++)
                            {
                                for (int ncolumn = 0; ncolumn < 3; ncolumn++)
                                {
                                    Notes[row, column, nrow, ncolumn].NoteVisible = false;
                                }
                            }
                        }
                    }
                    elapsedTime = 0.001; // Reset timer (small value to prevent regeneration)
                    _undoStack.Clear();
                    _redoStack.Clear();
                    _currentScene = Scenes.GameScreen;
                }
            };
            _pauseGameComponents.Add(ClearRestart);
            _pauseSolverComponents.Add(ClearRestart);

            // ============================================================================
            // PAUSE MENU CHANGE DIFFICULTY BUTTON
            // ============================================================================
            var ClearChangeDifficulty = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 194)
            };
            ClearChangeDifficulty.Click += (sender, e) =>
            {
                ClearCells();
                elapsedTime = 0;
                _cellsCorrect = 0;
                _undoStack.Clear();
                _redoStack.Clear();
                _currentScene = Scenes.DifficultyScreen;
            };
            _pauseGameComponents.Add(ClearChangeDifficulty);
            _pauseSolverComponents.Add(ClearChangeDifficulty);

            // ============================================================================
            // PAUSE MENU SETTINGS BUTTON
            // ============================================================================
            var ClearSettingsSideBar = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 329)
            };
            ClearSettingsSideBar.Click += (sender, e) =>
            {
                _currentScene = Scenes.SettingsScreen;
            };
            _pauseGameComponents.Add(ClearSettingsSideBar);
            _pauseSolverComponents.Add(ClearSettingsSideBar);

            // ============================================================================
            // PAUSE MENU HOW TO PLAY BUTTON
            // ============================================================================
            var ClearHowToPlaySideBar = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 464)
            };
            ClearHowToPlaySideBar.Click += (sender, e) =>
            {
                _currentScene = Scenes.InformationScreen;
            };
            _pauseGameComponents.Add(ClearHowToPlaySideBar);
            _pauseSolverComponents.Add(ClearHowToPlaySideBar);

            // ============================================================================
            // PAUSE GAME MENU TO SOLVER BUTTON - Converts current game to solver
            // ============================================================================
            var ClearToSolver = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 599)
            };
            ClearToSolver.Click += (sender, e) =>
            {
                // Copy visible cells to inputs, hide all cells
                for (int row = 0; row < 9; row++)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        if (Cell[row, column].CellVisible)
                        {
                            Cell[row, column].Input = Cell[row, column].Value;
                        }
                        Cell[row, column].CellVisible = false;
                    }
                }

                _currentScene = Scenes.SolverScreen;
            };
            _pauseGameComponents.Add(ClearToSolver);

            // ============================================================================
            // PAUSE MENU EXIT TO MAIN MENU BUTTON
            // ============================================================================
            var ClearExit = new Button(SideMenuClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1305, 937)
            };
            ClearExit.Click += (sender, e) =>
            {
                _currentScene = Scenes.MainMenuScreen;
            };
            _pauseGameComponents.Add(ClearExit);
            _pauseSolverComponents.Add(ClearExit);

            // ============================================================================
            // INFORMATION SCREEN TAB BUTTONS
            // ============================================================================
            var ClearButtonHowToPlay = new Button(InformationClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(157, 15)
            };
            ClearButtonHowToPlay.Click += (sender, e) =>
            {
                InformationIndex = 1;
            };
            _informationComponents.Add(ClearButtonHowToPlay);

            var ClearButtonTipsTricks = new Button(InformationClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(481, 15)
            };
            ClearButtonTipsTricks.Click += (sender, e) =>
            {
                InformationIndex = 2;
            };
            _informationComponents.Add(ClearButtonTipsTricks);

            var ClearButtonControls = new Button(InformationClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(805, 15)
            };
            ClearButtonControls.Click += (sender, e) =>
            {
                InformationIndex = 3;
            };
            _informationComponents.Add(ClearButtonControls);

            // ============================================================================
            // SETTINGS SCREEN TAB BUTTONS
            // ============================================================================
            var ClearButtonSettings = new Button(InformationClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(157, 15)
            };
            ClearButtonSettings.Click += (sender, e) =>
            {
                SettingsIndex = 1;
            };
            _settingsComponents.Add(ClearButtonSettings);

            var ClearButtonAccount = new Button(InformationClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(481, 15)
            };
            ClearButtonAccount.Click += (sender, e) =>
            {
                SettingsIndex = 2;
            };
            _settingsComponents.Add(ClearButtonAccount);

            // ============================================================================
            // LEADERBOARD DIFFICULTY TAB BUTTONS
            // ============================================================================
            var ClearButtonEasy = new Button(LeaderboardClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(150, 173)
            };
            ClearButtonEasy.Click += (sender, e) =>
            {
                LeaderboardDifficulty = 1;
            };
            _leaderboardComponents.Add(ClearButtonEasy);

            var ClearButtonMedium = new Button(LeaderboardClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(474, 173)
            };
            ClearButtonMedium.Click += (sender, e) =>
            {
                LeaderboardDifficulty = 2;
            };
            _leaderboardComponents.Add(ClearButtonMedium);

            var ClearButtonHard = new Button(LeaderboardClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(798, 173)
            };
            ClearButtonHard.Click += (sender, e) =>
            {
                LeaderboardDifficulty = 3;
            };
            _leaderboardComponents.Add(ClearButtonHard);

            var ClearButtonExpert = new Button(LeaderboardClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1122, 173)
            };
            ClearButtonExpert.Click += (sender, e) =>
            {
                LeaderboardDifficulty = 4;
            };
            _leaderboardComponents.Add(ClearButtonExpert);

            var ClearButtonMaster = new Button(LeaderboardClearButton, Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1446, 173)
            };
            ClearButtonMaster.Click += (sender, e) =>
            {
                LeaderboardDifficulty = 5;
            };
            _leaderboardComponents.Add(ClearButtonMaster);

            // ============================================================================
            // SETTINGS TOGGLE SWITCHES - Game settings
            // ============================================================================

            // Hint limit switch (limit hints to 3 per game)
            hintLimitSwitch = new Button(Content.Load<Texture2D>(HintLimit ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 142)
            };
            hintLimitSwitch.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                HintLimit = !HintLimit;
            };
            _settingsComponents.Add(hintLimitSwitch);

            // Lives limit switch (3 strikes and you lose)
            livesLimitSwitch = new Button(Content.Load<Texture2D>(LivesLimit ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 293)
            };
            livesLimitSwitch.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                LivesLimit = !LivesLimit;
            };
            _settingsComponents.Add(livesLimitSwitch);

            // Mistake highlighter switch (highlight incorrect entries in red)
            mistakeHighlighterSwitch = new Button(Content.Load<Texture2D>(MistakeHighlighter ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 448)
            };
            mistakeHighlighterSwitch.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                MistakeHighlighter = !MistakeHighlighter;

                // When turning on, highlight existing mistakes or change IsIncorrect Value
                for (int row = 0; row < 9; row++)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        if (MistakeHighlighter && Cell[row, column].Value != Cell[row, column].Input && Cell[row, column].CellVisible == false && Cell[row, column].Input != 0)
                        {
                            Cell[row, column].IsIncorrect = true;
                        }
                        else
                        {
                            Cell[row, column].IsIncorrect = false;
                        }
                    }
                }
            };
            _settingsComponents.Add(mistakeHighlighterSwitch);

            // Show timer switch
            showTimerSwitch = new Button(Content.Load<Texture2D>(ShowTimer ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 609)
            };
            showTimerSwitch.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                ShowTimer = !ShowTimer;
            };
            _settingsComponents.Add(showTimerSwitch);

            // Music switch
            musicSwitch = new Button(Content.Load<Texture2D>(MusicOn ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 767)
            };
            musicSwitch.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                MusicOn = !MusicOn;
                if (MusicOn)
                {
                    MediaPlayer.Play(song);
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Resume();
                }
                else
                {
                    MediaPlayer.Pause();
                }
            };
            _settingsComponents.Add(musicSwitch);

            // Sound effects switch
            soundEffectsSwitch = new Button(Content.Load<Texture2D>(SoundEffectsOn ? "Controls/SettingsScreen/OnSwitch" : "Controls/SettingsScreen/OffSwitch"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1020, 919)
            };
            soundEffectsSwitch.Click += (sender, e) =>
            {

                SoundEffectsOn = !SoundEffectsOn;
                // Play click sound if just turned on
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
            };
            _settingsComponents.Add(soundEffectsSwitch);

            // ============================================================================
            // ERROR DIALOG BOX COMPONENTS
            // ============================================================================
            ErrorBox = new NonMoving(Content.Load<Texture2D>("Controls/ErrorBox"), Content.Load<SpriteFont>("Fonts/BubbleFont"), Color.White)
            {
                _textcolour = _textColour,
                Position = new Vector2(553, 301),

            };

            OkButton = new Button(Content.Load<Texture2D>("Controls/OkButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(837, 598)
            };
            OkButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                ErrorMessage = ""; // Clear error message
            };

            // ============================================================================
            // LOG OUT BUTTON - Settings screen
            // ============================================================================
            var LogOutButton = new Button(Content.Load<Texture2D>("Controls/SettingsScreen/LogOutButton"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = new Vector2(1034, 740)
            };
            LogOutButton.Click += (sender, e) =>
            {
                if (SoundEffectsOn == true)
                {
                    clickSound.Play();
                }
                // Save settings before logging out
                if (UserID != "")
                {
                    SaveUserSettings(UserID);

                    // Reset all settings to defaults
                    MistakeHighlighter = false;
                    LivesLimit = false;
                    HintLimit = false;
                    ShowTimer = true;
                    MusicOn = true;

                    //starts the music playing again
                    Song song = Content.Load<Song>("Aestetics/LofiGirlMusic");
                    MediaPlayer.Play(song);
                    MediaPlayer.IsRepeating = true;

                    // Clear personal best times
                    UserBestTimeEasy = "--:--:--";
                    UserBestTimeMedium = "--:--:--";
                    UserBestTimeHard = "--:--:--";
                    UserBestTimeExpert = "--:--:--";
                    UserBestTimeMaster = "--:--:--";

                    // Clear credentials
                    UserID = "";
                    Password = "";
                }
            };
            _settingsComponents.Add(LogOutButton);
        }

        // ============================================================================
        // NUMBER BUTTON CLICK HANDLER
        // Handles clicks on number buttons (1-9) for both normal input and notes mode
        // ============================================================================
        private void NumberButton_Click(object sender, System.EventArgs e)
        {
            if (SoundEffectsOn == true)
            {
                clickSound.Play();
            }

            if (_selectedCell != null)
            {
                Button clickedButton = sender as Button;

                // ============================================================================
                // NORMAL INPUT MODE - Enter number in cell
                // ============================================================================
                if (clickedButton != null && _notesMode == false)
                {

                    if (_currentScene == Scenes.GameScreen)
                    {
                        // Save state for undo
                        CellState state = new CellState
                        {
                            Row = _selectedCell._row,
                            Column = _selectedCell._column,
                            PreviousInput = _selectedCell.Input,
                            NewInput = clickedButton.Value,
                            PreviousNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column),
                            NewNotes = new bool[3, 3]
                        };
                        _undoStack.Push(state);
                        _redoStack.Clear();

                        // Clear all notes when entering a number
                        for (int value = 1; value <= 9; value++)
                        {
                            int row = (value - 1) / 3;
                            int col = (value - 1) % 3;
                            if (Notes[_selectedCell._row, _selectedCell._column, row, col].NoteVisible == true)
                            {
                                Notes[_selectedCell._row, _selectedCell._column, row, col].NoteVisible = false;
                            }
                        }

                        // Update correct cell counter
                        if (_selectedCell.Input == _selectedCell.Value && _selectedCell.Value != clickedButton.Value)
                        {
                            _cellsCorrect--;
                        }

                        // Lives limit check - wrong answer costs a life
                        if (LivesLimit && clickedButton.Value != _selectedCell.Value && !_selectedCell.CellVisible)
                        {
                            _mistakesMade++;
                            if (_mistakesMade >= 3)
                            {
                                // Game over
                                _currentScene = Scenes.YouLostScreen;
                                ClearCells();
                                _cellsCorrect = 0;
                                elapsedTime = 0;
                                _mistakesMade = 0;
                                _hintsUsed = 0;
                                return;
                            }
                        }

                        // Update correct cell counter if new answer is correct
                        if (clickedButton.Value == _selectedCell.Value && _selectedCell.Input != _selectedCell.Value)
                        {
                            _cellsCorrect++;
                        }

                        // Mistake highlighter - mark incorrect entries in red
                        if (MistakeHighlighter && clickedButton.Value != _selectedCell.Value && !_selectedCell.CellVisible)
                        {
                            _selectedCell.IsIncorrect = true;
                        }
                        else
                        {
                            _selectedCell.IsIncorrect = false;
                        }
                    }
                    if (_currentScene == Scenes.SolverScreen)
                    {
                        // Save state for undo
                        CellState state = new CellState
                        {
                            Row = _selectedCell._row,
                            Column = _selectedCell._column,
                            PreviousInput = _selectedCell.Input,
                            NewInput = clickedButton.Value,
                            PreviousNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column),
                            NewNotes = new bool[3, 3]
                        };
                        _undoStack.Push(state);
                        _redoStack.Clear();
                    }

                    _selectedCell.Input = clickedButton.Value;
                }
                // ============================================================================
                // NOTES MODE - Toggle note in cell
                // ============================================================================
                else if (clickedButton != null && _notesMode == true)
                {
                    if (_currentScene == Scenes.GameScreen)
                    {
                        // Calculate note position in 3x3 grid
                        int noterow = (clickedButton.Value - 1) / 3;
                        int notecol = (clickedButton.Value - 1) % 3;

                        // Save state for undo
                        CellState state = new CellState
                        {
                            Row = _selectedCell._row,
                            Column = _selectedCell._column,
                            PreviousInput = _selectedCell.Input,
                            NewInput = _selectedCell.Input,
                            PreviousNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column)
                        };

                        // Toggle note visibility (only if cell is empty and not pre-filled)
                        if (Notes[_selectedCell._row, _selectedCell._column, noterow, notecol].NoteVisible == false && _selectedCell.Input == 0 && _selectedCell.CellVisible == false)
                        {
                            Notes[_selectedCell._row, _selectedCell._column, noterow, notecol].NoteVisible = true;
                        }
                        else
                        {
                            Notes[_selectedCell._row, _selectedCell._column, noterow, notecol].NoteVisible = false;
                        }

                        state.NewNotes = GetCellNoteState(_selectedCell._row, _selectedCell._column);
                        _undoStack.Push(state);
                        _redoStack.Clear();
                    }
                }
            }
        }

        // ============================================================================
        // CELL CLICK HANDLER
        // Handles selection/deselection of cells and visual highlighting
        // ============================================================================
        private void Cell_Click(object sender, System.EventArgs e)
        {
            // Deselect previous cell if one was selected
            if (_selectedCell != null)
            {
                _selectedCell.IsSelected = false;

                // Clear cross highlighting (row and column)
                for (int i = 0; i < 9; i++)
                {
                    Cell[i, _selectedCell._column].IsCrossSelected = false;
                    Cell[_selectedCell._row, i].IsCrossSelected = false;
                }

                // Clear 3x3 square highlighting
                if (_currentScene == Scenes.SolverScreen)
                {
                    foreach (var cell in _solverComponents.OfType<Cell>())
                    {
                        if (Cell[cell._row, cell._column]._square == Cell[_selectedCell._row, _selectedCell._column]._square)
                        {
                            Cell[cell._row, cell._column].IsSquareSelected = false;
                        }
                    }
                }
                else
                {
                    foreach (var cell in _gameComponents.OfType<Cell>())
                    {
                        if (Cell[cell._row, cell._column]._square == Cell[_selectedCell._row, _selectedCell._column]._square)
                        {
                            Cell[cell._row, cell._column].IsSquareSelected = false;
                        }
                    }
                }
            }

            // Select new cell
            _selectedCell = sender as Cell;

            if (_selectedCell != null)
            {
                _selectedCell.IsSelected = true;

                // Highlight row and column
                for (int i = 0; i < 9; i++)
                {
                    Cell[i, _selectedCell._column].IsCrossSelected = true;
                    Cell[_selectedCell._row, i].IsCrossSelected = true;
                }

                // Highlight 3x3 square
                foreach (var cell in _gameComponents.OfType<Cell>())
                {
                    if (Cell[cell._row, cell._column]._square == Cell[_selectedCell._row, _selectedCell._column]._square)
                    {
                        Cell[cell._row, cell._column].IsSquareSelected = true;
                    }
                }
            }
        }

        // ============================================================================
        // UPDATE METHOD - Called every frame, handles scene-specific update logic
        // ============================================================================
        protected override void Update(GameTime gameTime)
        {

            switch (_currentScene)
            {
                // ============================================================================
                // MAIN MENU SCREEN UPDATE
                // ============================================================================
                case Scenes.MainMenuScreen:
                    elapsedTime = 0; // Reset timer when at main menu
                    foreach (var component in _mainMenuScreenComponents)
                    {
                        component.Update(gameTime);
                    }

                    base.Update(gameTime);

                    break;

                // ============================================================================
                // DIFFICULTY SELECTION SCREEN UPDATE
                // Updates difficulty carousel bubble texts
                // ============================================================================
                case Scenes.DifficultyScreen:

                    var leftSmallBubble = _difficultyComponents[2] as NonMoving;
                    var rightSmallBubble = _difficultyComponents[3] as NonMoving;
                    var leftMediumBubble = _difficultyComponents[4] as NonMoving;
                    var rightMediumBubble = _difficultyComponents[5] as NonMoving;
                    var bigBubble = _difficultyComponents[6] as Button;

                    // Wrap difficulty index to stay in range 0-4
                    DifficultyIndex = ((DifficultyIndex % 5) + 5) % 5;

                    // Update bubble texts to show adjacent difficulties
                    if (leftSmallBubble != null) leftSmallBubble.Text = _difficulties[(DifficultyIndex + 3) % 5];
                    if (rightSmallBubble != null) rightSmallBubble.Text = _difficulties[(DifficultyIndex + 2) % 5];
                    if (leftMediumBubble != null) leftMediumBubble.Text = _difficulties[(DifficultyIndex + 4) % 5];
                    if (rightMediumBubble != null) rightMediumBubble.Text = _difficulties[(DifficultyIndex + 6) % 5];
                    if (bigBubble != null) bigBubble.Text = _difficulties[DifficultyIndex + 5 % 5];

                    foreach (var component in _difficultyComponents)
                    {
                        component.Update(gameTime);
                    }
                    ClearCells(); // Ensure grid is clear

                    base.Update(gameTime);

                    break;

                // ============================================================================
                // GAME SCREEN UPDATE
                // Main gameplay logic - timer, notes mode, sidebar
                // ============================================================================
                case Scenes.GameScreen:

                    foreach (var component in _gameComponents)
                    {
                        component.Update(gameTime);
                    }
                    SideBar.Update(gameTime);

                    // Update appropriate sidebar buttons based on sidebar state
                    if (_SideBarOn)
                    {
                        ButtonSideBarArrowLeft.Update(gameTime);
                        ButtonUndo.Update(gameTime);
                        ButtonRedo.Update(gameTime);
                        ButtonEraser.Update(gameTime);
                        ButtonHint.Update(gameTime);
                    }
                    else
                    {
                        ButtonSideBarArrowRight.Update(gameTime);
                    }

                    // Increment elapsed time
                    elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;

                    // Update notes button texture if notes mode changed
                    if (_previousNotesMode != _notesMode)
                    {
                        _gameComponents.Remove(ButtonNote);

                        ButtonNote = new Button(
                            Content.Load<Texture2D>(_notesMode ? "Controls/GameScreen/ButtonNotesOn" : "Controls/GameScreen/ButtonNotes"),
                            Content.Load<SpriteFont>("Fonts/Font"))
                        {
                            Position = new Vector2(1263, 88)
                        };
                        ButtonNote.Click += (sender, e) =>
                        {
                            _notesMode = !_notesMode;
                        };

                        _gameComponents.Add(ButtonNote);
                        _previousNotesMode = _notesMode;
                    }

                    base.Update(gameTime);
                    IsCorrect(); // Check for win condition
                    break;

                // ============================================================================
                // SETTINGS SCREEN UPDATE
                // Updates toggle switch textures based on current settings
                // ============================================================================
                case Scenes.SettingsScreen:
                    _settingsComponents[SettingsIndex - 1].Update(gameTime);
                    _settingsComponents[2].Update(gameTime);
                    _settingsComponents[3].Update(gameTime);
                    _settingsComponents[4].Update(gameTime);

                    // GAME SETTINGS TAB
                    if (SettingsIndex == 1)
                    {
                        var hintSwitch = _settingsComponents[5] as Button;
                        var livesSwitch = _settingsComponents[6] as Button;
                        var mistakeSwitch = _settingsComponents[7] as Button;
                        var timerSwitch = _settingsComponents[8] as Button;
                        var musicSwitch = _settingsComponents[9] as Button;
                        var soundSwitch = _settingsComponents[10] as Button;

                        // Update all switch textures based on current state
                        if (hintSwitch != null)
                        {
                            if (HintLimit == true)
                            {
                                hintSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                hintSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            hintSwitch.Texture = Content.Load<Texture2D>(hintSwitchTexture);
                        }
                        if (livesSwitch != null)
                        {
                            if (LivesLimit == true)
                            {
                                livesSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                livesSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            livesSwitch.Texture = Content.Load<Texture2D>(livesSwitchTexture);
                        }
                        if (mistakeSwitch != null)
                        {
                            if (MistakeHighlighter == true)
                            {
                                mistakeSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                mistakeSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            mistakeSwitch.Texture = Content.Load<Texture2D>(mistakeSwitchTexture);
                        }
                        if (timerSwitch != null)
                        {
                            if (ShowTimer == true)
                            {
                                timerSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                timerSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            timerSwitch.Texture = Content.Load<Texture2D>(timerSwitchTexture);
                        }
                        if (musicSwitch != null)
                        {
                            if (MusicOn == true)
                            {
                                musicSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                musicSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            musicSwitch.Texture = Content.Load<Texture2D>(musicSwitchTexture);
                        }
                        if (soundSwitch != null)
                        {
                            if (SoundEffectsOn == true)
                            {
                                soundSwitchTexture = "Controls/SettingsScreen/OnSwitch";
                            }
                            else
                            {
                                soundSwitchTexture = "Controls/SettingsScreen/OffSwitch";
                            }
                            soundSwitch.Texture = Content.Load<Texture2D>(soundSwitchTexture);
                        }

                        _settingsComponents[5].Update(gameTime);
                        _settingsComponents[6].Update(gameTime);
                        _settingsComponents[7].Update(gameTime);
                        _settingsComponents[8].Update(gameTime);
                        _settingsComponents[9].Update(gameTime);
                        _settingsComponents[10].Update(gameTime);

                    }
                    // ACCOUNT SETTINGS TAB
                    else
                    {
                        _settingsComponents[11].Update(gameTime);
                    }
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // PAUSE GAME SCREEN UPDATE
                // ============================================================================
                case Scenes.PauseGameScreen:
                    foreach (var component in _pauseGameComponents)
                    {
                        component.Update(gameTime);
                    }
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // PAUSE SOLVER SCREEN UPDATE
                // ============================================================================
                case Scenes.PauseSolverScreen:
                    foreach (var component in _pauseSolverComponents)
                    {
                        component.Update(gameTime);
                    }

                    base.Update(gameTime);
                    break;

                // ============================================================================
                // SOLVER SCREEN UPDATE
                // ============================================================================
                case Scenes.SolverScreen:
                    foreach (var component in _solverComponents)
                    {
                        component.Update(gameTime);
                    }

                    // Update error dialog if present
                    if (ErrorMessage != "")
                    {
                        ErrorBox.Update(gameTime);
                        OkButton.Update(gameTime);
                    }

                    SideBar.Update(gameTime);

                    // Update appropriate sidebar buttons based on sidebar state
                    if (_SideBarOn)
                    {
                        ButtonSideBarArrowLeft.Update(gameTime);
                        ButtonUndo.Update(gameTime);
                        ButtonRedo.Update(gameTime);
                        ButtonEraser.Update(gameTime);
                        ButtonHint.Update(gameTime);
                    }
                    else
                    {
                        ButtonSideBarArrowRight.Update(gameTime);
                    }
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // WINNER SCREEN UPDATE
                // ============================================================================
                case Scenes.WinnerScreen:
                    foreach (var component in _winnerComponents)
                    {
                        component.Update(gameTime);
                    }
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // YOU LOST SCREEN UPDATE
                // ============================================================================
                case Scenes.YouLostScreen:
                    foreach (var component in _youLostComponents)
                    {
                        component.Update(gameTime);
                    }
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // CREATE ACCOUNT SCREEN UPDATE
                // ============================================================================
                case Scenes.CreateAccountScreen:
                    foreach (var component in _createaccountComponents)
                    {
                        component.Update(gameTime);
                    }

                    // Update error dialog if present
                    if (ErrorMessage != "")
                    {
                        ErrorBox.Update(gameTime);
                        OkButton.Update(gameTime);
                    }


                    base.Update(gameTime);
                    break;

                // ============================================================================
                // SIGN IN SCREEN UPDATE
                // ============================================================================
                case Scenes.SignInScreen:
                    foreach (var component in _signinaccountComponents)
                    {
                        component.Update(gameTime);
                    }

                    // Update error dialog if present
                    if (ErrorMessage != "")
                    {
                        ErrorBox.Update(gameTime);
                        OkButton.Update(gameTime);
                    }

                    base.Update(gameTime);
                    break;

                // ============================================================================
                // LEADERBOARD SCREEN UPDATE
                // ============================================================================
                case Scenes.LeaderBoardScreen:

                    _leaderboardComponents[LeaderboardDifficulty - 1].Update(gameTime);
                    _leaderboardComponents[5].Update(gameTime);
                    _leaderboardComponents[6].Update(gameTime);
                    _leaderboardComponents[7].Update(gameTime);
                    _leaderboardComponents[8].Update(gameTime);
                    _leaderboardComponents[9].Update(gameTime);
                    _leaderboardComponents[10].Update(gameTime);
                    base.Update(gameTime);
                    break;

                // ============================================================================
                // INFORMATION SCREEN UPDATE
                // ============================================================================
                case Scenes.InformationScreen:
                    base.Update(gameTime);
                    _informationComponents[InformationIndex - 1].Update(gameTime);
                    _informationComponents[3].Update(gameTime);
                    _informationComponents[4].Update(gameTime);
                    _informationComponents[5].Update(gameTime);
                    _informationComponents[6].Update(gameTime);
                    break;
            }
        }

        // ============================================================================
        // DRAW METHOD - Called every frame, handles scene-specific rendering
        // ============================================================================
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_backgroundColor);

            switch (_currentScene)
            {
                // ============================================================================
                // MAIN MENU SCREEN DRAW
                // ============================================================================
                case Scenes.MainMenuScreen:
                    GraphicsDevice.Clear(_backgroundColor);
                    _previousScene = _currentScene;
                    _currentScene = Scenes.MainMenuScreen;
                    _spriteBatch.Begin();

                    // Draw all main menu components
                    foreach (var component in _mainMenuScreenComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Display logged-in username if user is logged in
                    if (UserID != "")
                    {
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), "Logged in as " + UserID, new Vector2(10, 0), _textColour);
                    }

                    _spriteBatch.End();

                    base.Draw(gameTime);

                    break;

                // ============================================================================
                // DIFFICULTY SELECTION SCREEN DRAW
                // ============================================================================
                case Scenes.DifficultyScreen:
                    GraphicsDevice.Clear(_backgroundColor);

                    _spriteBatch.Begin();
                    foreach (var component in _difficultyComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }
                    _spriteBatch.End();

                    base.Draw(gameTime);

                    break;

                // ============================================================================
                // GAME SCREEN DRAW
                // Draws grid, timer, difficulty, and game stats
                // ============================================================================
                case Scenes.GameScreen:
                    GraphicsDevice.Clear(_backgroundColor);

                    // Generate new puzzle if timer is at 0 (first frame)
                    if (elapsedTime == 0)
                    {
                        FillCells();
                        MakeSudoku();
                    }

                    // Format timer string (HH:MM:SS)
                    var Timer = new string($"{(int)elapsedTime / 3600:D2}:{(int)elapsedTime / 60 % 60:D2}:{(int)elapsedTime % 60:D2}");
                    _spriteBatch.Begin();

                    // Draw all game components
                    foreach (var component in _gameComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Draw sidebar and appropriate buttons based on sidebar state
                    if (_SideBarOn)
                    {
                        ButtonSideBarArrowLeft.Draw(gameTime, _spriteBatch);
                        ButtonHint.Draw(gameTime, _spriteBatch);
                        ButtonUndo.Draw(gameTime, _spriteBatch);
                        ButtonRedo.Draw(gameTime, _spriteBatch);
                        ButtonEraser.Draw(gameTime, _spriteBatch);
                        SideBar.Position = new Vector2(1028, 108); // Expanded position
                    }
                    else
                    {
                        ButtonSideBarArrowRight.Draw(gameTime, _spriteBatch);
                        SideBar.Position = new Vector2(895, 108); // Collapsed position
                    }

                    // Convert difficulty number to name
                    string DifficultyName = "";
                    if (_difficulty == 1)
                    {
                        DifficultyName = "Easy";
                    }
                    else if (_difficulty == 2)
                    {
                        DifficultyName = "Medium";

                    }
                    else if (_difficulty == 3)
                    {
                        DifficultyName = "Hard";
                    }
                    else if (_difficulty == 4)
                    {
                        DifficultyName = "Expert";
                    }
                    else
                    {
                        DifficultyName = "Master";
                    }

                    // Draw timer if enabled
                    if (ShowTimer)
                    {
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/TimerBubbleFont"), Timer, new Vector2(1432, 70), _textColour);
                    }

                    // Draw difficulty name
                    _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/TimerBubbleFont"), DifficultyName, new Vector2(1454, 167), _textColour);

                    // Draw hints counter if limit is enabled
                    if (HintLimit)
                    {
                        string hintsText = "Hints: " + (3 - _hintsUsed) + "/3";
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), hintsText, new Vector2(1350, 950), _textColour);
                    }

                    // Draw lives counter if limit is enabled
                    if (LivesLimit)
                    {
                        string livesText = "Lives: " + (3 - _mistakesMade) + "/3";
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), livesText, new Vector2(1600, 950), _textColour);
                    }
                    _spriteBatch.End();

                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // SETTINGS SCREEN DRAW
                // Draws current settings tab and user information if on account tab
                // ============================================================================
                case Scenes.SettingsScreen:
                    _spriteBatch.Begin();
                    _settingsComponents[SettingsIndex - 1].Draw(gameTime, _spriteBatch);
                    _settingsComponents[2].Draw(gameTime, _spriteBatch);
                    _settingsComponents[3].Draw(gameTime, _spriteBatch);
                    _settingsComponents[4].Draw(gameTime, _spriteBatch);

                    // GAME SETTINGS TAB
                    if (SettingsIndex == 1)
                    {
                        _settingsComponents[5].Draw(gameTime, _spriteBatch);
                        _settingsComponents[6].Draw(gameTime, _spriteBatch);
                        _settingsComponents[7].Draw(gameTime, _spriteBatch);
                        _settingsComponents[8].Draw(gameTime, _spriteBatch);
                        _settingsComponents[9].Draw(gameTime, _spriteBatch);
                        _settingsComponents[10].Draw(gameTime, _spriteBatch);

                    }
                    // ACCOUNT TAB - Display user info and personal bests
                    else
                    {
                        _settingsComponents[11].Draw(gameTime, _spriteBatch);
                        LoadPersonalBestTImes(UserID);
                        Color _darkTextColour = new Color(0x41, 0x4e, 0x4e);

                        // Draw personal best times for each difficulty
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), UserBestTimeEasy, new Vector2(552, 297), _darkTextColour);
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), UserBestTimeMedium, new Vector2(552, 450), _darkTextColour);
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), UserBestTimeHard, new Vector2(552, 603), _darkTextColour);
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), UserBestTimeExpert, new Vector2(552, 756), _darkTextColour);
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/SmallBubbleFont"), UserBestTimeMaster, new Vector2(552, 909), _darkTextColour);

                        // Draw username and password
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/BubbleFont"), UserID, new Vector2(918, 249), _darkTextColour);
                        _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/BubbleFont"), Password, new Vector2(918, 474), _darkTextColour);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // PAUSE GAME SCREEN DRAW
                // ============================================================================
                case Scenes.PauseGameScreen:
                    _spriteBatch.Begin();
                    foreach (var component in _pauseGameComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // PAUSE SOLVER SCREEN DRAW
                // ============================================================================
                case Scenes.PauseSolverScreen:
                    _spriteBatch.Begin();
                    foreach (var component in _pauseSolverComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // SOLVER SCREEN DRAW
                // ============================================================================
                case Scenes.SolverScreen:
                    GraphicsDevice.Clear(_backgroundColor);

                    // Clear grid on first frame
                    if (elapsedTime == 0)
                    {
                        ClearCells();
                        elapsedTime = 1;
                    }
                    _spriteBatch.Begin();

                    // Draw all solver components
                    foreach (var component in _solverComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Draw sidebar and appropriate buttons based on sidebar state
                    if (_SideBarOn)
                    {
                        ButtonSideBarArrowLeft.Draw(gameTime, _spriteBatch);
                        ButtonHint.Draw(gameTime, _spriteBatch);
                        ButtonUndo.Draw(gameTime, _spriteBatch);
                        ButtonRedo.Draw(gameTime, _spriteBatch);
                        ButtonEraser.Draw(gameTime, _spriteBatch);
                        SideBar.Position = new Vector2(1028, 108);
                    }
                    else
                    {
                        ButtonSideBarArrowRight.Draw(gameTime, _spriteBatch);
                        SideBar.Position = new Vector2(895, 108);

                    }

                    // Draw error dialog if present
                    if (ErrorMessage != "")
                    {
                        DrawError(ErrorMessage, _textColour, gameTime);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // WINNER SCREEN DRAW
                // Displays completion time
                // ============================================================================
                case Scenes.WinnerScreen:
                    GraphicsDevice.Clear(_backgroundColor);

                    _spriteBatch.Begin();
                    foreach (var component in _winnerComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Format and display final time
                    var FinishTime = new string($"{(int)FinalTime / 3600:D2}:{(int)FinalTime / 60:D2}:{(int)FinalTime % 60:D2}");
                    _spriteBatch.DrawString(Content.Load<SpriteFont>("Fonts/BubbleFont"), FinishTime, new Vector2(812, 575), _textColour);
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // YOU LOST SCREEN DRAW
                // ============================================================================
                case Scenes.YouLostScreen:
                    GraphicsDevice.Clear(_backgroundColor);
                    _spriteBatch.Begin();
                    foreach (var component in _youLostComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // LEADERBOARD SCREEN DRAW
                // Displays top 10 times for selected difficulty
                // ============================================================================
                case Scenes.LeaderBoardScreen:

                    GraphicsDevice.Clear(_backgroundColor);
                    _spriteBatch.Begin();

                    // Draw background and tab buttons
                    _leaderboardComponents[LeaderboardDifficulty - 1].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[5].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[6].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[7].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[8].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[9].Draw(gameTime, _spriteBatch);
                    _leaderboardComponents[10].Draw(gameTime, _spriteBatch);

                    // Get leaderboard data for current difficulty
                    var leaderboardData = GetLeaderBoardTimes(LeaderboardDifficulty);
                    var font = Content.Load<SpriteFont>("Fonts/SmallBubbleFont");

                    // Draw top 5 on left side
                    for (int rank = 0; rank < leaderboardData.Count && rank < 5; rank++)
                    {
                        string username = leaderboardData[rank].Username;
                        string time = leaderboardData[rank].BestTime.ToString(@"hh\:mm\:ss");
                        float yPosition = 479 + (rank * 110);

                        _spriteBatch.DrawString(font, username, new Vector2(262, yPosition), Color.White);
                        _spriteBatch.DrawString(font, time, new Vector2(716, yPosition), Color.White);
                    }

                    // Draw ranks 6-10 on right side
                    for (int rank = 5; rank < leaderboardData.Count && rank < 10; rank++)
                    {
                        string username = leaderboardData[rank].Username;
                        string time = leaderboardData[rank].BestTime.ToString(@"hh\:mm\:ss");
                        float yPosition = 479 + ((rank - 5) * 110);

                        _spriteBatch.DrawString(font, username, new Vector2(1085, yPosition), Color.White);
                        _spriteBatch.DrawString(font, time, new Vector2(1539, yPosition), Color.White);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // SIGN IN SCREEN DRAW
                // ============================================================================
                case Scenes.SignInScreen:
                    GraphicsDevice.Clear(_backgroundColor);
                    _spriteBatch.Begin();
                    foreach (var component in _signinaccountComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Draw error dialog if present
                    if (ErrorMessage != "")
                    {
                        DrawError(ErrorMessage, _textColour, gameTime);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // CREATE ACCOUNT SCREEN DRAW
                // ============================================================================
                case Scenes.CreateAccountScreen:
                    GraphicsDevice.Clear(_backgroundColor);
                    _spriteBatch.Begin();
                    foreach (var component in _createaccountComponents)
                    {
                        component.Draw(gameTime, _spriteBatch);
                    }

                    // Draw error dialog if present
                    if (ErrorMessage != "")
                    {
                        DrawError(ErrorMessage, _textColour, gameTime);
                    }
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;

                // ============================================================================
                // INFORMATION SCREEN DRAW
                // ============================================================================
                case Scenes.InformationScreen:
                    GraphicsDevice.Clear(_backgroundColor);
                    _spriteBatch.Begin();
                    _informationComponents[InformationIndex - 1].Draw(gameTime, _spriteBatch);
                    _informationComponents[3].Draw(gameTime, _spriteBatch);
                    _informationComponents[4].Draw(gameTime, _spriteBatch);
                    _informationComponents[5].Draw(gameTime, _spriteBatch);
                    _informationComponents[6].Draw(gameTime, _spriteBatch);
                    _spriteBatch.End();
                    base.Draw(gameTime);
                    break;
            }
        }

        // ============================================================================
        // PUZZLE GENERATION METHODS
        // ============================================================================

        // ============================================================================
        // FILL CELLS - Creates a complete valid Sudoku solution
        // Called at start of new game to generate the base solved grid
        // ============================================================================
        void FillCells()
        {
            // Clear entire grid
            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    Cell[row, column].Value = 0;
                }
            }
            // Fill grid using backtracking with randomization
            Filling(0, 0);

        }

        // ============================================================================
        // FILLING - Recursive backtracking algorithm to fill grid
        // Uses randomization to create different puzzles each time
        // Returns: true if successful, false if no valid solution
        // ============================================================================
        bool Filling(int Row, int Column)
        {
            // Base case: reached end of grid
            if (Row == 9) return true;

            // Create randomized list of numbers 1-9
            List<int> AvailableNumbers = Enumerable.Range(1, 9).ToList();

            while (AvailableNumbers.Count > 0)
            {
                // Pick random number from available list
                int number = AvailableNumbers[_random.Next(AvailableNumbers.Count)];
                AvailableNumbers.Remove(number);
                bool NotRepeated = true;

                // Check row and column for conflicts
                for (int i = 0; i < 9; i++)
                {
                    if ((Cell[i, Column].Value == number && i != Row) || (Cell[Row, i].Value == number && i != Column))
                    {
                        NotRepeated = false;
                        break;
                    }
                }

                // Check 3x3 square for conflicts
                int startRow = 3 * ((Row) / 3);
                int startCol = 3 * ((Column) / 3);
                for (int i = startRow; i < startRow + 3; i++)
                {
                    for (int j = startCol; j < startCol + 3; j++)
                    {
                        if (Cell[i, j].Value == number)
                        {
                            NotRepeated = false;
                            break;
                        }
                    }
                }

                // If valid placement, try to continue filling
                if (NotRepeated)
                {
                    Cell[Row, Column].Value = number;

                    // Calculate next cell position
                    int nextRow = Column == 8 ? Row + 1 : Row;
                    int nextCol = Column == 8 ? 0 : Column + 1;

                    // Recursively fill next cell
                    if (Filling(nextRow, nextCol))
                    {
                        return true;
                    }

                    // Backtrack if filling failed
                    Cell[Row, Column].Value = 0;
                }
            }
            return false;
        }

        // ============================================================================
        // GENERATE NOTES - Creates note objects for a cell
        // Each cell has a 3x3 grid of notes for numbers 1-9
        // ============================================================================
        private void GenerateNotes(Cell Cell, int row, int col, ref List<Component> _gamecomponents)
        {
            int NotesValue = 0;
            for (int nrow = 0; nrow < 3; nrow++)
            {
                for (int ncol = 0; ncol < 3; ncol++)
                {
                    Notes[row, col, nrow, ncol] = new Note(Content.Load<SpriteFont>("Fonts/BubbleFontNotes"));
                    NotesValue++;
                    Notes[row, col, nrow, ncol].Value = NotesValue;

                    // Position note within cell (divide cell into 3x3 grid)
                    Notes[row, col, nrow, ncol].Position = new Vector2(Cell.Position.X + (ncol) * ((98) / 3) + 10, Cell.Position.Y + (nrow) * ((98) / 3) + 2);
                    Notes[row, col, nrow, ncol].NoteVisible = false;
                    _gamecomponents.Add(Notes[row, col, nrow, ncol]);
                }
            }
        }

        // ============================================================================
        // MAKE SUDOKU - Removes numbers from complete grid to create puzzle
        // Ensures puzzle has exactly one solution
        // ============================================================================
        void MakeSudoku()
        {
            // Reset game state
            _hintsUsed = 0;
            _mistakesMade = 0;
            _undoStack.Clear();
            _redoStack.Clear();

            // Clear hint tracking
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    _hintRevealedCells[row, col] = false;
                }
            }

            // Determine how many cells to show based on difficulty
            int NumberOfCellsShown = 0;
            if (_difficulty == 1) NumberOfCellsShown = 65;       // Easy: 65 clues
            else if (_difficulty == 2) NumberOfCellsShown = 48;  // Medium: 48 clues
            else if (_difficulty == 3) NumberOfCellsShown = 32;  // Hard: 32 clues
            else if (_difficulty == 4) NumberOfCellsShown = 25;  // Expert: 25 clues
            else if (_difficulty == 5) NumberOfCellsShown = 20;  // Master: 20 clues

            // Save the complete solution
            int[,] SavedSolution = new int[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    SavedSolution[row, col] = Cell[row, col].Value;
                    Cell[row, col].CellVisible = true;
                }
            }

            int CellsToRemove = 81 - NumberOfCellsShown;
            int NumberOfCellsRemoved = 0;

            // Create randomized list of all cell positions
            List<(int row, int col)> AttemptRemove = new List<(int, int)>();

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    AttemptRemove.Add((row, col));
                }
            }

            // Shuffle the list (Fisher-Yates shuffle)
            for (int removing = AttemptRemove.Count - 1; removing > 0; removing--)
            {
                int temporary = _random.Next(removing + 1);
                var temp = AttemptRemove[removing];
                AttemptRemove[removing] = AttemptRemove[temporary];
                AttemptRemove[temporary] = temp;
            }

            // Try to remove cells while maintaining unique solution
            foreach (var (row, col) in AttemptRemove)
            {
                if (NumberOfCellsRemoved >= CellsToRemove)
                {
                    break;
                }

                int savedValue = Cell[row, col].Value;
                Cell[row, col].CellVisible = false;
                Cell[row, col].Value = 0;

                // Check if puzzle still has unique solution
                if (!HasUniqueSolution(SavedSolution))
                {
                    // Restore cell if removal creates multiple solutions
                    Cell[row, col].CellVisible = true;
                    Cell[row, col].Value = savedValue;
                }
                else
                {
                    // Keep solution value but hide from player
                    Cell[row, col].Value = savedValue;
                    NumberOfCellsRemoved++;
                }
            }

            // Count visible cells (initial clues)
            int actualVisible = 0;
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Cell[row, col].CellVisible)
                    {
                        actualVisible++;
                    }
                }
            }
            _cellsCorrect = actualVisible;
        }

        // ============================================================================
        // HAS UNIQUE SOLUTION - Verifies puzzle has exactly one solution
        // Used during puzzle generation to ensure valid puzzles
        // ============================================================================
        bool HasUniqueSolution(int[,] targetSolution)
        {
            // Create working grid with only visible cells
            int[,] workingGrid = new int[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Cell[row, col].CellVisible)
                    {
                        workingGrid[row, col] = Cell[row, col].Value;
                    }
                    else
                    {
                        workingGrid[row, col] = 0;
                    }
                }
            }

            // Count solutions (should be exactly 1)
            int solutionCount = 0;
            CheckOneSolution(workingGrid, 0, 0, ref solutionCount);
            return solutionCount == 1;
        }

        // ============================================================================
        // CHECK ONE SOLUTION - Counts solutions up to 2
        // Returns: true if 1 or fewer solutions found, false if more than 1
        // ============================================================================
        bool CheckOneSolution(int[,] grid, int row, int col, ref int count)
        {
            // Stop if we've found more than one solution
            if (count > 1) return false;

            // Base case: reached end of grid (found a solution)
            if (row == 9)
            {
                count++;
                return count <= 1;
            }

            // Calculate next cell position
            int nextRow = col == 8 ? row + 1 : row;
            int nextCol = col == 8 ? 0 : col + 1;

            // Skip filled cells
            if (grid[row, col] != 0)
            {
                return CheckOneSolution(grid, nextRow, nextCol, ref count);
            }

            // Try all numbers 1-9
            for (int num = 1; num <= 9; num++)
            {
                if (IsValidPlacementInGrid(grid, row, col, num))
                {
                    grid[row, col] = num;
                    if (!CheckOneSolution(grid, nextRow, nextCol, ref count))
                    {
                        grid[row, col] = 0;
                        return false;
                    }
                    grid[row, col] = 0;
                }
            }
            return true;
        }

        // ============================================================================
        // IS VALID PLACEMENT IN GRID - Checks if number can be placed in cell
        // Validates against row, column, and 3x3 box constraints
        // ============================================================================
        bool IsValidPlacementInGrid(int[,] grid, int row, int col, int num)
        {
            // Check row and column
            for (int count = 0; count < 9; count++)
            {
                if (grid[count, col] == num || grid[row, count] == num)
                {
                    return false;
                }
            }

            // Check 3x3 box
            int startRow = 3 * (row / 3);
            int startCol = 3 * (col / 3);
            for (int i = startRow; i < startRow + 3; i++)
            {
                for (int j = startCol; j < startCol + 3; j++)
                {
                    if (grid[i, j] == num)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // ============================================================================
        // CLEAR CELLS - Resets entire grid to empty state
        // ============================================================================
        void ClearCells()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    Cell[row, column].Value = 0;
                    Cell[row, column].Input = 0;
                    Cell[row, column].CellVisible = false;
                    Cell[row, column].IsIncorrect = false;
                    _hintRevealedCells[row, column] = false;

                    // Clear all notes
                    for (int nrow = 0; nrow < 3; nrow++)
                    {
                        for (int ncolumn = 0; ncolumn < 3; ncolumn++)
                        {
                            Notes[row, column, nrow, ncolumn].NoteVisible = false;
                        }
                    }
                }
            }
        }

        // ============================================================================
        // SOLVE SUDOKU - Recursive backtracking solver
        // Fills empty cells with valid numbers
        // Returns: true if solution found, false if unsolvable
        // ============================================================================
        bool SolveSudoku()
        {
            // Find first empty cell
            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    if (Cell[row, column].Value == 0)
                    {
                        // Try numbers 1-9
                        for (int number = 1; number <= 9; number++)
                        {
                            bool NotRepeated = true;

                            // Check row and column
                            for (int i = 0; i < 9; i++)
                            {
                                if ((Cell[i, column].Value == number && i != row) || (Cell[row, i].Value == number && i != column))
                                {
                                    NotRepeated = false;
                                    break;
                                }
                            }

                            // Check 3x3 box
                            int startRow = 3 * ((row) / 3);
                            int startColumn = 3 * ((column) / 3);
                            for (int i = startRow; i < startRow + 3; i++)
                            {
                                for (int j = startColumn; j < startColumn + 3; j++)
                                {
                                    if (Cell[i, j].Value == number)
                                    {
                                        NotRepeated = false;
                                        break;
                                    }
                                }
                            }

                            // If valid, try this number and recurse
                            if (NotRepeated)
                            {
                                Cell[row, column].Value = number;
                                if (SolveSudoku())
                                {
                                    return true;
                                }
                                Cell[row, column].Value = 0; // Backtrack

                            }
                        }
                        return false; // No valid number found
                    }
                }
            }
            return true; // Grid complete
        }

        // ============================================================================
        // IS SUDOKU VALID - Checks if current grid has no duplicate numbers
        // Used to validate user input in solver mode
        // ============================================================================
        bool IsSudokuValid()
        {
            for (int numberrow = 0; numberrow < 9; numberrow++)
            {
                bool[] row = new bool[10];  // Track numbers 1-9
                bool[] col = new bool[10];
                bool[] box = new bool[10];

                for (int numbercol = 0; numbercol < 9; numbercol++)
                {
                    // Check row for duplicates
                    int rowcheck = Cell[numberrow, numbercol].Input;
                    if (rowcheck != 0 && row[rowcheck]) return false;
                    if (rowcheck != 0) row[rowcheck] = true;

                    // Check column for duplicates
                    int colcheck = Cell[numbercol, numberrow].Input;
                    if (colcheck != 0 && col[colcheck]) return false;
                    if (colcheck != 0) col[colcheck] = true;

                    // Check 3x3 box for duplicates
                    int boxrow = 3 * (numberrow / 3) + numbercol / 3;
                    int boxcol = 3 * (numberrow % 3) + numbercol % 3;
                    int boxcheck = Cell[boxrow, boxcol].Input;
                    if (boxcheck != 0 && box[boxcheck]) return false;
                    if (boxcheck != 0) box[boxcheck] = true;
                }
            }
            return true;
        }

        // ============================================================================
        // TEXT INPUT HANDLER
        // Processes keyboard input for text boxes
        // ============================================================================
        private void TextInputHandler(object sender, TextInputEventArgs args)
        {
            var character = args.Character;

            // Ignore if no text box is focused
            if (currentTextBox == null)
            {
                return;
            }

            // Handle backspace
            if (args.Key == Keys.Back)
            {
                if (currentTextBox.Text.Length > 0)
                {
                    currentTextBox.Text = currentTextBox.Text.Substring(0, currentTextBox.Text.Length - 1);
                }
                return;
            }

            // Ignore control characters
            if (char.IsControl(character))
            {
                return;
            }

            // Add character to text
            currentTextBox.Text += character;
        }

        // ============================================================================
        // DATABASE METHODS - Account management and leaderboard
        // ============================================================================

        // ============================================================================
        // ACCOUNT EXISTS - Checks if username/password combination is valid
        // Returns: true if account found, false otherwise
        // ============================================================================
        public static bool AccountExists(string GivenUsername, string GivenPassword)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                string query = "SELECT COUNT(*) FROM Users WHERE username = @username AND userpass = @password";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", GivenUsername);
                    command.Parameters.AddWithValue("@password", HashPassword(GivenPassword));

                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    if (count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        // ============================================================================
        // CREATE ACCOUNT - Creates new user account in database
        // Returns: true if successful, false if username already exists
        // ============================================================================
        public static bool CreateAccount(string givenUsername, string givenPassword)
        {
            if (givenPassword != "" && givenUsername != "")
            {
                // Check if username already exists
                using (SqlConnection checkConnection = new SqlConnection(ConnectionString))
                {
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE username = @username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, checkConnection))
                    {
                        checkCommand.Parameters.AddWithValue("@username", givenUsername);
                        checkConnection.Open();
                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            return false; // Username already exists
                        }
                    }
                }

                // Create new account
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    string insertQuery = @"INSERT INTO Users (username, userpass) 
                               VALUES (@username, @password)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", givenUsername);
                        command.Parameters.AddWithValue("@password", HashPassword(givenPassword));

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            else
            {
                return false;
            }

        }

        // ============================================================================
        // HASH PASSWORD - Creates SHA256 hash of password
        // Used for secure password storage
        // ============================================================================
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // ============================================================================
        // PARSE NULLABLE TIME - Converts database value to TimeSpan
        // Handles null/empty values
        // ============================================================================
        private static TimeSpan? ParseNullableTime(object field)
        {
            if (field == DBNull.Value || string.IsNullOrWhiteSpace(field?.ToString())) return null;
            return TimeSpan.TryParse(field.ToString(), out var ts) ? ts : (TimeSpan?)null;
        }

        // ============================================================================
        // CHECK BEST TIME - Updates user's best time if new time is better
        // Called when player completes a puzzle
        // ============================================================================
        public static void CheckBestTime(string username, string difficulty, TimeSpan? timeComplete)
        {

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // Get user's primary key
                string PKQuery = "SELECT user_pk FROM Users WHERE username = @username";
                int userPk;
                using (SqlCommand getPKCommand = new SqlCommand(PKQuery, connection))
                {
                    getPKCommand.Parameters.AddWithValue("@username", username);
                    object userResult = getPKCommand.ExecuteScalar();
                    if (userResult == null)
                    {
                        return;
                    }
                    userPk = (int)userResult;

                    // Get existing best time for this difficulty
                    string SecondQuery = "SELECT Time FROM BestTimes WHERE user_pk = @userPk AND DifficultyName = @difficulty";
                    using (SqlCommand secondcommand = new SqlCommand(SecondQuery, connection))
                    {
                        secondcommand.Parameters.AddWithValue("@userPk", userPk);
                        secondcommand.Parameters.AddWithValue("@difficulty", difficulty);
                        object Secondresult = secondcommand.ExecuteScalar();
                        TimeSpan? bestTime = ParseNullableTime(Secondresult);

                        // Update if no existing time or new time is better
                        if (bestTime == null || timeComplete < bestTime)
                        {
                            if (Secondresult == null)
                            {
                                // Insert new record
                                string insertQuery = "INSERT INTO BestTimes (user_pk, DifficultyName, Time) VALUES (@userPk, @difficulty, @timeComplete)";
                                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@userPk", userPk);
                                    insertCommand.Parameters.AddWithValue("@difficulty", difficulty);
                                    insertCommand.Parameters.AddWithValue("@timeComplete", timeComplete.Value.ToString());
                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Update existing record
                                string updateQuery = "UPDATE BestTimes SET Time = @timeComplete WHERE user_pk = @userPk AND DifficultyName = @difficulty";
                                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@userPk", userPk);
                                    updateCommand.Parameters.AddWithValue("@difficulty", difficulty);
                                    updateCommand.Parameters.AddWithValue("@timeComplete", timeComplete.Value.ToString());
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                }
            }
        }

        // ============================================================================
        // GET LEADERBOARD TIMES - Retrieves top 10 times for given difficulty
        // Returns: List of LeaderboardEntry objects
        // ============================================================================
        public List<LeaderboardEntry> GetLeaderBoardTimes(int difficulty)
        {
            List<LeaderboardEntry> LeaderBoardEntries = new List<LeaderboardEntry>();

            // Convert difficulty number to name
            string diffName;
            if (difficulty == 1)
            {
                diffName = "easy";
            }
            else if (difficulty == 2)
            {
                diffName = "medium";
            }
            else if (difficulty == 3)
            {
                diffName = "hard";
            }
            else if (difficulty == 4)
            {
                diffName = "expert";
            }
            else
            {
                diffName = "master";
            }

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // Get top 10 times for difficulty, sorted by time
                string query = "SELECT TOP 10 Users.username, BestTimes.Time FROM BestTimes INNER JOIN Users ON BestTimes.user_pk = Users.user_pk WHERE BestTimes.DifficultyName = @difficulty AND BestTimes.Time IS NOT NULL ORDER BY CAST(BestTimes.Time AS TIME) ASC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@difficulty", diffName);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string username = reader.GetString(0);
                            TimeSpan? time = ParseNullableTime(reader[1]);

                            if (time.HasValue)
                            {
                                LeaderBoardEntries.Add(new LeaderboardEntry
                                {
                                    Username = username,
                                    BestTime = time.Value
                                });
                            }
                        }
                    }
                }
            }
            return LeaderBoardEntries;
        }

        // ============================================================================
        // NOTES STATE MANAGEMENT
        // Helper methods for undo/redo functionality
        // ============================================================================

        // ============================================================================
        // GET CELL NOTE STATE - Returns current state of all notes in a cell
        // Used for undo/redo
        // ============================================================================
        private bool[,] GetCellNoteState(int row, int col)
        {
            bool[,] noteState = new bool[3, 3];
            for (int nrow = 0; nrow < 3; nrow++)
            {
                for (int ncol = 0; ncol < 3; ncol++)
                {
                    noteState[nrow, ncol] = Notes[row, col, nrow, ncol].NoteVisible;
                }
            }
            return noteState;
        }

        // ============================================================================
        // SET CELL NOTE STATE - Restores note state in a cell
        // Used for undo/redo
        // ============================================================================
        private void SetCellNoteState(int row, int col, bool[,] noteState)
        {
            for (int nrow = 0; nrow < 3; nrow++)
            {
                for (int ncol = 0; ncol < 3; ncol++)
                {
                    Notes[row, col, nrow, ncol].NoteVisible = noteState[nrow, ncol];
                }
            }
        }
        // ============================================================================
        // LOAD PERSONAL BEST TIMES - Loads user's best times from database
        // Updates display strings for each difficulty
        // ============================================================================
        public void LoadPersonalBestTImes(string username)
        {
            UserBestTimeEasy = "--:--:--";
            UserBestTimeMedium = "--:--:--";
            UserBestTimeHard = "--:--:--";
            UserBestTimeExpert = "--:--:--";
            UserBestTimeMaster = "--:--:--";

            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                username = username.Trim();

                // Get user's primary key
                string PKQuery = "SELECT user_pk FROM Users WHERE username = @username";
                int userPk;

                using (SqlCommand getUserCommand = new SqlCommand(PKQuery, connection))
                {
                    getUserCommand.Parameters.AddWithValue("@username", username);
                    object userResult = getUserCommand.ExecuteScalar();
                    if (userResult == null)
                    {
                        return;
                    }
                    userPk = (int)userResult;
                }

                // Get all best times for user
                string query = "SELECT DifficultyName, Time FROM BestTimes WHERE user_pk = @userPk";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userPk", userPk);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string difficultyName = reader.GetString(0).Trim();
                            TimeSpan? time = ParseNullableTime(reader[1]);

                            if (time.HasValue)
                            {
                                string timeString = time.Value.ToString(@"hh\:mm\:ss");

                                // Update appropriate difficulty string
                                if (difficultyName.ToLower() == "easy")
                                {
                                    UserBestTimeEasy = timeString;
                                }
                                else if (difficultyName.ToLower() == "medium")
                                {
                                    UserBestTimeMedium = timeString;
                                }
                                else if (difficultyName.ToLower() == "hard")
                                {
                                    UserBestTimeHard = timeString;
                                }
                                else if (difficultyName.ToLower() == "expert")
                                {
                                    UserBestTimeExpert = timeString;
                                }
                                else if (difficultyName.ToLower() == "master")
                                {
                                    UserBestTimeMaster = timeString;
                                }
                            }
                        }
                    }
                }
            }
        }
        // ============================================================================
        // LOAD USER SETTINGS - Loads user's game settings from database
        // Called when user logs in
        // ============================================================================
        public void LoadUserSettings(string username)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                username = username.Trim();

                // Get user's primary key
                string getUserPkQuery = "SELECT user_pk FROM Users WHERE username = @username";
                int userPk;

                using (SqlCommand getUserCommand = new SqlCommand(getUserPkQuery, connection))
                {
                    getUserCommand.Parameters.AddWithValue("@username", username);
                    object userResult = getUserCommand.ExecuteScalar();
                    if (userResult == null)
                    {
                        // Set defaults if user not found
                        HintLimit = false;
                        LivesLimit = false;
                        MistakeHighlighter = false;
                        ShowTimer = true;
                        MusicOn = true;
                        SoundEffectsOn = true;
                        return;
                    }
                    userPk = (int)userResult;
                }

                // Get all settings for user
                string query = "SELECT SettingsType, TurnedOn FROM Settings WHERE user_pk = @userPk";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userPk", userPk);

                    // Initialize to defaults
                    HintLimit = false;
                    LivesLimit = false;
                    MistakeHighlighter = false;
                    ShowTimer = true;
                    MusicOn = true;
                    SoundEffectsOn = true;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string settingType = reader.GetString(0).Trim();
                            bool turnedOn = reader.GetBoolean(1);

                            // Update appropriate setting
                            if (settingType.ToLower() == "hintlimit")
                            {
                                HintLimit = turnedOn;
                            }
                            else if (settingType.ToLower() == "liveslimit")
                            {
                                LivesLimit = turnedOn;
                            }
                            else if (settingType.ToLower() == "mistake")
                            {
                                MistakeHighlighter = turnedOn;
                            }
                            else if (settingType.ToLower() == "showtimer")
                            {
                                ShowTimer = turnedOn;
                            }
                            else if (settingType.ToLower() == "musicon")
                            {
                                MusicOn = turnedOn;
                            }
                            else if (settingType.ToLower() == "soundeffects")
                            {
                                SoundEffectsOn = turnedOn;
                            }
                        }
                    }
                }
            }
        }
        // ============================================================================
        // SAVE USER SETTINGS - Saves current settings to database
        // Only saves settings that differ from defaults to save space
        // ============================================================================
        public void SaveUserSettings(string username)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // Get user's primary key
                string getUserPkQuery = "SELECT user_pk FROM Users WHERE username = @username";
                int userPk;

                using (SqlCommand getUserCommand = new SqlCommand(getUserPkQuery, connection))
                {
                    getUserCommand.Parameters.AddWithValue("@username", username);
                    object userResult = getUserCommand.ExecuteScalar();
                    if (userResult == null)
                    {
                        return;
                    }
                    userPk = (int)userResult;
                }

                // Define settings and their defaults
                string[] settingTypes = new string[] { "hintlimit", "liveslimit", "mistake", "showtimer", "musicon", "soundeffects" };
                bool[] settingValues = new bool[] { HintLimit, LivesLimit, MistakeHighlighter, ShowTimer, MusicOn, SoundEffectsOn };
                bool[] defaultValues = new bool[] { false, false, false, true, true, true };

                for (int i = 0; i < settingTypes.Length; i++)
                {
                    // Delete settings that match defaults
                    if (settingValues[i] == defaultValues[i])
                    {
                        string deleteQuery = "DELETE FROM Settings WHERE user_pk = @userPk AND SettingsType = @settingType";
                        using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@userPk", userPk);
                            deleteCommand.Parameters.AddWithValue("@settingType", settingTypes[i]);
                            deleteCommand.ExecuteNonQuery();
                        }
                    }
                    // Save non-default settings
                    else
                    {
                        // Check if setting exists
                        string checkQuery = "SELECT COUNT(*) FROM Settings WHERE user_pk = @userPk AND SettingsType = @settingType";
                        int exists = 0;

                        using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@userPk", userPk);
                            checkCommand.Parameters.AddWithValue("@settingType", settingTypes[i]);
                            exists = (int)checkCommand.ExecuteScalar();
                        }

                        if (exists > 0)
                        {
                            // Update existing setting
                            string updateQuery = "UPDATE Settings SET TurnedOn = @turnedOn WHERE user_pk = @userPk AND SettingsType = @settingType";
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@userPk", userPk);
                                updateCommand.Parameters.AddWithValue("@settingType", settingTypes[i]);
                                updateCommand.Parameters.AddWithValue("@turnedOn", settingValues[i] ? 1 : 0);
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Insert new setting
                            string insertQuery = "INSERT INTO Settings (user_pk, SettingsType, TurnedOn) VALUES (@userPk, @settingType, @turnedOn)";
                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@userPk", userPk);
                                insertCommand.Parameters.AddWithValue("@settingType", settingTypes[i]);
                                insertCommand.Parameters.AddWithValue("@turnedOn", settingValues[i] ? 1 : 0);
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        // ============================================================================
        // WRAP TEXT - Breaks text into lines that fit within max width
        // Used for error message display
        // Returns: List of text lines
        // ============================================================================
        private List<string> WrapText(SpriteFont font, string text)
        {
            int maxWidth = 738;
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                Vector2 size = font.MeasureString(testLine);

                if (size.X > maxWidth)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        lines.Add(word);
                        currentLine = "";
                    }
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
            return lines;
        }

        // ============================================================================
        // IS CORRECT - Checks if puzzle is complete and handles win condition
        // ============================================================================
        private void IsCorrect()
        {
            if (_cellsCorrect == 81)
            {
                _currentScene = Scenes.WinnerScreen;
                if (SoundEffectsOn)
                {
                    winSound.Play();
                }
                FinalTime = (int)elapsedTime;

                // Don't save time if not logged in
                if (UserID == null)
                {
                    return;
                }

                // Save best time for appropriate difficulty
                if (_difficulty == 1)
                {
                    CheckBestTime(UserID, "easy", TimeSpan.FromSeconds((FinalTime)));
                }
                else if (_difficulty == 2)
                {
                    CheckBestTime(UserID, "medium", TimeSpan.FromSeconds((FinalTime)));
                }
                else if (_difficulty == 3)
                {
                    CheckBestTime(UserID, "hard", TimeSpan.FromSeconds((FinalTime)));
                }
                else if (_difficulty == 4)
                {
                    CheckBestTime(UserID, "expert", TimeSpan.FromSeconds((FinalTime)));
                }
                else if (_difficulty == 5)
                {
                    CheckBestTime(UserID, "master", TimeSpan.FromSeconds((FinalTime)));
                }
                return;
            }
        }

        // ============================================================================
        // DRAW ERROR - Displays error message in dialog box
        // ============================================================================
        private void DrawError(string ErrorMessage, Color _textColour, GameTime gameTime)
        {
            var Font = Content.Load<SpriteFont>("Fonts/BubbleFont");
            ErrorBox.Draw(gameTime, _spriteBatch);

            // Wrap text to fit in error box
            List<string> wrappedLines = WrapText(Font, ErrorMessage);

            int startY = 330;
            float lineHeight = Font.LineSpacing;

            // Draw each line centered
            for (int i = 0; i < wrappedLines.Count; i++)
            {
                Vector2 textSize = Font.MeasureString(wrappedLines[i]);
                float textX = 603 + (704 - textSize.X) / 2;
                float textY = startY + (i * lineHeight);

                _spriteBatch.DrawString(Font, wrappedLines[i], new Vector2(textX, textY), _textColour);
            }
            OkButton.Draw(gameTime, _spriteBatch);
        }

        // ============================================================================
        // COUNT ALL SOLUTIONS - Counts total solutions for a puzzle (up to 1000)
        // Used in solver to verify puzzle validity
        // Returns: Number of solutions, or -1 if more than 1000
        // ============================================================================
        int CountAllSolutions(int[,] grid, int row, int col)
        {
            // Base case: reached end of grid
            if (row == 9)
            {
                return 1;
            }

            // Calculate next cell position
            int nextRow = col == 8 ? row + 1 : row;
            int nextCol = col == 8 ? 0 : col + 1;

            // Skip filled cells
            if (grid[row, col] != 0)
            {
                return CountAllSolutions(grid, nextRow, nextCol);
            }

            int totalSolutions = 0;

            // Try all numbers 1-9
            for (int num = 1; num <= 9; num++)
            {
                if (IsValidPlacementInGrid(grid, row, col, num))
                {
                    grid[row, col] = num;
                    int result = CountAllSolutions(grid, nextRow, nextCol);

                    // Stop counting if we hit 1000+
                    if (result == -1)
                    {
                        grid[row, col] = 0;
                        return -1;
                    }

                    totalSolutions += result;
                    grid[row, col] = 0;

                    // Return -1 if more than 1000 solutions
                    if (totalSolutions > 1000)
                    {
                        return -1;
                    }
                }
            }
            return totalSolutions;
        }
    }
}