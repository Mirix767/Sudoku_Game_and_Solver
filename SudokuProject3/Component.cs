using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3
{
    // ============================================================================
    // COMPONENT - Abstract base class for all game components
    // Provides a common interface for drawable and updateable game objects
    // All UI elements, cells, buttons, and visual components inherit from this
    // ============================================================================
    public abstract class Component
    {
        // ============================================================================
        // DRAW - Renders the component to the screen
        // Must be implemented by all derived classes
        // Called every frame during the draw phase
        // ============================================================================
        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        // ============================================================================
        // UPDATE - Updates the component's state
        // Must be implemented by all derived classes
        // Called every frame during the update phase
        // ============================================================================
        public abstract void Update(GameTime gameTime);
    }
}