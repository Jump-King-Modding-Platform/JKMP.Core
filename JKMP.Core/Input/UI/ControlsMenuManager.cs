using System;
using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.Plugins;
using JKMP.Core.UI;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;
using ActionInfo = JKMP.Core.Input.InputManager.ActionInfo;

namespace JKMP.Core.Input.UI
{
    internal static class ControlsMenuManager
    {
        private static readonly GuiFormat GuiFormat = new()
        {
            anchor_bounds = new Rectangle(0, 0, Game1.WIDTH, Game1.HEIGHT),
            all_margin = 16,
            all_padding = 16,
            element_margin = 2
        };

        private static readonly float FieldWidth = 450;
        
        public static IBTnode CreateMenu(List<IDrawable> drawables)
        {
            var menuSelector = new AdvancedMenuSelector(GuiFormat);

            AddMenuItems(menuSelector);

            drawables.Add(menuSelector);
            return menuSelector;
        }

        private static void AddMenuItems(AdvancedMenuSelector menuSelector)
        {
            IReadOnlyDictionary<Plugin, InputManager.Bindings> allBindings = InputManager.GetAllBindings();

            foreach (var kv in allBindings)
            {
                var plugin = kv.Key;
                var bindings = kv.Value;
                string categoryName = plugin.Info.Name!;

                if (plugin == Plugin.InternalPlugin)
                {
                    categoryName = "Jump King";
                    menuSelector.SetCategoryOrder(categoryName, order: int.MinValue);
                }

                menuSelector.AddChild(categoryName, new HeaderField());
                var actions = bindings.GetActions();
                
                foreach (ActionInfo action in actions)
                {
                    var menuItem = new ActionBindField(bindings, action, FieldWidth);
                    menuSelector.AddChild(categoryName, menuItem);
                }
            }
        }

        private class HeaderField : IBTnode, IMenuItem, UnSelectable
        {
            private static readonly SpriteFont Font = JKContentManager.Font.MenuFont;

            private readonly float col1Offset;
            private readonly float col2Offset;
            private readonly float col3Offset;

            public HeaderField()
            {
                col1Offset = 0;
                col2Offset = (float)Math.Round(FieldWidth * 0.33f);
                col3Offset = (float)Math.Round(FieldWidth * 0.66f);
            }

            protected override BTresult MyRun(TickData tickData)
            {
                return BTresult.Failure;
            }

            public void Draw(int x, int y, bool selected)
            {
                TextHelper.DrawString(Font, "Action", new Vector2(x + col1Offset, y), Color.White, Vector2.Zero);
                TextHelper.DrawString(Font, "Primary key", new Vector2(x + col2Offset, y), Color.White, Vector2.Zero);
                TextHelper.DrawString(Font, "Secondary key", new Vector2(x + col3Offset, y), Color.White, Vector2.Zero);
            }

            public Point GetSize()
            {
                var textSize = Font.MeasureString("|").ToPoint();
                return new Point((int)FieldWidth, textSize.Y);
            }
        }
    }
}