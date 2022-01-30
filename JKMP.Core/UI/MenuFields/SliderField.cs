using System;
using BehaviorTree;
using JumpKing;
using JumpKing.Controller;
using JumpKing.PauseMenu;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.UI.MenuFields
{
    /// <summary>
    /// A slider field that can be used to adjust a value between a min and max value.
    /// </summary>
    public class SliderField : IBTnode, IMenuItem
    {
        /// <summary>
        /// The name of the field.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public string Name
        {
            get => name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (name == value)
                    return;
                
                name = value;
                nameTextSize = font.MeasureString(value);
                valueTextSize = font.MeasureString($" {Value:0.00}");
            }
        }

        /// <summary>
        /// The value of the field.
        /// </summary>
        public float Value
        {
            get => value;
            set
            {
                this.value = MathHelper.Clamp(value, MinValue, MaxValue);
            }
        }

        /// <summary>
        /// The minimum value of the field.
        /// </summary>
        public float MinValue { get; set; }
        
        /// <summary>
        /// The maximum value of the field.
        /// </summary>
        public float MaxValue { get; set; }
        
        /// <summary>
        /// The amount the value changes when the slider cursor is moved.
        /// </summary>
        public float StepSize { get; set; }

        /// <summary>
        /// The normalized value of the slider (between 0 and 1).
        /// </summary>
        public float NormalizedValue
        {
            get => (Value - MinValue) / (MaxValue - MinValue);
            set => Value = MinValue + value * (MaxValue - MinValue);
        }

        /// <summary>
        /// Invoked when the value of the field changes.
        /// </summary>
        public Action<float>? ValueChanged { get; set; }

        private Vector2 nameTextSize;
        private Vector2 valueTextSize;
        private readonly SpriteFont font;

        private readonly Sprite sliderLeft, sliderRight, sliderLine, sliderCursor;
        private float value;
        private string name = null!; // Name is set in the constructor through the Name property.

        private const int TextPadding = 2;
        private const int SliderLineWidth = 50;

        /// <summary>
        /// Instantiates a new <see cref="SliderField"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="stepSize">The amount the value should increment/decrement.</param>
        /// <param name="font">The font to use to draw the name.</param>
        public SliderField(string name, float initialValue, float minValue, float maxValue, float stepSize, SpriteFont? font = null)
        {
            this.font = font ?? JKContentManager.Font.MenuFont;
            Name = name;
            MinValue = minValue;
            MaxValue = maxValue;
            Value = initialValue;
            StepSize = stepSize;
            sliderLeft = JKContentManager.GUI.SliderLeft;
            sliderRight = JKContentManager.GUI.SliderRight;
            sliderLine = JKContentManager.GUI.SliderLine;
            sliderCursor = JKContentManager.GUI.SliderCursor;
        }

        /// <inheritdoc />
        protected override BTresult MyRun(TickData tickData)
        {
            var padState = ControllerManager.instance.MenuController.GetPadState();
            if (!padState.left && !padState.right)
                return BTresult.Failure;

            float oldValue = Value;
            
            if (padState.left)
                Value -= StepSize;
            if (padState.right)
                Value += StepSize;
            
            // Round the value to 2 fractional digits
            Value = (float)Math.Round(value, digits: 2, MidpointRounding.ToEven);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Value != oldValue)
                ValueChanged?.Invoke(Value);

            return BTresult.Success;
        }

        /// <inheritdoc />
        public void Draw(int x, int y, bool selected)
        {
            // Draw the name
            Vector2 drawPos = new Vector2(x, y);
            TextHelper.DrawString(font, Name, drawPos, Color.White, Vector2.Zero);
            drawPos.X += nameTextSize.X + TextPadding;
            drawPos.Y += (int)(nameTextSize.Y / 2f - sliderLeft.source.Height / 2f);

            // Draw the slider
            sliderLine.Draw(new Rectangle((int)(drawPos.X + sliderLeft.source.Width), (int)drawPos.Y, SliderLineWidth, sliderLine.source.Height));
            sliderLeft.Draw(drawPos);
            sliderRight.Draw(drawPos + new Vector2(sliderLeft.source.Width + SliderLineWidth, 0));

            // Draw the cursor
            sliderCursor.Draw(drawPos.X + sliderLeft.source.Width + (SliderLineWidth * NormalizedValue), drawPos.Y);
            
            // Draw the value
            var valueDrawPos = new Vector2(drawPos.X + SliderLineWidth + sliderLeft.source.Width + sliderRight.source.Width - 2, drawPos.Y);
            TextHelper.DrawString(JKContentManager.Font.MenuFontSmall, $" {Value:0.00}", valueDrawPos, Color.White, new Vector2(0, 0.25f));
        }

        /// <inheritdoc />
        public Point GetSize()
        {
            int x = (int)(sliderLeft.source.Width + sliderRight.source.Width + SliderLineWidth + nameTextSize.X + TextPadding + valueTextSize.X);
            int y = (int)Math.Max(sliderLeft.source.Height, nameTextSize.Y);
            return new Point(x, y);
        }
    }
}