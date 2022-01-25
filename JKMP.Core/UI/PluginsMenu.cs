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
    public class PluginsMenu : IBTcomposite, IDrawable
    {
        private const int Width = Game1.WIDTH;
        private const int Height = Game1.HEIGHT;

        private static GuiFormat pluginsGuiFormat = new()
        {
            margin = new GuiSpacing { left = 16, right = 8, top = 16, bottom = 16 },
            padding = new GuiSpacing { left = 16, right = 16, top = 16, bottom = 16 },
            anchor = Vector2.Zero,
            element_margin = 8,
            all_padding = 16,
            anchor_bounds = new Rectangle(0, 0, Width / 2, Height),
        };

        public static readonly GuiFormat PluginOptionsGuiFormat = new()
        {
            margin = new GuiSpacing { left = 8, right = 16, top = 16, bottom = 16 },
            padding = new GuiSpacing { left = 16, right = 16, top = 16, bottom = 16 },
            anchor = Vector2.Zero,
            element_margin = 8,
            all_padding = 16,
            anchor_bounds = new Rectangle(Width / 2, 0, Width / 2, Height),
        };

        public int HoverItemIndex
        {
            get => hoverItemIndex;
            set
            {
                hoverItemIndex = value;

                if (hoverItemIndex < 0)
                {
                    hoverItemIndex = 0;
                }
                else if (hoverItemIndex >= Children.Length)
                {
                    hoverItemIndex = Children.Length - 1;
                }
            }
        }

        private readonly GuiFrame bgFrame;
        private readonly List<IBTnode> pluginMenus = new();
        private readonly PluginLoadOrderMenu pluginLoadOrderMenu;
        
        private bool dirty;
        private IMenuItem[]? allItems;
        private int hoverItemIndex;
        private IBTnode? activeItem;

        public PluginsMenu()
        {
            bgFrame = new GuiFrame(GetBackgroundRectangle());
            pluginLoadOrderMenu = new();
        }

        public IEnumerable<IMenuItem> GetMenuItems() => Children.Cast<IMenuItem>();

        protected override BTresult MyRun(TickData tickData)
        {
            if (dirty)
            {
                RefreshPrivates();
            }

            var padState = ControllerManager.instance.MenuController.GetPadState();
            bool hadActiveItem = false;
            
            if (activeItem != null)
            {
                hadActiveItem = true;
                BTresult result = activeItem.Run(tickData);

                if (result == BTresult.Running)
                    return BTresult.Running;
                
                if (result == BTresult.Failure || result == BTresult.Success)
                {
                    activeItem = null;
                }
            }

            if (padState.cancel || padState.pause)
            {
                ControllerManager.instance.MenuController.ConsumePadPresses();

                if (hadActiveItem)
                {
                    activeItem = null;
                    return BTresult.Running;
                }

                return BTresult.Failure;
            }

            if (padState.down)
            {
                int newIndex = HoverItemIndex + 1;

                if (newIndex < Children.Length)
                {
                    while (Children[newIndex] is UnSelectable && newIndex < Children.Length - 1)
                    {
                        ++newIndex;
                    }

                    if (Children[newIndex] is not UnSelectable)
                        HoverItemIndex = newIndex;
                }
            }

            if (padState.up)
            {
                int newIndex = HoverItemIndex - 1;

                if (newIndex >= 0)
                {
                    while (Children[newIndex] is UnSelectable && newIndex > 0)
                    {
                        --newIndex;
                    }

                    if (Children[newIndex] is not UnSelectable)
                        HoverItemIndex = newIndex;
                }
            }

            var hoverResult = Children[HoverItemIndex].Run(tickData);

            if (hoverResult != BTresult.Failure && activeItem?.last_result != BTresult.Running)
            {
                activeItem = Children[HoverItemIndex];
                JKContentManager.Audio.Menu.OnSelect();
            }

            return BTresult.Running;
        }

        public void Draw()
        {
            if (last_result != BTresult.Running)
                return;
            
            bgFrame.Draw();
            string bottomMessage = "Press escape or backspace to close the menu";
            var textSize = JKContentManager.Font.MenuFontSmall.MeasureString(bottomMessage);
            TextHelper.DrawString(JKContentManager.Font.MenuFontSmall, bottomMessage, new Vector2(Width / 2, Height - 40), Color.White,
                new Vector2(0.5f, 0));
            
            pluginsGuiFormat.DrawMenuItemsWithCursor(allItems, pluginsGuiFormat.CalculateBounds(allItems), hoverItemIndex);
        }

        public void AddChild<T>(T menuItem) where T : IBTnode, IMenuItem
        {
            pluginMenus.Add(menuItem);
            dirty = true;
        }
        
        public void AddChild(IMenuItem menuItem)
        {
            if (menuItem is not IBTnode item)
            {
                throw new ArgumentException("Menu item must inherit IBTnode", nameof(menuItem));
            }

            pluginMenus.Add(item);
            dirty = true;
        }

        protected override void OnNewRun()
        {
            Reset();
        }

        protected override void ResumeRun()
        {
            Reset();
        }

        private void Reset()
        {
            ResetHoverIndex();
            activeItem = null;
            ControllerManager.instance.MenuController.ConsumePadPresses();
        }

        private Rectangle GetBackgroundRectangle()
        {
            var result = new Rectangle();

            var bounds = pluginsGuiFormat.anchor_bounds;
            var margin = pluginsGuiFormat.margin;
            
            result.X = bounds.X + margin.left;
            result.Y = bounds.Y + margin.bottom;
            result.Width = bounds.Width - margin.left - margin.right;
            result.Height = bounds.Height - margin.top - margin.bottom;
            
            return result;
        }

        private void RefreshPrivates()
        {
            bgFrame.SetBounds(GetBackgroundRectangle());

            var newChildren = new List<IBTnode>();

            // Add plugins title
            newChildren.Add(new TextInfo("Plugins", Color.White));
            
            // Add plugin menus
            foreach (var menu in pluginMenus)
            {
                newChildren.Add(menu);
            }

            // Add core options
            newChildren.Add(new TextInfo("Core options", Color.White));
            newChildren.Add(new TextButton("Load order", pluginLoadOrderMenu, JKContentManager.Font.MenuFontSmall, Color.LightGray));

            // Set children
            Traverse.Create(this).Field<IBTnode[]>("m_children").Value = newChildren.ToArray();

            activeItem = null;
            allItems = GetMenuItems().ToArray();

            ResetHoverIndex();
            
            dirty = false;
        }

        protected void ResetHoverIndex()
        {
            for (int i = 0; i < Children.Length; ++i)
            {
                if (Children[i] is not UnSelectable)
                {
                    HoverItemIndex = i;
                    break;
                }
            }
        }
    }
}