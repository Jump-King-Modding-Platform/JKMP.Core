using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using HarmonyLib;
using JKMP.Core.Logging;
using JumpKing;
using JumpKing.Controller;
using JumpKing.GameManager;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    internal class PluginsMenu : AdvancedMenuSelector
    {
        public static readonly GuiFormat PluginOptionsGuiFormat = new()
        {
            margin = new GuiSpacing { left = 8, right = 16, top = 16, bottom = 16 },
            all_padding = 16,
            anchor = Vector2.Zero,
            element_margin = 8,
            anchor_bounds = new Rectangle(Width / 2, 0, Width / 2, Height),
        };
        
        private const int Width = Game1.WIDTH;
        private const int Height = Game1.HEIGHT;

        private static readonly GuiFormat PluginsGuiFormat = new()
        {
            margin = new GuiSpacing { left = 16, right = 8, top = 16, bottom = 16 },
            all_padding = 16,
            anchor = Vector2.Zero,
            element_margin = 8,
            anchor_bounds = new Rectangle(0, 0, Width / 2, Height),
        };

        public PluginsMenu(List<IDrawable> drawables) : base(PluginsGuiFormat, autoSize: false)
        {
            drawables.Add(this);
        }

        public override void Draw()
        {
            base.Draw();

            if (last_result != BTresult.Running)
                return;
            
            string closeMessage = "Press escape or backspace to close the menu";
            TextHelper.DrawString(JKContentManager.Font.MenuFontSmall, closeMessage, new Vector2(Width / 2, Height - 30), Color.White,
                new Vector2(0.5f, 1));
        }
    }
}