// ============================================================================
// USING DIRECTIVES - Required namespaces for Button component
// ============================================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SudokuProject3.Controls
{
    // ============================================================================
    // BUTTON CLASS - Represents a clickable button with optional text overlay
    // Used throughout the game for navigation, number input, and game actions
    // Supports hover effects and dynamic texture changes
    // ============================================================================
    internal class Button : Component
    {
        // ============================================================================
        // MOUSE STATE TRACKING
        // Used to detect clicks and hover states
        // ============================================================================
        private MouseState _currentMouse;   // Current frame's mouse state
        private MouseState _previousMouse;  // Previous frame's mouse state (for click detection)

        // ============================================================================
        // RENDERING COMPONENTS
        // ============================================================================
        private SpriteFont _font;           // Font for rendering button text
        private Texture2D _texture;         // Button background image
        private Color _colour;              // Current tint color (changes on hover)

        // ============================================================================
        // INTERACTION STATE
        // ============================================================================
        private bool _isHovering;           // True when mouse is over the button

        // ============================================================================
        // CLICK EVENT
        // Raised when button is clicked
        // Handled in Game1.cs LoadContent with lambda expressions or method references
        // ============================================================================
        public event EventHandler Click;

        // ============================================================================
        // BUTTON PROPERTIES
        // ============================================================================
        public int Value { get; set; }              // Numeric value for number buttons (1-9)
        public Color PenColour { get; set; }        // Color for button text
        public Vector2 Position { get; set; }       // Screen position of the button

        // ============================================================================
        // RECTANGLE PROPERTY - Defines clickable area and draw bounds
        // Automatically sized to match the texture dimensions
        // ============================================================================
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            }
        }

        // ============================================================================
        // TEXT PROPERTY - Optional text to display on button
        // Used for difficulty bubbles and other labeled buttons
        // ============================================================================
        public string Text { get; set; }

        // ============================================================================
        // TEXTURE PROPERTY - Allows dynamic texture changes
        // Used for toggle switches (on/off) and notes button state changes
        // ============================================================================
        public Texture2D Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        // ============================================================================
        // CONSTRUCTOR - Initializes button with texture and font
        // ============================================================================
        public Button(Texture2D texture, SpriteFont font)
        {
            _texture = texture;
            _font = font;
        }

        // ============================================================================
        // DRAW METHOD - Renders the button with hover effect and optional text
        // Called every frame by the game loop
        // ============================================================================
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // ============================================================================
            // HOVER EFFECT - Tint button light blue when mouse is over it
            // ============================================================================
            if (_isHovering)
            {
                _colour = Color.LightBlue;  // Visual feedback for hover state
            }
            else
            {
                _colour = Color.White;      // Normal state (no tint)
            }

            // Draw button background with current color tint
            spriteBatch.Draw(_texture, Rectangle, _colour);

            // ============================================================================
            // DRAW TEXT OVERLAY - Center text on button if text is provided
            // Used for buttons with labels (difficulty names, menu options, etc.)
            // ============================================================================
            if (!string.IsNullOrEmpty(Text))
            {
                // Calculate centered position for text
                // X: Center horizontally within button
                var x = (Rectangle.X + (Rectangle.Width / 2)) - (_font.MeasureString(Text).X / 2);
                // Y: Center vertically within button
                var y = (Rectangle.Y + (Rectangle.Width / 2)) - (_font.MeasureString(Text).Y / 2);

                // Draw text in specified color (PenColour)
                spriteBatch.DrawString(_font, Text, new Vector2(x, y), PenColour);
            }
        }

        // ============================================================================
        // UPDATE METHOD - Handles mouse interaction, hover detection, and clicks
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

            // Reset hover state (will be set to true if mouse is over button)
            _isHovering = false;

            // ============================================================================
            // HOVER AND CLICK DETECTION
            // Check if mouse is over button and handle click events
            // ============================================================================
            if (mouseRectangle.Intersects(Rectangle))
            {
                _isHovering = true;  // Enable hover effect

                // Check for click: mouse was down last frame, up this frame
                if (_currentMouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
                {
                    if (Click != null)
                    {
                        // Raise click event (handled in Game1.cs)
                        // Different buttons trigger different actions:
                        // - Number buttons: NumberButton_Click
                        // - Navigation: Lambda expressions changing _currentScene
                        // - Game actions: Hint, Undo, Redo, Eraser.
                        Click(this, new EventArgs());
                    }
                }
            }
        }
    }
}