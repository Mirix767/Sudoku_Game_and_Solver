using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3.Aestetics
{
    // ============================================================================
    // NON-MOVING COMPONENT
    // Represents a static UI element that displays a texture with optional text
    // Used for backgrounds, labels, and non-interactive visual elements
    // ============================================================================
    internal class NonMoving : Component
    {
        // ========================================
        // FIELDS - Visual Resources
        // ========================================

        // The texture that will be rendered for this component
        private Texture2D _texture;

        // The font used to render text overlay on the texture
        private SpriteFont _font;

        // The color tint applied to the texture when rendering
        private Color _colour;

        // ========================================
        // PROPERTIES - Position and Display
        // ========================================

        // Gets or sets the position of this component on the screen
        // Represents the top-left corner of the component
        public Vector2 Position { get; set; }

        // Gets or sets the text to be displayed centered on the component
        // If null or empty, no text will be rendered
        public string Text { get; set; }

        // Gets or sets the color of the text overlay
        public Color _textcolour { get; set; }

        // ============================================================================
        // CONSTRUCTOR - Initializes a new NonMoving component
        // ============================================================================
        public NonMoving(Texture2D texture, SpriteFont font, Color colour)
        {
            _texture = texture;
            _font = font;
            _colour = colour;
        }

        // ========================================
        // PROPERTIES - Calculated Bounds
        // ========================================

        // Gets the rectangular bounds of this component on the screen
        // The rectangle is defined by the Position and the texture's dimensions
        // Used for rendering and potentially for collision detection
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            }
        }

        // ============================================================================
        // DRAW - Renders the component to the screen
        // Performs two rendering operations:
        // 1. Draws the texture with the specified color tint
        // 2. If text is present, draws it centered on the texture
        // ============================================================================
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw the base texture with color tint
            spriteBatch.Draw(_texture, Rectangle, _colour);

            // Draw text overlay if text is provided
            if (!string.IsNullOrEmpty(Text))
            {
                // Calculate horizontal center position for the text
                var x = (Rectangle.X + (Rectangle.Width / 2)) - (_font.MeasureString(Text).X / 2);

                // Calculate vertical center position for the text
                var y = (Rectangle.Y + (Rectangle.Width / 2)) - (_font.MeasureString(Text).Y / 2);

                // Render the centered text with the specified text color
                spriteBatch.DrawString(_font, Text, new Vector2(x, y), _textcolour);
            }
        }

        // ============================================================================
        // UPDATE - Updates the component's state
        // Since this is a non-moving, non-interactive component, this method is empty
        // No updates are performed as the component remains static throughout its lifetime
        // ============================================================================
        public override void Update(GameTime gameTime)
        {
            // No update logic needed for static components
        }
    }
}