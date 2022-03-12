using System.Collections.Generic;
using BehaviorTree;
using JumpKing.Util;

namespace JKMP.Core.UI
{
    public partial class ModalDialog
    {
        internal static IBTnode CreateMainMenuNode(IBTnode mainMenu, List<IDrawable> drawables)
        {
            instance ??= new ModalManager(drawables);
            drawables.Add(instance);

            var result = new BTsimultaneous(instance, mainMenu);
            return result;
        }
    }
}