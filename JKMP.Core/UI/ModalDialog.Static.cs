using System;

namespace JKMP.Core.UI
{
    public partial class ModalDialog
    {
        private static ModalManager? instance;

        /// <summary>
        /// Displays a modal dialog. onClick is called when the user clicks any of the buttons.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="onClick">
        /// The action to invoke when the users clicks a button. Can be null.
        /// The int parameter indicates the index of the button that was pressed.
        /// </param>
        /// <param name="inputDelay">
        /// The amount of seconds to delay before enabling input.
        /// Useful for preventing the user from accidentally closing the dialog as soon as it opens.
        /// </param>
        /// <param name="buttonNames">The text for each button.</param>
        public static ModalDialog ShowDialog(string message, Action<int?>? onClick = null, float inputDelay = 0, params string[] buttonNames)
        {
            ThrowIfNotInitialized();

            var dialog = new ModalDialog(message, buttonNames, onClick, inputDelay);
            instance!.PushModal(dialog);
            return dialog;
        }

        /// <summary>
        /// Displays a modal info dialog. The only option is "OK". The callback is invoked when the modal is closed.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="onClosed">The callback to invoke when the modal is closed. Can be null.</param>
        /// <param name="inputDelay">
        /// The amount of seconds to delay before enabling input.
        /// Useful for preventing the user from accidentally closing the dialog as soon as it opens.
        /// </param>
        public static ModalDialog ShowInfo(string message, Action? onClosed = null, float inputDelay = 0)
        {
            ThrowIfNotInitialized();

            Action<int?>? callback = null;

            if (onClosed != null)
            {
                callback = _ => onClosed();
            }

            var dialog = new ModalDialog(message, new[] { "ok" }, callback, inputDelay);
            instance!.PushModal(dialog);
            return dialog;
        }

        /// <summary>
        /// Displays a modal with yes and no options. The callback is invoked when the modal is closed.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="onClosed">The callback to invoke when the modal is closed. Can be null.</param>
        /// <param name="inputDelay">
        /// The amount of seconds to delay before enabling input.
        /// Useful for preventing the user from accidentally closing the dialog as soon as it opens.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown is onClosed is null.</exception>
        public static ModalDialog ShowConfirm(string message, Action<bool>? onClosed = null, float inputDelay = 0)
        {
            ThrowIfNotInitialized();

            var dialog = new ModalDialog(
                message,
                new[] { "yes", "no" },
                index => { onClosed?.Invoke(index == 0); },
                inputDelay
            );

            instance!.PushModal(dialog);
            return dialog;
        }

        private static void ThrowIfNotInitialized()
        {
            if (instance == null)
            {
                // Should never happen since the modal manager is initialized before the menu is fully created.
                throw new InvalidOperationException("ModalManager is not initialized.");
            }
        }
    }
}