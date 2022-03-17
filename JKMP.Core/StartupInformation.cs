using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using JKMP.Core.Input;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
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
            var jkBindings = InputManager.GetBindings(Plugin.InternalPlugin);
            var action = jkBindings.GetActionByName("confirm")!.Value;
            var primaryKey = jkBindings.GetKeyBindsForAction(action.Name).FirstOrDefault();
            string confirmButtonName = primaryKey.ToDisplayString();
            
            yield return ModalDialog.ShowDialog("Thank you for installing JKMP!" +
                                                "\n\nJKMP replaces the vanilla input system, which" +
                                                "\nunfortunately means that your keybinds have been reset." +
                                                "\n\nIf you had any custom keybinds you can re-bind them" +
                                                "\nin the settings menu." +
                                                "\n\nThis message will not be shown again.",
                null,
                inputDelay: 3,
                $"Press {confirmButtonName} to continue"
            );
        }
    }
}