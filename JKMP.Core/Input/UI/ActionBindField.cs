using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using JKMP.Core.Logging;
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
        
        private IReadOnlyList<KeyBind> keyBinds = null!;
        private KeyBind bind1;
        private KeyBind bind2;

        private int SelectedBindIndex
        {
            get => getSelectedBindIndex();
            set => setSelectedBindIndex(value);
        }
        private State currentState;

        private static SpriteFont Font => JKContentManager.Font.MenuFontSmall;

        public ActionBindField(InputManager.Bindings bindings, InputManager.ActionInfo action, float fieldWidth, List<IDrawable> drawables, Func<int> getSelectedBindIndex, Action<int> setSelectedBindIndex)
        {
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            this.action = action;
            this.fieldWidth = fieldWidth;
            this.getSelectedBindIndex = getSelectedBindIndex;
            this.setSelectedBindIndex = setSelectedBindIndex;
            xNameOffset = 0;
            xBind1Offset = (float)Math.Round(fieldWidth * 0.33f);
            xBind2Offset = (float)Math.Round(fieldWidth * 0.66f);
            
            UpdateKeyBinds();
            
            rebindWindow = new(action);
            drawables.Add(rebindWindow);
        }

        private void UpdateKeyBinds()
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
                        if (result == BTresult.Success)
                        {
                            KeyBind oldBind = SelectedBindIndex == 0 ? bind1 : bind2;

                            // Make sure that new bind is not the same as the alternative bind
                            // However make sure to allow it if both binds are invalid (unbound)
                            switch (SelectedBindIndex)
                            {
                                case 0:
                                    if (bind2.IsValid && bind2 == rebindWindow.CurrentBind)
                                    {
                                        currentState = State.None;
                                        return BTresult.Failure;
                                    }

                                    break;
                                case 1:
                                    if (bind1.IsValid && bind1 == rebindWindow.CurrentBind)
                                    {
                                        currentState = State.None;
                                        return BTresult.Failure;
                                    }
                                    break;
                            }

                            // Unmap old key if valid
                            if (oldBind.IsValid)
                            {
                                bindings.UnmapAction(oldBind, action.Name);
                            }
                            
                            // Map new key if valid
                            if (rebindWindow.CurrentBind.IsValid)
                            {
                                bindings.MapAction(rebindWindow.CurrentBind, action.Name);
                            }

                            switch (SelectedBindIndex)
                            {
                                case 0:
                                    bind1 = rebindWindow.CurrentBind;
                                    break;
                                case 1:
                                    bind2 = rebindWindow.CurrentBind;
                                    break;
                            }

                            InputManager.Save();
                        }
                        
                        currentState = State.None;
                    }
                    
                    return result;
                }
            }

            return BTresult.Failure;
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