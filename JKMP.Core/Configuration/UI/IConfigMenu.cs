using System.Collections.Generic;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.UI
{
    public interface IConfigMenu
    {
        public MenuSelector CreateMenu(GuiFormat format, string name, MenuSelector parent, List<IDrawable> drawables);
    }
    
    public interface IConfigMenu<T> : IConfigMenu where T : class, new()
    {
        public T Values { get; }
    }
}