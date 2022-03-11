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
        public Vector2 HorizontalBarOrigin { get; set; }
        public Vector2 VerticalBarOrigin { get; set; }

        public bool ShowVerticalBar { get; set; } = true;
        public bool ShowHorizontalBar { get; set; } = false;

        public Vector2 ScrollPosition
        {
            get => scrollPosition;
            set => scrollPosition = Vector2.Clamp(value, Vector2.Zero, ContentSize - ViewportSize);
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
            
            // Draw the vertical scrollbar
            if (ShowVerticalBar && BarSize.Y < ViewportSize.Y)
            {
                Rectangle drawRect = new(
                    (int)VerticalBarOrigin.X,
                    (int)(VerticalBarOrigin.Y + ScrollPosition.Y),
                    1,
                    (int)BarSize.Y
                );
                
                Game1.spriteBatch.Draw(pixel, drawRect, Color.White);
            }
            
            // Draw the horizontal scrollbar
            if (ShowHorizontalBar && BarSize.X < ViewportSize.X)
            {
                Rectangle drawRect = new(
                    (int)(HorizontalBarOrigin.X + ScrollPosition.X),
                    (int)HorizontalBarOrigin.Y,
                    (int)BarSize.X,
                    1
                );

                Game1.spriteBatch.Draw(pixel, drawRect, Color.White);
            }
        }

        //public Vector2 GetDrawOffset() => new((float)-Math.Round(ContentSize.X - ScrollPosition.X), (float)-Math.Round(ContentSize.Y - ScrollPosition.Y));

        public Vector2 GetDrawOffset()
        {
            return -ScrollPosition;
        }

        private void UpdateProperties()
        {
            Vector2 barSize = Vector2.Zero;
                
            if (ViewportSize.X >= ContentSize.X)
            {
                barSize.X = ViewportSize.X;
            }
            else
            {
                barSize.X = ViewportSize.X * (ViewportSize.X / ContentSize.X);
            }
                
            if (ViewportSize.Y >= ContentSize.Y)
            {
                barSize.Y = ViewportSize.Y;
            }
            else
            {
                barSize.Y = ViewportSize.Y * (ViewportSize.Y / ContentSize.Y);
            }

            BarSize = barSize;

            ScrollPosition = ScrollPosition; // Clamps the scroll position to the new content size
        }
    }
}