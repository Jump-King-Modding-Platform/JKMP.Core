using System;
using JKMP.Core.Logging;
using JumpKing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    internal class ScrollBar : IDrawable
    {
        public bool ShowVerticalBar { get; set; } = true;
        public bool ShowHorizontalBar { get; set; } = false;

        public Vector2 ScrollPosition
        {
            get => scrollPosition;
            set => scrollPosition = Vector2.Clamp(value, Vector2.Zero, ContentSize - ViewportSize);
        }
        
        public Rectangle HorizontalBar
        {
            get => horizontalBar;
            set
            {
                horizontalBar = value;
                UpdateProperties();
            }
        }

        public Rectangle VerticalBar
        {
            get => verticalBar;
            set
            {
                verticalBar = value;
                UpdateProperties();
            }
        }

        public Vector2 ViewportSize
        {
            get => viewportSize;
            set
            {
                viewportSize = value;
                UpdateProperties();
            }
        }

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
        
        public ScrollBar(Vector2 startPosition, Vector2 viewportSize, Vector2 contentSize)
        {
            ScrollPosition = startPosition;
            ViewportSize = viewportSize;
            ContentSize = contentSize;
            UpdateProperties();
        }

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