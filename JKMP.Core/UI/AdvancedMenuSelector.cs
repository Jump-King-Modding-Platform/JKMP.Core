using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
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

        public virtual void AddChild<T>(T menuItem) where T : IBTnode, IMenuItem
        {
            base.AddChild(menuItem);
            Invalidate();
        }
        
        public virtual void AddChild(IMenuItem menuItem)
        {
            if (menuItem is not IBTnode item)
            {
                throw new ArgumentException("Menu item must inherit IBTnode", nameof(menuItem));
            }

            base.AddChild(item);
            Invalidate();
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
            guiFormat.DrawMenuItemsWithCursor(allItems, guiFormat.CalculateBounds(allItems), hoverItemIndex);
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

            var hoverResult = Children[HoverItemIndex].Run(tickData);

            if (hoverResult != BTresult.Failure && activeItem?.last_result != BTresult.Running)
            {
                activeItem = Children[HoverItemIndex];
                JKContentManager.Audio.Menu.OnSelect();
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
            var result = new Rectangle();

            var bounds = guiFormat.anchor_bounds;
            var margin = guiFormat.margin;
            
            result.X = bounds.X + margin.left;
            result.Y = bounds.Y + margin.bottom;
            result.Width = bounds.Width - margin.left - margin.right;
            result.Height = bounds.Height - margin.top - margin.bottom;
            
            return result;
        }

        protected virtual void OnDirty()
        {
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