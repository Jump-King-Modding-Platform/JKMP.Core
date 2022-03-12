using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.Logging;
using JumpKing.Util;

namespace JKMP.Core.UI
{
    public partial class ModalDialog
    {
        internal static IBTnode CreateMainMenuNode(IBTnode mainMenu, List<IDrawable> drawables)
        {
            if (instance != null)
            {
                instance.OnDispose();
                instance = null;
            }

            instance = new ModalManager(drawables);
            drawables.Add(instance);

            var result = new BTsimultaneous(instance, mainMenu);
            return result;
        }
    }
}