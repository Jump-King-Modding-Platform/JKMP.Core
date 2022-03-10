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

        /// <summary>
        /// A node that runs the children in the order they're added. Only proceeds to run the next child if the previous one returns Success or Failure.
        /// Returns Success if all children return Success. Returns Failure if no child returns Running.
        /// </summary>
        private class BTPriorityComposite : IBTcomposite
        {
            public BTPriorityComposite(params IBTnode[] children) : base(children)
            {
            }
            
            protected override BTresult MyRun(TickData tickData)
            {
                bool allSuccess = true;
                
                foreach (IBTnode node in Children)
                {
                    BTresult result = node.Run(tickData);

                    if (result is BTresult.Running)
                        return BTresult.Running;

                    if (result != BTresult.Success)
                        allSuccess = false;
                }

                return allSuccess ? BTresult.Success : BTresult.Failure;
            }
        }
    }
}