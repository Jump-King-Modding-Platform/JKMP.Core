using System;
using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.Logging;
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

        private static readonly float ColWidth = 150;
        private static int selectedBindIndex;
        
        public static IBTnode CreateMenu(List<IDrawable> drawables)
        {
            var controlsMenu = new AdvancedMenuSelector(new GuiFormat
            {
                anchor_bounds = new Rectangle(0, 0, 240, 360),
                anchor = Vector2.Zero,
                element_margin = 8,
                all_padding = 16,
                all_margin = 16
            });
            drawables.Add(controlsMenu);
            
            var bindsMenu = new AdvancedMenuSelector(GuiFormat);
            drawables.Add(bindsMenu);

            AddMenuItems(bindsMenu, drawables);

            controlsMenu.AddChild(null, new TextButton("Keybinds", bindsMenu));
            controlsMenu.AddChild(null, new TextButton("Reset to default", new ResetKeyBinds(() => UpdateAllBinds(bindsMenu))));

            return controlsMenu;
        }

        private static void AddMenuItems(AdvancedMenuSelector menuSelector, List<IDrawable> drawables)
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
                    var menuItem = new ActionBindField(
                        bindings,
                        action,
                        drawables,
                        () => selectedBindIndex,
                        val => selectedBindIndex = val,
                        () => UpdateAllBinds(menuSelector)
                    );
                    menuSelector.AddChild(categoryName, menuItem);
                }
            }
        }

        private static void UpdateAllBinds(AdvancedMenuSelector menuSelector)
        {
            foreach (IBTnode menuItem in menuSelector.Children)
            {
                if (menuItem is not ActionBindField bindField)
                    continue;

                bindField.UpdateKeyBinds();
            }
        }

        private class HeaderField : IBTnode, IMenuItem, UnSelectable
        {
            private static readonly SpriteFont Font = JKContentManager.Font.MenuFont;

            private readonly float col1Offset;
            private readonly float col2Offset;

            public HeaderField()
            {
                col1Offset = 0;
                col2Offset = (float)Math.Round(ColWidth);
            }

            protected override BTresult MyRun(TickData tickData)
            {
                return BTresult.Failure;
            }

            public void Draw(int x, int y, bool selected)
            {
                TextHelper.DrawString(Font, "Action", new Vector2(x + col1Offset, y), Color.White, Vector2.Zero);
                TextHelper.DrawString(Font, "Binds", new Vector2(x + col2Offset, y), Color.White, Vector2.Zero);
            }

            public Point GetSize()
            {
                return new Point((int)ColWidth, Font.LineSpacing);
            }
        }
    }

    internal class ResetKeyBinds : IBTnode
    {
        private readonly Action updateKeyBindsCallback;
        private ModalDialog? dialog;

        public ResetKeyBinds(Action updateKeyBindsCallback)
        {
            this.updateKeyBindsCallback = updateKeyBindsCallback;
        }

        protected override BTresult MyRun(TickData tickData)
        {
            if (dialog == null)
            {
                dialog = ModalDialog.ShowDialog(
                    "Are you sure you want to reset all\nkeybinds to their default values?",
                    onClick: OnModalResult,
                    0,
                    "Reset", "Cancel"
                );
                
                return BTresult.Success;
            }
            
            return dialog!.last_result == BTresult.Running ? BTresult.Running : BTresult.Failure;
        }

        private void OnModalResult(int? selectedOption)
        {
            if (selectedOption == 0)
            {
                InputManager.ResetKeyBinds();
                updateKeyBindsCallback();
            }

            dialog = null;
        }
    }
}