using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTree;
using JKMP.Core.Plugins;
using JKMP.Core.UI;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = JumpKing.Util.IDrawable;
using KeyBind = JKMP.Core.Input.InputManager.KeyBind;

namespace JKMP.Core.Input.UI
{
    internal class ActionBindField : IBTnode, IMenuItem
    {
        enum State
        {
            None,
            Binding,
            WaitingForModal,
        }
        
        private readonly InputManager.Bindings bindings;
        private readonly InputManager.ActionInfo action;
        private readonly RebindWindow rebindWindow;
        private readonly Func<int> getSelectedBindIndex;
        private readonly Action<int> setSelectedBindIndex;
        private readonly Action updateAllBinds;

        private KeyBind[] keyBinds = null!;

        private int SelectedBindIndex
        {
            get => getSelectedBindIndex();
            set
            {
                // Clamp index if it's out of bounds
                if (value > keyBinds.Length)
                {
                    value = keyBinds.Length;
                }

                setSelectedBindIndex(value);
            }
        }

        private State currentState;
        private ModalDialog? modal;

        private static SpriteFont Font => JKContentManager.Font.MenuFontSmall;

        private const int XSizePerCol = 150;

        public ActionBindField(InputManager.Bindings bindings,
            InputManager.ActionInfo action,
            List<IDrawable> drawables,
            Func<int> getSelectedBindIndex,
            Action<int> setSelectedBindIndex,
            Action updateAllBinds
        )
        {
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            this.action = action;
            this.getSelectedBindIndex = getSelectedBindIndex;
            this.setSelectedBindIndex = setSelectedBindIndex;
            this.updateAllBinds = updateAllBinds;

            UpdateKeyBinds();

            rebindWindow = new(action);
            drawables.Add(rebindWindow);
        }

        public void UpdateKeyBinds()
        {
            keyBinds = bindings.GetKeyBindsForAction(action.Name).ToArray();
        }

        protected override BTresult MyRun(TickData tickData)
        {
            SelectedBindIndex = SelectedBindIndex; // Clamps index if it's out of bounds
            
            var inputData = ControllerManager.instance.MenuController.GetPadState();

            switch (currentState)
            {
                case State.None:
                {
                    if (inputData.left || inputData.right || inputData.confirm)
                    {
                        ControllerManager.instance.MenuController.ConsumePadPresses();

                        if (inputData.confirm)
                        {
                            KeyBind keyBind = default;
                            
                            if (SelectedBindIndex < keyBinds.Length)
                            {
                                keyBind = keyBinds[SelectedBindIndex];
                            }

                            rebindWindow.Show(keyBind, GetRebindWindowPriorityText());
                            currentState = State.Binding;
                            return BTresult.Running;
                        }

                        if (inputData.left)
                        {
                            SelectedBindIndex = MathHelper.Clamp(SelectedBindIndex - 1, 0, keyBinds.Length);
                        }
                        
                        if (inputData.right)
                        {
                            SelectedBindIndex = MathHelper.Clamp(SelectedBindIndex + 1, 0, keyBinds.Length);
                        }
                    }
                    break;
                }
                case State.Binding:
                {
                    rebindWindow.Run(tickData);
                    BTresult result = rebindWindow.last_result;

                    if (result != BTresult.Running)
                    {
                        KeyBind targetBind = SelectedBindIndex < keyBinds.Length ? keyBinds[SelectedBindIndex] : default;

                        if (result == BTresult.Success)
                        {
                            if (targetBind == rebindWindow.CurrentBind)
                            {
                                currentState = State.None;
                                return BTresult.Failure;
                            }
                            
                            // Check if the new key is already bound to another action
                            if (rebindWindow.CurrentBind.IsValid)
                            {
                                // Check if the new key is already bound to this action
                                if (keyBinds.Contains(rebindWindow.CurrentBind))
                                {
                                    ModalDialog.ShowInfo($"'{rebindWindow.CurrentBind}' is already bound to this action.");
                                    currentState = State.None;
                                    return BTresult.Failure;
                                }
                                
                                var existingActions = InputManager.GetActionsForKeyBind(rebindWindow.CurrentBind);
                                if (existingActions.Count > 0)
                                {
                                    StringBuilder actionsBuilder = new();

                                    foreach (var kv in existingActions)
                                    {
                                        foreach (var actionInfo in kv.Value)
                                        {
                                            string pluginName = kv.Key.Info.Name!;

                                            if (kv.Key == Plugin.InternalPlugin)
                                                pluginName = "Jump King";

                                            actionsBuilder.Append(pluginName);
                                            actionsBuilder.Append(" - ");
                                            actionsBuilder.AppendLine(actionInfo.UiName);
                                        }
                                    }

                                    modal = ModalDialog.ShowDialog(
                                        $"'{rebindWindow.CurrentBind}' is already bound to the following actions:\n{actionsBuilder}\nWhat do you want to do?",
                                        onClick: null,
                                        "Replace", "Add", "Cancel"
                                    );
                                    currentState = State.WaitingForModal;
                                    return BTresult.Running;
                                }
                            }

                            DoRebind();

                            InputManager.Save();
                        }
                        
                        currentState = State.None;
                        return BTresult.Failure;
                    }

                    return result;
                }
                case State.WaitingForModal:
                {
                    if (modal!.last_result == BTresult.Running)
                        return BTresult.Running;

                    switch (modal.DialogResult)
                    {
                        case 0: // Overwrite
                        {
                            // Remove the keybinds that use the new key
                            var existingActions = InputManager.GetActionsForKeyBind(rebindWindow.CurrentBind);
                            
                            foreach (var kv in existingActions)
                            {
                                var pluginBindings = InputManager.GetBindings(kv.Key);
                                
                                // ReSharper disable once LocalVariableHidesMember
                                foreach (InputManager.ActionInfo action in kv.Value)
                                {
                                    pluginBindings.UnmapAction(rebindWindow.CurrentBind, action.Name);
                                }
                            }

                            DoRebind();
                            updateAllBinds();
                            InputManager.Save();

                            break;
                        }
                        case 1: // Add
                        {
                            DoRebind();
                            InputManager.Save();
                            break;
                        }
                    }

                    currentState = State.None;
                    modal = null;
                    return BTresult.Failure;
                }
            }

            return BTresult.Failure;
        }

