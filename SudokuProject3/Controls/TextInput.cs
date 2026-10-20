// ============================================================================
// USING DIRECTIVES - Required namespaces for TextInput component
// ============================================================================
using Microsoft.IdentityModel.Tokens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3.Controls
{
    // ============================================================================
    // TEXTINPUT CLASS - Represents a clickable text input field
    // Used for username and password entry on sign-in and create account screens
    // Handles display of typed text and focus detection through clicks
    // ============================================================================
    internal class TextInput : Component
    {
        // ============================================================================
        // MOUSE STATE TRACKING
        // Used to detect clicks to focus the text field
        // ============================================================================
        private MouseState _currentMouse;   // Current frame's mouse state
        private MouseState _previousMouse;  // Previous frame's mouse state (for click detection)

        // ============================================================================
        // RENDERING COMPONENTS
        // ============================================================================
        private SpriteFont _font;                               // Font for rendering text
        private Color _textColour = new Color(0x73, 0x99, 0x92); // Light teal color for text

        // ============================================================================
        // CLICK EVENT
        // Raised when text field is clicked to set it as the active input
        // Handled in Game1.cs LoadContent to set currentTextBox
        // ============================================================================
        public event EventHandler Click;

        // ============================================================================
        // TEXT INPUT PROPERTIES
        // ============================================================================
        public Vector2 Position { get; set; }  // Screen position of the text field
        public string Text { get; set; }       // Current text content (entered by user)

        // ============================================================================
        // CONSTRUCTOR - Initializes text input with font and empty text
        // ============================================================================
        public TextInput(SpriteFont font)
        {
            _font = font;
            Text = ""; // Start with empty text field
        }

        // ============================================================================
        // RECTANGLE PROPERTY - Defines clickable area of the text field
        // Fixed size of 711x108 pixels to match the text input design
        // ============================================================================
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, 711, 108);
            }
        }

        // ============================================================================
        // DRAW METHOD - Renders the text content if not empty
        // Called every frame by the game loop
        // ============================================================================
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Only draw text if user has entered something
            // Empty fields show placeholder from background image
            if (!string.IsNullOrEmpty(Text))
            {
                spriteBatch.DrawString(_font, Text, new Vector2(Position.X, Position.Y), _textColour);
            }
        }

        // ============================================================================
        // UPDATE METHOD - Handles mouse interaction and focus detection
        // Called every frame by the game loop
        // When clicked, this field becomes the active text input (currentTextBox in Game1.cs)
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
            // When clicked, raises Click event which sets this as the active input field
            // ============================================================================
            if (mouseRectangle.Intersects(Rectangle))
            {
                // Check for click: mouse was down last frame, up this frame
                if (_currentMouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
                {
                    if (Click != null)
                    {
                        // Raise click event (handled in Game1.cs to set currentTextBox)
                        // Once set as currentTextBox, TextInputHandler in Game1.cs will
                        // direct all keyboard input to this field
                        Click(this, new EventArgs());
                    }
                }
            }
        }
    }
}