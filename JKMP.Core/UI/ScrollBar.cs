using System;
using JKMP.Core.Logging;
using JumpKing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    /// <summary>
    /// A scrollbar that can be used to visualize how much content is visible relative to the viewport size.
    /// A scrollbar shows both a vertical and horizontal scrollbar (horizontal is hidden by default).
    /// </summary>
    public class ScrollBar : IDrawable
    {
        /// <summary>
        /// If true the vertical scrollbar is drawn.
        /// </summary>
        public bool ShowVerticalBar { get; set; } = true;
        
        /// <summary>
        /// If true the horizontal scrollbar is drawn.
        /// </summary>
        public bool ShowHorizontalBar { get; set; } = false;

        /// <summary>
        /// The position of the viewport relative to the content in pixels.
        /// </summary>
        public Vector2 ScrollPosition
        {
            get => scrollPosition;
            set => scrollPosition = Vector2.Clamp(value, Vector2.Zero, ContentSize - ViewportSize);
        }
        
        /// <summary>
        /// The position and size of the horizontal scrollbar.
        /// </summary>
        public Rectangle HorizontalBar
        {
            get => horizontalBar;
            set
            {
                horizontalBar = value;
                UpdateProperties();
            }
        }

        /// <summary>
        /// The position and size of the vertical scrollbar.
        /// </summary>
        public Rectangle VerticalBar
        {
            get => verticalBar;
            set
            {
                verticalBar = value;
                UpdateProperties();
            }
        }

        /// <summary>
        /// The size of the viewport in pixels.
        /// </summary>
        public Vector2 ViewportSize
        {
            get => viewportSize;
            set
            {
                viewportSize = value;
                UpdateProperties();
            }
        }

        /// <summary>
        /// The size of the content in pixels.
        /// </summary>
        public Vector2 ContentSize
        {
            get => contentSize;
            set
            {
                contentSize = value;
                UpdateProperties();
            }
        }
        
        private Vector2 viewportSize;
        private Vector2 contentSize;
        private Vector2 scrollPosition;
        private Rectangle verticalBar;
        private Rectangle horizontalBar;

        /// <summary>
        /// The size of the scrollbar.
        /// The X value indicates the width of the horizontal scrollbar,
        /// and the Y value indicates the height of the vertical scrollbar.
        /// </summary>
        public Vector2 BarSize { get; private set; }
        
        /// <summary>
        /// Instantiates a new scrollbar.
        /// </summary>
        /// <param name="startPosition">The initial ScrollPosition. Note that it is clamped to a value relating to viewport and content size.</param>
        /// <param name="viewportSize">The size of the viewport.</param>
        /// <param name="contentSize">The size of the content. This can also be updated at any point using the <see cref="ContentSize"/> property.</param>
        public ScrollBar(Vector2 startPosition, Vector2 viewportSize, Vector2 contentSize)
        {
            ContentSize = contentSize;
            ViewportSize = viewportSize;
            ScrollPosition = startPosition;
            UpdateProperties();
        }

        /// <inheritdoc />
        public void Draw()
        {
            Texture2D pixel = JKContentManager.Pixel.texture;
            Vector2 barOffset = ScrollPosition * (ViewportSize / ContentSize);

            // Draw the vertical scrollbar
            if (ShowVerticalBar && BarSize.Y < ViewportSize.Y)
            {
                // Draw background
                Game1.spriteBatch.Draw(pixel, VerticalBar, Color.Gray);

                Rectangle drawRect = new(
                    VerticalBar.X,
                    (int)barOffset.Y + VerticalBar.Y,
                    VerticalBar.Width,
                    (int)Math.Ceiling(BarSize.Y)
                );

                Game1.spriteBatch.Draw(pixel, drawRect, Color.White);
            }

            // Draw the horizontal scrollbar
            if (ShowHorizontalBar && BarSize.X < ViewportSize.X)
            {
                Rectangle drawRect = new(
                    (int)barOffset.X + VerticalBar.X,
                    HorizontalBar.Y,
                    (int)Math.Ceiling(BarSize.X),
                    HorizontalBar.Height
                );

                Game1.spriteBatch.Draw(pixel, drawRect, Color.White);
            }
        }

        /// <summary>
        /// Returns the draw offset to add when drawing the contents.
        /// </summary>
        public Vector2 GetDrawOffset() => -ScrollPosition;

        private void UpdateProperties()
        {
            Vector2 barSize = Vector2.Zero;
                
            if (ViewportSize.X >= ContentSize.X)
            {
                barSize.X = HorizontalBar.Width;
            }
            else
            {
                barSize.X = HorizontalBar.Width * (ViewportSize.X / ContentSize.X);
            }
                
            if (ViewportSize.Y >= ContentSize.Y)
            {
                barSize.Y = VerticalBar.Height;
            }
            else
            {
                barSize.Y = VerticalBar.Height * (ViewportSize.Y / ContentSize.Y);
            }

            BarSize = barSize;
            ScrollPosition = ScrollPosition; // Clamps the scroll position to the new content size
        }
    }
}