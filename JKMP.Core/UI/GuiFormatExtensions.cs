using JumpKing.PauseMenu;
using Microsoft.Xna.Framework;

namespace JKMP.Core.UI
{
    /// <summary>
    /// Methods that extend <see cref="GuiFormat"/>.
    /// </summary>
    public static class GuiFormatExtensions
    {
        /// <summary>
        /// Returns the fixed bounds of the given <see cref="GuiFormat"/>, accounting for margin.
        /// </summary>
        public static Rectangle GetBounds(this GuiFormat guiFormat)
        {
            var bounds = guiFormat.anchor_bounds;
            var margin = guiFormat.margin;
            var result = new Rectangle
            {
                X = bounds.X + margin.left,
                Y = bounds.Y + margin.bottom,
                Width = bounds.Width - margin.left - margin.right,
                Height = bounds.Height - margin.top - margin.bottom
            };

            return result;
        }
    }
}