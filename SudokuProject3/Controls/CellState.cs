// ============================================================================
// USING DIRECTIVES - Required namespaces for CellState class
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuProject3.Controls
{
    // ============================================================================
    // CELLSTATE CLASS - Stores the complete state of a cell for undo/redo functionality
    // Each CellState object represents a snapshot of a cell's state before and after a change
    // These states are stored in stacks (_undoStack and _redoStack in Game1.cs)
    // ============================================================================
    public class CellState
    {
        // ============================================================================
        // CELL LOCATION PROPERTIES
        // Identify which cell this state belongs to
        // ============================================================================
        public int Row { get; set; }      // Row index of the cell (0-8)
        public int Column { get; set; }   // Column index of the cell (0-8)

        // ============================================================================
        // INPUT STATE PROPERTIES
        // Track the cell's numeric input before and after the change
        // ============================================================================
        public int PreviousInput { get; set; }  // The input value before the action (0-9, where 0 = empty)
        public int NewInput { get; set; }       // The input value after the action (0-9, where 0 = empty)

        // ============================================================================
        // NOTES STATE PROPERTIES
        // Track the cell's notes (pencil marks) before and after the change
        // Each cell has a 3x3 grid of notes representing numbers 1-9
        // ============================================================================
        public bool[,] PreviousNotes { get; set; }  // 3x3 array of note visibility states before the action
        public bool[,] NewNotes { get; set; }       // 3x3 array of note visibility states after the action

        // ============================================================================
        // USAGE PATTERN:
        // When user performs an action (enters number, toggles note, uses hint, etc.):
        //    - Create new CellState with Previous values = current state
        //    - Perform the action
        //    - Set New values = updated state
        //    - Push CellState onto _undoStack
        //    - Clear _redoStack (can't redo after new action)
        // ============================================================================
    }
}