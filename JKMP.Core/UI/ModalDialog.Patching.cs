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
            // Dispose the old instance if it exists
            instance?.OnDispose();
            instance = null; // Prevents the ModalManager ctor from throwing an exception that the instance already exists

            instance = new ModalManager(drawables);
            drawables.Add(instance);

            var result = new BTsimultaneous(instance, mainMenu);
            return result;
        }
    }
}