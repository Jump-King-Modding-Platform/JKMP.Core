using System;

namespace JKMP.Core.UI
{
    public partial class ModalDialog
    {
        private static ModalManager? instance;

        /// <summary>
        /// Displays a modal dialog. onClick is called when the user clicks any of the buttons.
        /// The window is automatically closed afterwards.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="onClick">
        /// The action to invoke when the users clicks a button. Can be null.
        /// The int parameter indicates the index of the button that was pressed.
        /// </param>
        /// <param name="buttonNames">The text for each button.</param>
        public static void ShowDialog(string message, Action<int?>? onClick = null, params string[] buttonNames)
        {
            if (instance == null)
            {
                // Should never happen since the modal manager is initialized before the menu is fully created.
                throw new InvalidOperationException("ModalManager is not initialized.");
            }

            var dialog = new ModalDialog(message, buttonNames, onClick);

            instance.PushModal(dialog);
        }
    }
}