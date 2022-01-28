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
    /// <summary>
    /// The visibility of the text in a <see cref="TextInputField"/>.
    /// </summary>
    public enum TextVisibility
    {
        /// <summary>
        /// The text is visible at all times. This is the default value.
        /// </summary>
        Visible,
        
        /// <summary>
        /// The text is always masked (i.e. replaced with asterisks).
        /// </summary>
        Hidden,
        
        /// <summary>
        /// The text is only visible when the field is focused and text can be edited.
        /// </summary>
        HiddenWhenUnfocused
    }
    
    /// <summary>
    /// A text field that can be used to enter text with optional masking.
    /// </summary>
    public class TextInputField : IBTnode, IMenuItem
    {
        /// <summary>
        /// The name of the field.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The value of the field.
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// The maximum length of the value.
        /// </summary>
        public int MaxLength { get; set; }
        
        /// <summary>
        /// If true, the value is trimmed from whitespace when it's set.
        /// </summary>
        public bool TrimWhitespace { get; set; }
        
        /// <summary>
        /// The visibility of the text.
        /// </summary>
        public TextVisibility Visibility { get; set; }
        
        /// <summary>
        /// If true the text can't be changed.
        /// </summary>
        public bool Readonly { get; set; }

        /// <summary>
        /// Invoked when the value of the field changes.
        /// </summary>
        public Action<string>? ValueChanged { get; set; }

        private readonly SpriteFont font;

        private bool focused;
        private string pendingValue;
        private readonly Queue<char> pendingChars = new();
        private bool drawCursor = true;
        private float elapsedTimeSinceCursorToggle;
        
        /// <summary>
        /// Instantiates a new <see cref="TextInputField"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="initialValue">The initial value. Can not be null.</param>
        /// <param name="maxLength">The maximum length of the value. Should not be more than about 10 at the moment due to limited screen width and no text scrolling.</param>
        /// <param name="font">The font to use to draw the name and text.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is &lt;= 0.</exception>
        /// <exception cref="ArgumentNullException">Throw when name or value is null.</exception>
        public TextInputField(string name, string initialValue = "", int maxLength = 15, SpriteFont? font = null)
        {
            if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = initialValue ?? throw new ArgumentNullException(nameof(initialValue));
            MaxLength = maxLength;
            pendingValue = Value;
            this.font = font ?? JKContentManager.Font.MenuFont;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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