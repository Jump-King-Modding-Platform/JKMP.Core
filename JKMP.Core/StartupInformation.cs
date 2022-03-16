using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.Logging;
using JKMP.Core.UI;

namespace JKMP.Core
{
    internal class StartupInformation
    {
        private ModalDialog? currentDialog;
        private readonly IEnumerator<ModalDialog> dialogs;

        public StartupInformation()
        {
            dialogs = GetDialogs().GetEnumerator();
        }

        public bool Update()
        {
            if (currentDialog is { last_result: BTresult.Failure or BTresult.Success })
            {
                currentDialog = null;
            }
            
            if (currentDialog == null)
            {
                if (!dialogs.MoveNext())
                {
                    return true;
                }

                currentDialog = dialogs.Current;
            }

            return false;
        }
        
        private IEnumerable<ModalDialog> GetDialogs()
        {
            yield return ModalDialog.ShowInfo("This is a test");
            yield return ModalDialog.ShowConfirm("Is this a test?");
        }
    }
}