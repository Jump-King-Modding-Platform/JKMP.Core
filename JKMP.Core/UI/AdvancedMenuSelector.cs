using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BehaviorTree;
using HarmonyLib;
using JKMP.Core.Rendering;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.UI
{
    /// <summary>
    /// A class that handles drawing a menu. It is very similar to the <see cref="MenuSelector"/> except it had some more functionality such as categories.
    /// </summary>
    public class AdvancedMenuSelector : IBTcomposite, IDrawable
    {
        /// <summary>
        /// The index of the currently selected item.
        /// When set the value is clamped to the range of the menu items.
        /// </summary>
        public int SelectedItemIndex
        {
            get => selectedItemIndex;
            set
            {
                selectedItemIndex = value;

                if (selectedItemIndex < 0)
                {
                    selectedItemIndex = 0;
                }
                else if (selectedItemIndex >= Children.Length)
                {
                    selectedItemIndex = Children.Length - 1;
                }
            }
        }

        /// <summary>
        /// The format that is used to draw the menu.
        /// </summary>
        public GuiFormat GuiFormat => guiFormat;

        private readonly Dictionary<string, List<IMenuItem>> categories = new();
        private readonly List<IMenuItem> uncategorizedItems = new();
        private readonly Dictionary<string, string> categoryNames = new();
        private readonly Dictionary<string, int> categoryOrder = new();
        private readonly GuiFrame bgFrame;
        private readonly bool autoSize;

        private readonly RenderTarget2D renderTarget = new(
            Game1.instance.GraphicsDevice,
            Game1.WIDTH,
            Game1.HEIGHT,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.DiscardContents
        );

        private readonly SpriteBatch spriteBatch = new(Game1.instance.GraphicsDevice);

        private static readonly RasterizerState RasterizerState = new()
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            ScissorTestEnable = true
        };

        private bool dirty = true;
        private IMenuItem[]? allItems;
        private int selectedItemIndex;
        private IBTnode? activeItem;
        private GuiFormat guiFormat;

        /// <summary>
        /// Instantiates a new <see cref="AdvancedMenuSelector"/>.
        /// </summary>
        /// <param name="guiFormat">The gui format to use.</param>
        /// <param name="autoSize">If true then the menu will be resized to fit the contents. Use false if you want to make a fixed size menu.</param>
        public AdvancedMenuSelector(GuiFormat guiFormat, bool autoSize = false)
        {
            this.autoSize = autoSize;
            this.guiFormat = guiFormat;
            bgFrame = new GuiFrame(GetBackgroundRectangle());
        }

        /// <summary>
        /// Returns all children and casts them to <see cref="IMenuItem"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IMenuItem> GetMenuItems()
        {
            if (dirty)
                OnDirty();

            return allItems!.AsEnumerable();
        }

        /// <summary>
        /// Adds a menu item under the given category.
        /// </summary>
        /// <param name="category">The name of the category.</param>
        /// <param name="menuItem">The menu item to add.</param>
        /// <typeparam name="T">The type of the menu item.</typeparam>
        /// <exception cref="ArgumentException">Thrown if the menuItem does not inherit <see cref="IBTnode"/></exception>
        public void AddChild<T>(string category, T menuItem) where T : IBTnode, IMenuItem
        {
            AddChild(category, (IMenuItem)menuItem);
        }
        
        /// <summary>
        /// Adds a menu item under the given category.
        /// </summary>
        /// <param name="category">The name of the category.</param>
        /// <param name="menuItem">The menu item to add.</param>
        /// <exception cref="ArgumentException">Thrown if the menuItem does not inherit <see cref="IBTnode"/>.</exception>
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

        /// <summary>
        /// Adds an uncategorized menu item. It will be displayed after all categories.
        /// </summary>
        /// <param name="menuItem">The menu item to add.</param>
        /// <exception cref="ArgumentException">Thrown if menuItem does not implement <see cref="IMenuItem"/>.</exception>
        public new void AddChild(IBTnode menuItem)
        {
            if (menuItem is not IMenuItem item)
                throw new ArgumentException("Node must implement IMenuItem", nameof(menuItem));
            
            AddChild(null, item);
        }

        /// <summary>
        /// Sets the order of the given category. Higher numbers are displayed last.
        /// </summary>
        /// <param name="category">The name of the category.</param>
        /// <param name="order">The new order of the category. Higher numbers are displayed last.</param>
        public void SetCategoryOrder(string category, int order)
        {
            categoryOrder[category.ToLowerInvariant()] = order;
        }

        /// <summary>
        /// Invalidates the menu and forces it to rebuild the internal menu.
        /// </summary>
        public void Invalidate()
        {
            dirty = true;
        }

        /// <inheritdoc />
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

            RenderUtility.PushRenderContext(spriteBatch, renderTarget, bgFrame.GetBounds());
            Game1.instance.GraphicsDevice.Clear(Color.Transparent);
            Game1.spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp, rasterizerState: RasterizerState);

            Vector2 drawPos = guiFormat.CalculateBounds(allItems).Location.ToVector2();
            drawPos.Y += guiFormat.padding.top;

            for (int i = 0; i < allItems.Length; i++)
            {
                float x = drawPos.X + guiFormat.padding.left;
                float y = drawPos.Y;

                if (i == selectedItemIndex && allItems[i] is not UnSelectable)
                {
                    Game1.spriteBatch.Draw(JKContentManager.GUI.Cursor, drawPos, Color.White);
                    x += 5;
                }

                allItems[i].Draw((int)x, (int)y, selectedItemIndex == i);

                drawPos.Y += guiFormat.element_margin + allItems[i].GetSize().Y;
            }

            Game1.spriteBatch.End();
            RenderUtility.PopRenderContext();

            Game1.spriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);

            using (var file = File.Create("test.jpg"))
            {
                renderTarget.SaveAsPng(file, renderTarget.Width, renderTarget.Height);
            }
        }

        /// <inheritdoc />
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
                int newIndex = SelectedItemIndex + 1;

                if (newIndex < Children.Length)
                {
                    while (Children[newIndex] is UnSelectable && newIndex < Children.Length - 1)
                    {
                        ++newIndex;
                    }

                    if (Children[newIndex] is not UnSelectable)
                        SelectedItemIndex = newIndex;
                }
            }

            if (padState.up)
            {
                int newIndex = SelectedItemIndex - 1;

                if (newIndex >= 0)
                {
                    while (Children[newIndex] is UnSelectable && newIndex > 0)
                    {
                        --newIndex;
                    }

                    if (Children[newIndex] is not UnSelectable)
                        SelectedItemIndex = newIndex;
                }
            }

            if (Children.Length > 0)
            {
                var hoverResult = Children[SelectedItemIndex].Run(tickData);

                if (hoverResult != BTresult.Failure && activeItem?.last_result != BTresult.Running)
                {
                    activeItem = Children[SelectedItemIndex];
                    JKContentManager.Audio.Menu.OnSelect();
                }
            }

            return BTresult.Running;
        }

        /// <inheritdoc />
        protected override void OnNewRun()
        {
            Reset();
        }

        /// <inheritdoc />
        protected override void ResumeRun()
        {
            Reset();
        }

        /// <summary>
        /// Called when the menu should reset its state (close any open menus, etc.).
        /// Make sure to call the base method if you override this method.
        /// </summary>
        protected virtual void Reset()
        {
            ResetSelectedIndex();
            activeItem = null;
            ControllerManager.instance.MenuController.ConsumePadPresses();
        }

        private Rectangle GetBackgroundRectangle()
        {
            return guiFormat.GetBounds();
        }

        /// <summary>
        /// Called when Invalidate() has been called and the menu needs to be redrawn.
        /// Make sure to call the base method if you override this method.
        /// </summary>
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
            allItems = Children.Cast<IMenuItem>().ToArray();
            bgFrame.SetBounds(autoSize ? guiFormat.CalculateBounds(allItems) : GetBackgroundRectangle());

            ResetSelectedIndex();
            
            dirty = false;
        }

        /// <summary>
        /// Resets the selected item index to the first item that is not <see cref="UnSelectable"/>.
        /// </summary>
        protected void ResetSelectedIndex()
        {
            for (int i = 0; i < Children.Length; ++i)
            {
                if (Children[i] is not UnSelectable)
                {
                    SelectedItemIndex = i;
                    break;
                }
            }
        }
    }
}