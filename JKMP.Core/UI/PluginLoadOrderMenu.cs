using BehaviorTree;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    internal class PluginLoadOrderMenu : AdvancedMenuSelector
    {
        private static readonly GuiFormat GuiFormat = new()
        {
            anchor_bounds = new Rectangle(0, 0, Game1.WIDTH, Game1.HEIGHT),
            all_margin = 16,
            all_padding = 16,
            element_margin = 8
        };

        public PluginLoadOrderMenu() : base(GuiFormat)
        {
        }

        public override void Draw()
        {
            base.Draw();
            
            if (last_result != BTresult.Running)
                return;

            var drawLocation = GuiFormat.GetBounds().Center.ToVector2();
            TextHelper.DrawString(JKContentManager.Font.MenuFont,
                "This menu is not implemented yet",
                drawLocation,
                Color.White,
                new Vector2(0.5f, 0.5f)
            );
            TextHelper.DrawString(JKContentManager.Font.MenuFontSmall,
                "Press escape or backspace to close this menu",
                drawLocation + new Vector2(0, 16),
                Color.LightGray,
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}