        private string GetRebindWindowPriorityText()
        {
            var index = SelectedBindIndex + 1;
            switch (index)
            {
                case 1: return index + "st";
                case 2: return index + "nd";
                case 3: return index + "rd";
                default: return index + "th";
            }
        }

        private ref KeyBind GetSelectedBind()
        {
            return ref keyBinds[SelectedBindIndex];
        }

        void DoRebind()
        {
            KeyBind targetBind = SelectedBindIndex < keyBinds.Length ? GetSelectedBind() : default;
            
            // Unmap old key if valid
            if (targetBind.IsValid)
            {
                bindings.UnmapAction(targetBind, action.Name);
            }
                            
            // Map new key if valid
            if (rebindWindow.CurrentBind.IsValid)
            {
                bindings.MapAction(rebindWindow.CurrentBind, action.Name);
            }

            UpdateKeyBinds();
        }

        public void Draw(int x, int y, bool selected)
        {
            Color drawColor = selected ? Color.White : Color.LightGray;
            int drawBindIndex = (SelectedBindIndex / 2) * 2;

            // Draw action name
            TextHelper.DrawString(Font, action.UiName, new Vector2(x, y), drawColor, Vector2.Zero);

            // Draw bindings
            for (int i = drawBindIndex; i < keyBinds.Length; ++i)
            {
                int drawIndex = i - drawBindIndex;
                DrawBind(keyBinds[i], x + XSizePerCol * (drawIndex + 1), y, selected && SelectedBindIndex == i);
            }
            
            // Draw previous/next page indicators
            if (drawBindIndex > 0) // Left indicator
            {
                TextHelper.DrawString(Font, "<", new Vector2(x + (XSizePerCol * 1) - 10, y), drawColor, Vector2.Zero);
            }
            
            if (drawBindIndex + 2 <= keyBinds.Length) // Right indicator
            {
                ref readonly KeyBind bind = ref keyBinds[drawBindIndex + 1];
                string measureText = bind.ToDisplayString();
                
                if (selected && SelectedBindIndex == drawBindIndex + 1)
                {
                    measureText = $"[{measureText}]";
                }
                
                Vector2 size = Font.MeasureString(measureText);

                TextHelper.DrawString(Font, ">", new Vector2((x + size.X + 3) + (XSizePerCol * 2), y), drawColor, Vector2.Zero);
            }

            if (keyBinds.Length >= drawBindIndex) // Only draw add button if it's on the current "page"
            {
                // Draw add button
                Vector2 drawPosition = new Vector2(x + XSizePerCol * (keyBinds.Length + 1 - drawBindIndex), y);
                string text = "(add bind)";

                if (selected && SelectedBindIndex == keyBinds.Length)
                {
                    text = $"[{text}]";
                }

                TextHelper.DrawString(Font, text, drawPosition, drawColor, Vector2.Zero);
            }
        }

        private void DrawBind(KeyBind bind, float x, float y, bool selected)
        {
            Color drawColor = selected ? Color.White : Color.LightGray;
            string text = bind.ToDisplayString();

            if (selected)
                text = $"[{text}]";
            
            TextHelper.DrawString(Font, text, new Vector2(x, y), drawColor, Vector2.Zero);
        }

        public Point GetSize()
        {
            Point size = Point.Zero;

            size.X = (keyBinds.Length + 2) * XSizePerCol; // Add 2 to account for action name and the add button
            size.Y = Font.LineSpacing;
            
            return size;
        }
    }
}