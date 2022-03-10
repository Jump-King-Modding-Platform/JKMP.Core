using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTree;
using JKMP.Core.Logging;
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

        private readonly float fieldWidth;
        private readonly float xNameOffset;
        private readonly float xBind1Offset;
        private readonly float xBind2Offset;
        private readonly RebindWindow rebindWindow;
        private readonly Func<int> getSelectedBindIndex;
        private readonly Action<int> setSelectedBindIndex;
        private readonly Action updateAllBinds;

        private IReadOnlyList<KeyBind> keyBinds = null!;
        private KeyBind bind1;
        private KeyBind bind2;

        private int SelectedBindIndex
        {
            get => getSelectedBindIndex();
            set => setSelectedBindIndex(value);
        }
        private State currentState;
        private ModalDialog? modal;

        private static SpriteFont Font => JKContentManager.Font.MenuFontSmall;

        public ActionBindField(InputManager.Bindings bindings,
            InputManager.ActionInfo action,
            float fieldWidth,
            List<IDrawable> drawables,
            Func<int> getSelectedBindIndex,
            Action<int> setSelectedBindIndex,
            Action updateAllBinds
        )
        {
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            this.action = action;
            this.fieldWidth = fieldWidth;
            this.getSelectedBindIndex = getSelectedBindIndex;
            this.setSelectedBindIndex = setSelectedBindIndex;
            this.updateAllBinds = updateAllBinds;
            xNameOffset = 0;
            xBind1Offset = (float)Math.Round(fieldWidth * 0.33f);
            xBind2Offset = (float)Math.Round(fieldWidth * 0.66f);

            UpdateKeyBinds();

            rebindWindow = new(action);
            drawables.Add(rebindWindow);
        }

        public void UpdateKeyBinds()
        {
            keyBinds = bindings.GetKeyBindsForAction(action.Name);
            bind1 = keyBinds.FirstOrDefault();
            bind2 = keyBinds.Skip(1).FirstOrDefault();
        }

        protected override BTresult MyRun(TickData tickData)
        {
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
                            rebindWindow.Show(SelectedBindIndex == 0 ? bind1 : bind2, SelectedBindIndex == 0 ? "primary" : "secondary");
                            currentState = State.Binding;
                            return BTresult.Running;
                        }

                        if (inputData.left)
                        {
                            SelectedBindIndex = MathHelper.Clamp(SelectedBindIndex - 1, 0, 1);
                        }
                        
                        if (inputData.right)
                        {
                            SelectedBindIndex = MathHelper.Clamp(SelectedBindIndex + 1, 0, 1);
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
                        ref var targetBind = ref GetSelectedBind(out KeyBind otherBind);
                        KeyBind oldBind = targetBind;
                        
                        if (result == BTresult.Success)
                        {
                            if (targetBind == rebindWindow.CurrentBind)
                            {
                                currentState = State.None;
                                return BTresult.Failure;
                            }

                            // Check if the new key is already bound to this action
                            if (rebindWindow.CurrentBind == otherBind && otherBind.IsValid)
                            {
                                ModalDialog.ShowInfo($"'{rebindWindow.CurrentBind}' is already bound to this action.");
                                currentState = State.None;
                                return BTresult.Failure;
                            }
                            
                            // Check if the new key is already bound to another action
                            if (rebindWindow.CurrentBind.IsValid)
                            {
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

                            DoRebind(ref targetBind);

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

                    ref var currentBind = ref GetSelectedBind(out _);

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

                            DoRebind(ref currentBind);
                            updateAllBinds();

                            break;
                        }
                        case 1: // Add
                        {
                            DoRebind(ref currentBind);
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

        private ref KeyBind GetSelectedBind(out KeyBind otherBind)
        {
            ref KeyBind targetBind = ref SelectedBindIndex == 0 ? ref bind1 : ref bind2;
            otherBind = SelectedBindIndex == 0 ? bind2 : bind1;
            return ref targetBind;
        }

        void DoRebind(ref KeyBind targetBind)
        {
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

            // Update target bind
            targetBind = rebindWindow.CurrentBind;
        }

        public void Draw(int x, int y, bool selected)
        {
            Color drawColor = selected ? Color.White : Color.LightGray;
            
            // Draw action name
            TextHelper.DrawString(Font, action.UiName, new Vector2(x + xNameOffset, y), drawColor, Vector2.Zero);
            
            // Draw primary binding
            DrawBind(bind1, x + xBind1Offset, y, selected && SelectedBindIndex == 0);
            DrawBind(bind2, x + xBind2Offset, y, selected && SelectedBindIndex == 1);
        }

        private void DrawBind(KeyBind bind, float x, float y, bool selected)
        {
            Color drawColor = selected ? Color.White : Color.LightGray;
            string text = bind.IsValid ? bind.ToDisplayString() : "(unbound)";

            if (selected)
                text = $"[{text}]";
            
            TextHelper.DrawString(Font, text, new Vector2(x, y), drawColor, Vector2.Zero);
        }

        public Point GetSize()
        {
            var point = Font.MeasureString(action.UiName).ToPoint();
            point.X = (int)Math.Round(fieldWidth);
            
            return point;
        }
    }
}