// ============================================================================
// USING DIRECTIVES - Required namespaces for Cell component
// ============================================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3.Controls
{
    // ============================================================================
    // CELL CLASS - Represents a single cell in the Sudoku grid
    // Handles rendering, user interaction, and visual states
    // ============================================================================
    internal class Cell : Component
    {
        // ============================================================================
        // MOUSE STATE TRACKING
        // Used to detect clicks on the cell
        // ============================================================================
        private MouseState _currentMouse;   // Current frame's mouse state
        private MouseState _previousMouse;  // Previous frame's mouse state (for click detection)

        // ============================================================================
        // RENDERING COMPONENTS
        // ============================================================================
        private SpriteFont _font;           // Font for rendering numbers
        private Texture2D _texture;         // Cell background texture

        // ============================================================================
        // GRID POSITION IDENTIFIERS
        // ============================================================================
        public int _row;      // Row index (0-8)
        public int _column;   // Column index (0-8)
        public int _square;   // 3x3 square index (1-9)

        // ============================================================================
        // COLOR DEFINITIONS
        // All colors defined as RGB hex values for consistent theming
        // ============================================================================
        public Color textColor = new Color(0x62, 0X8F, 0x87);              // Default text color (teal)
        private Color _oddSquareCellColour = new Color(0xE8, 0xF9, 0xF7);  // Light cyan (odd squares 1,3,5,7,9)
        private Color _selectedCellColour = new Color(0x88, 0xBD, 0xB6);   // Darker teal (selected cell)
        private Color _selectedRCorSColour = new Color(0xAF, 0xEB, 0xE4);  // Light teal (row/col or square highlight)
        private Color _selectedRCandSColour = new Color(0xA0, 0xD4, 0xCE); // Medium teal (row/col AND square highlight)
        private Color _incorrectCellColour = new Color(0xFA, 0xA1, 0x8F);  // Salmon red (incorrect entry)

        // ============================================================================
        // CLICK EVENT
        // Raised when cell is clicked, handled by Game1 class
        // ============================================================================
        public event EventHandler Click;

        // ============================================================================
        // CELL STATE PROPERTIES
        // ============================================================================
        public bool CellVisible { get; set; }    // True if this is a pre-filled clue (or hint-revealed)
        public bool IsIncorrect { get; set; }    // True if mistake highlighter is on and entry is wrong

        // ============================================================================
        // POSITION AND BOUNDS
        // ============================================================================
        public Vector2 Position { get; set; }    // Screen position (x, y)

        // Rectangle property defines clickable area and draw bounds
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            }
        }

        // ============================================================================
        // CELL VALUES
        // ============================================================================
        public int Value { get; set; }           // Correct answer for this cell (1-9)
        public int Input { get; set; }           // Player's current input (0 = empty, 1-9 = number)

        // ============================================================================
        // VISUAL HIGHLIGHT STATES
        // Multiple cells can be highlighted simultaneously
        // ============================================================================
        public bool IsSelected { get; set; } = false;       // This cell is the actively selected cell
        public bool IsCrossSelected { get; set; } = false;  // This cell is in same row/column as selected cell
        public bool IsSquareSelected { get; set; } = false; // This cell is in same 3x3 square as selected cell

        // ============================================================================
        // CONSTRUCTOR - Initializes cell with texture and font
        // ============================================================================
        public Cell(Texture2D texture, SpriteFont font)
        {
            _texture = texture;
            _font = font;
        }

        // ============================================================================
        // DRAW METHOD - Renders the cell with appropriate colors and number
        // Called every frame by the game loop
        // ============================================================================
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // ============================================================================
            // DETERMINE BACKGROUND COLOR
            // Priority order (later conditions override earlier ones):
            // 1. Default: White (even squares) or light cyan (odd squares)
            // 2. Cross-selected or square-selected: Light teal
            // 3. Both cross and square selected: Medium teal
            // 4. Selected cell: Darker teal
            // 5. Incorrect entry: Red
            // ============================================================================

            // Base color: alternate between white and light cyan for odd squares
            // This creates the checkerboard pattern on the Sudoku grid
            Color cellColor = (_square % 2 == 1) ? _oddSquareCellColour : Color.White;

            // Highlight if in same row/column AND same square as selected cell
            if (IsCrossSelected == true && IsSquareSelected == true)
            {
                cellColor = _selectedRCandSColour;
            }
            // Highlight if in same row/column OR same square as selected cell
            else if (IsCrossSelected == true || IsSquareSelected)
            {
                cellColor = _selectedRCorSColour;
            }

            // Override with selection color if this is the selected cell
            if (IsSelected == true)
            {
                cellColor = _selectedCellColour;
            }

            // Override with error color if entry is incorrect
            if (IsIncorrect == true)
            {
                cellColor = _incorrectCellColour;
            }

            // Draw the cell background with determined color
            spriteBatch.Draw(_texture, Rectangle, cellColor);

            // ============================================================================
            // DRAW PLAYER INPUT
            // Only drawn if cell has input, is not empty, and is not a pre-filled clue
            // ============================================================================
            if (Input > 0 && CellVisible == false)
            {
                string numberText = Input.ToString();
                Vector2 textSize = _font.MeasureString(numberText);

                // Center the number within the cell
                Vector2 textPosition = new Vector2(
                    Position.X + (_texture.Width - textSize.X) / 2,
                    Position.Y + (_texture.Height - textSize.Y) / 2
                );

                // Use regular teal color for player input
                textColor = new Color(0x62, 0X8F, 0x87);
                spriteBatch.DrawString(_font, numberText, textPosition, textColor);
            }

            // ============================================================================
            // DRAW PRE-FILLED CLUE OR HINT
            // Drawn if cell is marked as visible (initial clue or hint-revealed)
            // ============================================================================
            if (CellVisible == true)
            {
                string numberText = Value.ToString();
                Vector2 textSize = _font.MeasureString(numberText);

                // Center the number within the cell
                Vector2 textPosition = new Vector2(
                    Position.X + (_texture.Width - textSize.X) / 2,
                Position.Y + (_texture.Height - textSize.Y) / 2);

                // Use darker teal color for pre-filled numbers to distinguish from player input
                textColor = new Color(0X35, 0x67, 0x5F);
                spriteBatch.DrawString(_font, numberText, textPosition, textColor);
            }
        }

        // ============================================================================
        // UPDATE METHOD - Handles mouse interaction and click detection
        // Called every frame by the game loop
        // ============================================================================
        public override void Update(GameTime gameTime)
        {
            // ============================================================================
            // MOUSE STATE TRACKING
            // Store previous state and get current state for click detection
            // ============================================================================
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            // Create 1x1 rectangle at mouse position for intersection testing
            var mouseRectangle = new Rectangle(_currentMouse.X, _currentMouse.Y, 1, 1);

            // ============================================================================
            // CLICK DETECTION
            // Only trigger if mouse was pressed last frame and released this frame
            // This prevents holding the mouse button and triggering multiple clicks
            // ============================================================================
            if (mouseRectangle.Intersects(Rectangle))
            {
                // Check for click: mouse was down last frame, up this frame
                if (_currentMouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
                {
                    // Double-check mouse is still within cell bounds
                    if (Rectangle.Contains(_currentMouse.Position))
                    {
                        // Raise click event (handled in Game1.cs by Cell_Click method)
                        Click?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
    }
}