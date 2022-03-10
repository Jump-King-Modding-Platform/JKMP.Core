using System.Linq;
using BehaviorTree;
using JKMP.Core.Logging;
using JKMP.Core.UI;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.Input.UI
{
    internal class RebindWindow : IBTnode, IDrawable
    {
        public InputManager.ActionInfo Action { get; }
        public InputManager.KeyBind CurrentBind { get; private set; }

        private string priority;
        private bool active = false;
        private float escapeHoldTime;
        private float unbindHoldTime;
        private InputManager.KeyBind lastPressedKey;
        
        private static readonly GuiFrame BgFrame;
        private static readonly InputManager.KeyBind CancelKey = new("escape");
        private static readonly InputManager.KeyBind UnbindKey = new("backspace");
        private static readonly SpriteFont MenuFont = JKContentManager.Font.MenuFont;
        private static readonly SpriteFont SmallFont = JKContentManager.Font.MenuFontSmall;

        static RebindWindow()
        {
            BgFrame = new(new(Game1.WIDTH / 2 - BindingFrameWidth / 2, Game1.HEIGHT / 2 - BindingFrameHeight / 2, BindingFrameWidth, BindingFrameHeight));
        }

        public RebindWindow(InputManager.ActionInfo action)
        {
            Action = action;
            priority = string.Empty;
        }

        private const int BindingFrameWidth = 350;
        private const int BindingFrameHeight = 150;
        private const float CancelTime = 1f;

        /// <param name="keyBind">The current keybind for this action</param>
        /// <param name="priority">The priority of this keybind, for example "primary" or "secondary"</param>
        public void Show(InputManager.KeyBind keyBind, string priority)
        {
            CurrentBind = keyBind;
            active = true;
            this.priority = priority;
        }

        public void Hide()
        {
            active = false;
            escapeHoldTime = 0;
            unbindHoldTime = 0;
            lastPressedKey = default;
        }

        protected override BTresult MyRun(TickData tickData)
        {
            if (!active)
                return BTresult.Failure;
            
            if (InputManager.IsKeyDown(CancelKey))
            {
                escapeHoldTime += tickData.delta_time;

                if (escapeHoldTime >= CancelTime)
                {
                    Hide();
                    return BTresult.Failure;
                }
            }

            if (InputManager.IsKeyDown(UnbindKey))
            {
                unbindHoldTime += tickData.delta_time;

                if (unbindHoldTime >= CancelTime)
                {
                    CurrentBind = default;
                    Hide();
                    return BTresult.Success;
                }
            }

            InputManager.KeyBind releasedKey = InputManager.GetReleasedKeys().FirstOrDefault();

            if (releasedKey.IsValid && lastPressedKey == releasedKey)
            {
                CurrentBind = releasedKey;
                Hide();
                return BTresult.Success;
            }
            
            var pressedKey = InputManager.GetPressedKeys().FirstOrDefault();

            if (pressedKey.IsValid)
                lastPressedKey = pressedKey;

            return BTresult.Running;
        }

        public void Draw()
        {
            if (last_result != BTresult.Running)
                return;

            BgFrame.Draw();

            const int padding = 16;

            Vector2 position = BgFrame.GetBounds().Center.ToVector2();
            position.Y = BgFrame.GetBounds().Y + padding;
            
            // Draw "window" title
            TextHelper.DrawString(MenuFont, $"Binding {priority} {Action.UiName} key", position, Color.White, new Vector2(0.5f, 0));
            position.Y += MenuFont.LineSpacing;
            
            // Draw cancel text
            TextHelper.DrawString(SmallFont, $"Hold escape for {CancelTime - escapeHoldTime:0.#} second(s) to cancel", position, Color.White, new Vector2(0.5f, 0));
            position.Y += SmallFont.LineSpacing;
            
            // Draw unbind text
            TextHelper.DrawString(SmallFont, $"Hold backspace for {CancelTime - unbindHoldTime:0.#} second(s) to unbind", position, Color.White, new Vector2(0.5f, 0));
            position.Y += SmallFont.LineSpacing;

            position.X = BgFrame.GetBounds().Left + padding;
            position.Y += 8;
            
            // Draw current keybind
            TextHelper.DrawString(MenuFont, "Current bind", position, Color.White, Vector2.Zero);
            position.Y += MenuFont.LineSpacing;

            TextHelper.DrawString(SmallFont, CurrentBind.IsValid ? CurrentBind.ToDisplayString() : "(unbound)", position, Color.LightGray, Vector2.Zero);
            position.Y += SmallFont.LineSpacing;
        }
    }
}