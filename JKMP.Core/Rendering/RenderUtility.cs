using System;
using System.Collections.Generic;
using JumpKing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.Rendering
{
    /// <summary>
    /// Provides some utility functions for rendering.
    /// </summary>
    public static class RenderUtility
    {
        private static readonly Stack<(SpriteBatch spriteBatch, RenderTargetBinding[], Rectangle? scissorRectangle)> RenderContextStack = new();

        /// <summary>
        /// Stores the current render context and updates the current one.
        /// The previous one can be restored by calling <see cref="PopRenderContext"/>.
        /// </summary>
        /// <param name="spriteBatch">The new spritebatch to use.</param>
        /// <param name="renderTarget">The render target to render to.</param>
        /// <param name="scissorRectangle">
        /// The scissor rectangle to use. Note that scissoring needs to be done by the caller.
        /// This method only saves the current rectangle and sets the new one.
        /// </param>
        public static void PushRenderContext(SpriteBatch spriteBatch, RenderTarget2D renderTarget, Rectangle? scissorRectangle = null)
        {
            if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));
            if (renderTarget == null) throw new ArgumentNullException(nameof(renderTarget));

            // Store the current render target and sprite batch
            RenderContextStack.Push((Game1.spriteBatch, Game1.instance.GraphicsDevice.GetRenderTargets(), scissorRectangle));

            // Set the new render target and sprite batch
            Game1.instance.GraphicsDevice.SetRenderTarget(renderTarget);

            if (scissorRectangle != null)
                Game1.instance.GraphicsDevice.ScissorRectangle = scissorRectangle.Value;

            Game1.spriteBatch = spriteBatch;
        }

        /// <summary>
        /// Restores the previous render context.
        /// If there is no previous render context, an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there are no pushed render contexts.</exception>
        public static void PopRenderContext()
        {
            if (RenderContextStack.Count == 0)
            {
                throw new InvalidOperationException("Render context stack is empty");
            }

            // Restore the previous values
            var (spriteBatch, renderTargets, scissorRectangle) = RenderContextStack.Pop();
            Game1.instance.GraphicsDevice.SetRenderTargets(renderTargets);

            if (scissorRectangle != null)
                Game1.instance.GraphicsDevice.ScissorRectangle = scissorRectangle.Value;

            Game1.spriteBatch = spriteBatch;
        }
    }
}