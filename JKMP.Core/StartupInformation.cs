using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.BehaviourTree.Nodes;
using JKMP.Core.Logging;
using JKMP.Core.UI;

namespace JKMP.Core
{
    internal class StartupInformation
    {
        private IBTnode? currentNode;
        private readonly IEnumerator<IBTnode> nodes;
        private int currentTick;

        public StartupInformation()
        {
            nodes = GetDialogs().GetEnumerator();
        }

        /// <summary>
        /// Updates the currently shown message and returns false if all messages have been shown.
        /// </summary>
        public bool Update(float delta)
        {
            // Run the current node if it's not a ModalDialog (they are run automatically)
            if (currentNode is not null and not ModalDialog)
                currentNode.Run(new TickData(delta, currentTick++));

            if (currentNode is { last_result: BTresult.Failure or BTresult.Success })
            {
                currentNode = null;
            }

            if (currentNode == null)
            {
                if (!nodes.MoveNext())
                {
                    return true;
                }

                currentNode = nodes.Current;
            }

            return false;
        }

        private IEnumerable<IBTnode> GetDialogs()
        {
            yield return ModalDialog.ShowInfo("This is a test");

            var confirmDialog = ModalDialog.ShowConfirm("Do you want to continue?");
            var waitForResult = new BTWaitForResult(confirmDialog, runChild: false); // Don't run the child node since dialogs are run automatically

            yield return waitForResult;

            if (confirmDialog.DialogResult == 0)
            {
                yield return ModalDialog.ShowInfo("You chose to continue");
            }
            else
            {
                yield return ModalDialog.ShowInfo("You chose to stop");
            }
        }
    }
}