using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.UI;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.UI
{
    public interface IConfigMenu
    {
        public IBTnode CreateMenu(GuiFormat format, string name, AdvancedMenuSelector parent, List<IDrawable> drawables);
    }
    
    public interface IConfigMenu<T> : IConfigMenu where T : class, new()
    {
        public T Values { get; }
    }
}