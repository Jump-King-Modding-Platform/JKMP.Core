using System;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT.Actions;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.UI.MenuFields
{
    /// <summary>
    /// A checkbox field type that be toggled on and off.
    /// </summary>
    public class CheckboxField : IToggle, IMenuItem
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
                textSize = font.MeasureString(value);
            }
        }

        /// <summary>
        /// The value of the field.
        /// </summary>
        public bool Value
        {
            get => toggle;
            set => OverrideToggle(value);
        }

        /// <summary>
        /// Invoked when the value of the field changes.
        /// </summary>
        public Action<bool>? ValueChanged { get; set; }

        private Vector2 textSize;
        private readonly SpriteFont font;
        private string name = null!; // Name is set in the constructor through the Name property.

        private const int TextPadding = 2;

        /// <summary>
        /// Instantiates a new checkbox field.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="font">The font to use to draw the name.</param>
        /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
        public CheckboxField(string name, bool initialValue = false, SpriteFont? font = null) : base(initialValue)
        {
            this.font = font ?? JKContentManager.Font.MenuFont;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public void Draw(int x, int y, bool selected)
        {
            TextHelper.DrawString(font, Name, new Vector2(x, y), Color.White, Vector2.Zero);

            Vector2 drawPosition = Vector2.Zero;
            drawPosition.X = x + textSize.X + TextPadding;
            drawPosition.Y = y + textSize.Y / 2;
            DrawCheckBox(drawPosition, Value);
        }

        /// <inheritdoc />
        public Point GetSize()
        {
            var size = GetCheckBoxSize();
            size.X += (int)textSize.X;
            size.X += TextPadding; // Account for padding between the checkbox and the text
            return size;
        }

        /// <inheritdoc />
        protected override void OnToggle()
        {
            ValueChanged?.Invoke(toggle);
        }
    }
}