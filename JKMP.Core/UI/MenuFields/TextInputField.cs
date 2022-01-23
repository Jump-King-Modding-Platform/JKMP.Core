using System;
using System.Collections.Generic;
using BehaviorTree;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.UI.MenuFields
{
    public enum TextVisibility
    {
        Visible,
        Hidden,
        HiddenWhenUnfocused
    }
    
    public class TextInputField : IBTnode, IMenuItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int MaxLength { get; set; }
        public bool TrimWhitespace { get; set; }
        public TextVisibility Visibility { get; set; }
        public bool Readonly { get; set; }

        public Action<string>? ValueChanged { get; set; }

        private SpriteFont font;

        private bool focused;
        private string pendingValue;
        private readonly Queue<char> pendingChars = new();
        private bool drawCursor = true;
        private float elapsedTimeSinceCursorToggle;
        
        public TextInputField(string name, string initialValue = "", int maxLength = 15, SpriteFont? font = null)
        {
            if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = initialValue ?? throw new ArgumentNullException(nameof(initialValue));
            MaxLength = maxLength;
            pendingValue = Value;
            this.font = font ?? JKContentManager.Font.MenuFont;
        }

        protected override BTresult MyRun(TickData tickData)
        {
            var menuController = ControllerManager.instance.MenuController;
            var padState = menuController.GetPadState();
            
            if (focused)
            {
                elapsedTimeSinceCursorToggle += tickData.delta_time;

                if (elapsedTimeSinceCursorToggle > 0.53f)
                {
                    elapsedTimeSinceCursorToggle = 0;
                    drawCursor = !drawCursor;
                }
                
                if (padState.pause)
                {
                    menuController.ConsumePadPresses();

                    ApplyNewValueAndUnfocus();
                    return BTresult.Success;
                }

                while (pendingChars.Count > 0)
                {
                    char ch = pendingChars.Dequeue();
                    bool resetCursor = true;

                    switch ((byte)ch)
                    {
                        case 8: // Backspace
                        {
                            if (pendingValue.Length > 0)
                            {
                                pendingValue = pendingValue.Substring(0, pendingValue.Length - 1);
                            }
                            break;
                        }
                        case 13: // Enter/return
                        {
                            ApplyNewValueAndUnfocus();
                            break;
                        }
                        case 27: // Escape
                        {
                            resetCursor = false;
                            break;
                        }
                        case 1: // Happens when you press CTRL+A
                        {
                            break;
                        }
                        case 127: // Ctrl+Backspace
                        {
                            string newValue = pendingValue.TrimEnd();
                            int lastIndexOfSpace = newValue.LastIndexOf(' ');

                            if (lastIndexOfSpace >= 0)
                            {
                                newValue = newValue.Substring(0, lastIndexOfSpace).TrimEnd() + " ";
                                pendingValue = newValue;
                            }
                            else
                            {
                                pendingValue = string.Empty;
                            }

                            break;
                        }
                        case 9: // Tab
                        {
                            break;
                        }
                        default:
                        {
                            pendingValue += ch;
                            break;
                        }
                    }

                    if (resetCursor)
                    {
                        drawCursor = true;
                        elapsedTimeSinceCursorToggle = 0;
                    }
                }

                if (pendingValue.Length > MaxLength)
                    pendingValue = pendingValue.Substring(0, MaxLength);

                return BTresult.Running;
            }

            if (padState.confirm && !Readonly)
            {
                menuController.ConsumePadPresses();

                pendingValue = Value;
                SetFocus(true);
                return BTresult.Success;
            }
                
            return BTresult.Failure;
        }

        private void ApplyNewValueAndUnfocus()
        {
            string newValue = TrimWhitespace ? pendingValue.Trim() : pendingValue;
            pendingValue = newValue; // Set pending value to potentially trimmed value
            
            if (Value != newValue)
            {
                Value = newValue;
                ValueChanged?.Invoke(Value);
            }

            SetFocus(false);
        }

        private void SetFocus(bool focused)
        {
            if (this.focused == focused)
                return;

            elapsedTimeSinceCursorToggle = 0;
            drawCursor = true;
            this.focused = focused;

            if (focused)
            {
                Game1.instance.Window.TextInput += OnTextInput;
            }
            else
            {
                Game1.instance.Window.TextInput -= OnTextInput;
                pendingChars.Clear();
            }
        }

        public void Draw(int x, int y, bool selected)
        {
            Vector2 nameSize = font.MeasureString(Name + ":");
            Game1.spriteBatch.DrawString(font, Name, new Vector2(x, y), Color.White);

            Vector2 valuePosition = new Vector2(x + nameSize.X, y);
            Game1.spriteBatch.DrawString(font, GetValueDrawString(), valuePosition, !Readonly ? Color.White : Color.Gray);
            
            // Draw line under input text
            Vector2 maxValueSize = font.MeasureString(new string('_', MaxLength));
            Vector2 linePosition = new(valuePosition.X, valuePosition.Y + maxValueSize.Y);
            Rectangle lineRectangle = new((int)linePosition.X, (int)linePosition.Y, (int)maxValueSize.X, 1);
            Game1.spriteBatch.Draw(JKContentManager.Pixel.texture, lineRectangle, !Readonly ? Color.White : Color.Gray);

            if (focused && drawCursor)
            {
                Vector2 valueSize = font.MeasureString(GetValueDrawString());
                
                // Draw rectangle at the end of the input text but 4 pixels smaller than text height and centered vertically
                // Use nameSize.Y for height since height will be 0 if input text is empty
                Rectangle cursorRectangle = new((int)(valuePosition.X + valueSize.X), (int)valuePosition.Y + 2, 1, (int)nameSize.Y - 4);
                Game1.spriteBatch.Draw(JKContentManager.Pixel.texture, cursorRectangle, Color.White);
            }
        }

        private string GetValueDrawString()
        {
            switch (Visibility)
            {
                case TextVisibility.Visible:
                    return pendingValue;
                case TextVisibility.Hidden:
                    return new string('*', pendingValue.Length);
                case TextVisibility.HiddenWhenUnfocused:
                {
                    if (focused)
                        return pendingValue;

                    return new string('*', pendingValue.Length);
                }
                default: throw new NotSupportedException("Unexpected text visibility");
            }
        }

        public Point GetSize()
        {
            Vector2 maxValueSize = font.MeasureString(new string('_', MaxLength));
            Vector2 nameSize = font.MeasureString(Name + ":");
            return new Point((int)(maxValueSize.X + nameSize.X), (int)Math.Max(maxValueSize.Y, nameSize.Y));
        }

        private void OnTextInput(object sender, TextInputEventArgs args)
        {
            if (!focused)
                return;
            
            pendingChars.Enqueue(args.Character);
        }
    }
}