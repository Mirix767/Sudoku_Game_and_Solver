// ============================================================================
// USING DIRECTIVES - Required namespaces for Note component
// ============================================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3.Controls
{
    // ============================================================================
    // NOTE CLASS - Represents a single note/pencil mark within a cell
    // Each cell contains a 3x3 grid of Note objects (9 total) for numbers 1-9
    // Used when players want to mark possible candidates for a cell
    // ============================================================================
    internal class Note : Component
    {
        // ============================================================================
        // RENDERING COMPONENTS
        // ============================================================================
        private SpriteFont _font;                               // Font for rendering note numbers
        private Color _textColour = new Color(0x73, 0x99, 0x92); // Light teal color for note text

        // ============================================================================
        // NOTE PROPERTIES
        // ============================================================================
        public bool NoteVisible { get; set; }  // Whether this note is currently displayed
        public Vector2 Position { get; set; }  // Screen position of this note
        public int Value { get; set; }         // The number this note represents (1-9)

        // ============================================================================
        // CONSTRUCTOR - Initializes note with font
        // Position and Value are set by the GenerateNotes method in Game1.cs
        // ============================================================================
        public Note(SpriteFont font)
        {
            _font = font;
        }

        // ============================================================================
        // DRAW METHOD - Renders the note number if visible
        // Called every frame by the game loop
        // Notes are only drawn when NoteVisible is true (toggled by player in notes mode)
        // ============================================================================
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Only draw if note has been toggled on by the player
            if (NoteVisible == true)
            {
                string numberText = Value.ToString();
                spriteBatch.DrawString(_font, numberText, Position, _textColour);
            }
        }

        // ============================================================================
        // UPDATE METHOD - No update logic needed for notes
        // Notes are purely visual and don't respond to mouse/keyboard input
        // Their visibility is controlled by the number button click handler in Game1.cs
        // ============================================================================
        public override void Update(GameTime gameTime)
        {
            // No update logic required - notes are static visual elements
        }
    }
}