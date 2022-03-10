using System;
using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.Logging;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    public partial class ModalDialog : IBTnode, IDrawable
    {
        public string Message { get; }
        public string[] Buttons { get; }
        public Action<int?>? Callback { get; }

        private SpriteFont Font => JKContentManager.Font.MenuFont;
        private SpriteFont SmallFont => JKContentManager.Font.MenuFontSmall;

        private readonly GuiFrame bgFrame;
        private readonly Rectangle rect;

        private const int PaddingX = 30;
        private const int PaddingY = 50;
        
        int selectedButton = 0;

        internal ModalDialog(string message, string[] buttons, Action<int?>? callback)
        {
            if (buttons.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(buttons));
            
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Buttons = buttons;
            Callback = callback;

            Vector2 sizeOfMessage = SmallFont.MeasureString(message);

            int width = (int)(sizeOfMessage.X + PaddingX * 2);
            int height = (int)(sizeOfMessage.Y + PaddingY * 2);
            int x = (int)Math.Round(Game1.WIDTH / 2f - width / 2f);
            int y = (int)Math.Round(Game1.HEIGHT / 2f - height / 2f);

            rect = new Rectangle(x, y, width, height);
            bgFrame = new(rect);
        }

        /// <inheritdoc />
        protected override BTresult MyRun(TickData tickData)
        {
            var input = ControllerManager.instance.MenuController.GetPadState();
            ControllerManager.instance.MenuController.ConsumePadPresses(); // Always consume the pad presses

            if (input.cancel || input.pause)
            {
                Callback?.Invoke(null);
                return BTresult.Failure;
            }

            if (input.left)
            {
                selectedButton = MathHelper.Clamp(selectedButton - 1, 0, Buttons.Length - 1);
            }

            if (input.right)
            {
                selectedButton = MathHelper.Clamp(selectedButton + 1, 0, Buttons.Length - 1);
            }

            if (input.confirm || input.jump)
            {
                Callback?.Invoke(selectedButton);
                JKContentManager.Audio.Menu.OnSelect();
                return BTresult.Success;
            }
            
            return BTresult.Running;
        }

        /// <inheritdoc />
        public void Draw()
        {
            bgFrame.Draw();

            // Draw message
            TextHelper.DrawString(SmallFont, Message, rect.Center.ToVector2(), Color.White, new Vector2(0.5f, 0.5f));
            
            // Draw buttons
            Vector2 position = new Vector2(rect.Left + 14, rect.Bottom - 14 - Font.LineSpacing);

            for (int i = 0; i < Buttons.Length; ++i)
            {
                bool selected = selectedButton == i;

                TextHelper.DrawString(Font, Buttons[i], position, selected ? Color.White : Color.LightGray, Vector2.Zero);

                if (selected)
                {
                    // Draw underline
                    Vector2 size = Font.MeasureString(Buttons[i]);
                    Rectangle underlineRect = new Rectangle((int)position.X, (int)position.Y + Font.LineSpacing, (int)size.X, 1);
                    JKContentManager.Pixel.sprite.Draw(underlineRect);
                }

                position.X += Font.MeasureString(Buttons[i]).X + 8;
            }
        }
    }
}