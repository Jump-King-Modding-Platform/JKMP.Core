using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KeyBind = JKMP.Core.Input.InputManager.KeyBind;

namespace JKMP.Core.Input.UI
{
    internal class ActionBindField : IBTnode, IMenuItem
    {
        private readonly InputManager.Bindings bindings;
        private readonly InputManager.ActionInfo action;

        private readonly float fieldWidth;
        private readonly float xNameOffset;
        private readonly float xBind1Offset;
        private readonly float xBind2Offset;

        private static SpriteFont Font => JKContentManager.Font.MenuFontSmall;
        private static IReadOnlyList<KeyBind> keyBinds = null!;
        private KeyBind bind1;
        private KeyBind bind2;

        public ActionBindField(InputManager.Bindings bindings, InputManager.ActionInfo action, float fieldWidth)
        {
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            this.action = action;
            this.fieldWidth = fieldWidth;
            xNameOffset = 0;
            xBind1Offset = (float)Math.Round(fieldWidth * 0.33f);
            xBind2Offset = (float)Math.Round(fieldWidth * 0.66f);
            UpdateKeyBinds();
        }

        private void UpdateKeyBinds()
        {
            keyBinds = bindings.GetKeyBindsForAction(action.Name);
            bind1 = keyBinds.FirstOrDefault();
            bind2 = keyBinds.Skip(1).FirstOrDefault();
        }

        protected override BTresult MyRun(TickData tickData)
        {
            return BTresult.Failure;
        }

        public void Draw(int x, int y, bool selected)
        {
            Color drawColor = selected ? Color.White : Color.LightGray;
            
            // Draw action name
            TextHelper.DrawString(Font, action.UiName, new Vector2(x + xNameOffset, y), drawColor, Vector2.Zero);
            
            // Draw primary binding
            if (bind1.IsValid)
            {
                TextHelper.DrawString(Font, bind1.ToDisplayString(), new Vector2(x + xBind1Offset, y), drawColor, Vector2.Zero);
            }

            // Draw secondary binding
            if (bind2.IsValid)
            {
                TextHelper.DrawString(Font, bind2.ToDisplayString(), new Vector2(x + xBind2Offset, y), drawColor, Vector2.Zero);
            }
        }

        public Point GetSize()
        {
            return Font.MeasureString(action.UiName).ToPoint();
        }
    }
}