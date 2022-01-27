using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using HarmonyLib;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using Microsoft.Xna.Framework;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    public class AdvancedMenuSelector : IBTcomposite, IDrawable
    {
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

        public GuiFormat GuiFormat => guiFormat;

        private readonly Dictionary<string, List<IMenuItem>> categories = new();
        private readonly List<IMenuItem> uncategorizedItems = new();
        private readonly Dictionary<string, string> categoryNames = new();
        private readonly Dictionary<string, int> categoryOrder = new();
        private readonly GuiFrame bgFrame;
        private readonly bool autoSize;

        private bool dirty = true;
        private IMenuItem[]? allItems;
        private int hoverItemIndex;
        private IBTnode? activeItem;
        private GuiFormat guiFormat;

        public AdvancedMenuSelector(GuiFormat guiFormat, bool autoSize = false)
        {
            this.autoSize = autoSize;
            this.guiFormat = guiFormat;
            bgFrame = new GuiFrame(GetBackgroundRectangle());
        }

        public IEnumerable<IMenuItem> GetMenuItems() => Children.Cast<IMenuItem>();

        public void AddChild<T>(string category, T menuItem) where T : IBTnode, IMenuItem
        {
            AddChild(category, (IMenuItem)menuItem);
        }
        
        public void AddChild(string? category, IMenuItem menuItem)
        {
            if (menuItem is not IBTnode)
            {
                throw new ArgumentException("Menu item must inherit IBTnode", nameof(menuItem));
            }

            string? lowerCategory = category?.ToLowerInvariant();

            if (lowerCategory == null)
            {
                uncategorizedItems.Add(menuItem);
            }
            else
            {
                if (!categoryNames.ContainsKey(lowerCategory))
                {
                    categoryNames.Add(lowerCategory, category!);
                }

                if (!categories.TryGetValue(lowerCategory, out var menuItems))
                {
                    menuItems = new List<IMenuItem>();
                    categories.Add(lowerCategory, menuItems);
                    categoryOrder[lowerCategory] = 0;
                }

                menuItems.Add(menuItem);
            }

            Invalidate();
        }

        public new void AddChild(IBTnode node)
        {
            if (node is not IMenuItem item)
                throw new ArgumentException("Node must implement IMenuItem", nameof(node));
            
            AddChild(null, item);
        }

        public void SetCategoryOrder(string category, int order)
        {
            categoryOrder[category.ToLowerInvariant()] = order;
        }

        public void Invalidate()
        {
            dirty = true;
        }

        public virtual void Draw()
        {
            if (last_result != BTresult.Running)
                return;
            
            bgFrame.Draw();
            DrawMenuItems();
        }

        private void DrawMenuItems()
        {
            if (allItems == null)
                return;

            Vector2 drawPos = guiFormat.CalculateBounds(allItems).Location.ToVector2();
            drawPos.Y += guiFormat.padding.top;

            for (int i = 0; i < allItems.Length; i++)
            {
                float x = drawPos.X + guiFormat.padding.left;
                float y = drawPos.Y;

                if (i == hoverItemIndex && allItems[i] is not UnSelectable)
                {
                    Game1.spriteBatch.Draw(JKContentManager.GUI.Cursor, drawPos, Color.White);
                    x += 5;
                }

                allItems[i].Draw((int)x, (int)y, hoverItemIndex == i);

                drawPos.Y += guiFormat.element_margin + allItems[i].GetSize().Y;
            }
        }

        protected override BTresult MyRun(TickData tickData)
        {
            if (dirty)
            {
                OnDirty();
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

            if (Children.Length > 0)
            {
                var hoverResult = Children[HoverItemIndex].Run(tickData);

                if (hoverResult != BTresult.Failure && activeItem?.last_result != BTresult.Running)
                {
                    activeItem = Children[HoverItemIndex];
                    JKContentManager.Audio.Menu.OnSelect();
                }
            }

            return BTresult.Running;
        }

        protected override void OnNewRun()
        {
            Reset();
        }

        protected override void ResumeRun()
        {
            Reset();
        }

        protected virtual void Reset()
        {
            ResetHoverIndex();
            activeItem = null;
            ControllerManager.instance.MenuController.ConsumePadPresses();
        }

        private Rectangle GetBackgroundRectangle()
        {
            return guiFormat.GetBounds();
        }

        protected virtual void OnDirty()
        {
            var newChildren = new List<IBTnode>();

            // Add categorized items, order by custom order then alphabetical
            var orderedCategories = categories.OrderBy(kv => categoryOrder[kv.Key]).ThenBy(kv => kv.Key);
            foreach (KeyValuePair<string, List<IMenuItem>> kv in orderedCategories)
            {
                // The original name of the category which has casing kept intact
                string displayName = categoryNames[kv.Key];

                newChildren.Add(new TextInfo(displayName, Color.White));

                foreach (IMenuItem menuItem in kv.Value)
                {
                    newChildren.Add((IBTnode)menuItem);
                }
            }
            
            // Add uncategorized items
            foreach (IMenuItem item in uncategorizedItems)
            {
                newChildren.Add((IBTnode)item);
            }

            // Set children
            Traverse.Create(this).Field<IBTnode[]>("m_children").Value = newChildren.ToArray();
            
            activeItem = null;
            allItems = GetMenuItems().ToArray();
            bgFrame.SetBounds(autoSize ? guiFormat.CalculateBounds(allItems) : GetBackgroundRectangle());

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