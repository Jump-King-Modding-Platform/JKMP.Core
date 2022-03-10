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
    /// <summary>
    /// <para>
    /// A UI element that displays a message and a group of buttons that can be clicked to select an option.
    /// When opened it takes over the input and the game is paused until the user selects an option.
    /// </para>
    ///
    /// <para>
    /// The last_result property is set to Success if an option was selected or Failure if the window was closed by pressing the cancel/back buttons.
    /// While waiting for an answer, the last_result property is set to Running.
    /// </para>
    /// </summary>
    public partial class ModalDialog : IBTnode, IDrawable
    {
        /// <summary>
        /// The message to display.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// The buttons to display.
        /// </summary>
        public string[] Buttons { get; }
        
        /// <summary>
        /// The callback to invoke when the user selects an option.
        /// </summary>
        public Action<int?>? Callback { get; }
        
        public int? DialogResult { get; private set; }

        private static SpriteFont Font => JKContentManager.Font.MenuFont;
        private static SpriteFont SmallFont => JKContentManager.Font.MenuFontSmall;

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
                DialogResult = null;
                Callback?.Invoke(null);
                return BTresult.Failure;
            }

            if (input.left)
            {
                selectedButton--;

                if (selectedButton < 0)
                    selectedButton = Buttons.Length - 1;
            }

            if (input.right)
            {
                selectedButton++;
                
                if (selectedButton >= Buttons.Length)
                    selectedButton = 0;
            }

            if (input.confirm || input.jump)
            {
                DialogResult = selectedButton;
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