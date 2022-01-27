using System;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT.Actions;
using JumpKing.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.UI.MenuFields
{
    public class CheckboxField : IToggle, IMenuItem
    {
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

        public bool Value
        {
            get => toggle;
            set => OverrideToggle(value);
        }

        public Action<bool>? ValueChanged { get; set; }

        private Vector2 textSize;
        private readonly SpriteFont font;
        private string name = null!; // Name is set in the constructor through the Name property.

        private const int TextPadding = 2;

        public CheckboxField(string name, bool initialValue = false, SpriteFont? font = null) : base(initialValue)
        {
            this.font = font ?? JKContentManager.Font.MenuFont;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
        
        public void Draw(int x, int y, bool selected)
        {
            TextHelper.DrawString(font, Name, new Vector2(x, y), Color.White, Vector2.Zero);

            Vector2 drawPosition = Vector2.Zero;
            drawPosition.X = x + textSize.X + TextPadding;
            drawPosition.Y = y + textSize.Y / 2;
            DrawCheckBox(drawPosition, Value);
        }

        public Point GetSize()
        {
            var size = GetCheckBoxSize();
            size.X += (int)textSize.X;
            size.X += TextPadding; // Account for padding between the checkbox and the text
            return size;
        }

        protected override void OnToggle()
        {
            ValueChanged?.Invoke(toggle);
        }
    }
}