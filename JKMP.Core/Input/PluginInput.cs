using System;
using JKMP.Core.Plugins;

namespace JKMP.Core.Input
{
    /// <summary>
    /// Handles registering input actions and subscribing to events that are called when they're pressed.
    /// </summary>
    public class PluginInput
    {
        private bool finalized;
        private readonly Plugin owner;

        internal PluginInput(Plugin owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Register an input action that can be bound to a mouse or keyboard button.
        /// </summary>
        /// <param name="name">
        /// The name should be something descriptive, such as "Jump". This value will also be displayed in the settings menu.
        /// If you want to display a custom string in the menu use the <see cref="RegisterAction(string,string,string)"/> overload.
        /// </param>
        /// <param name="defaultKey">The default key name. Must be a valid key name from <see cref="InputManager.ValidKeyNames"/>. Can also be null, in which case it'll be unbound by default.</param>
        /// <returns>True if the action did not already exist. Note that action names are unique per plugin. Two different plugins can use the same name.</returns>
        /// <exception cref="ArgumentException">Thrown if defaultKey is not part of <see cref="InputManager.ValidKeyNames"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if this method is called after the plugin has been initialized.</exception>
        public bool RegisterAction(string name, string? defaultKey) => RegisterAction(name, name, defaultKey);

        /// <summary>
        /// Registers an input action with a custom name that can be bound to a mouse or keyboard button.
        /// </summary>
        /// <param name="name">The name can be anything as long as it's unique for this plugin.</param>
        /// <param name="uiName">This value will be displayed in the settings menu for this action.</param>
        /// <param name="defaultKey">The default key name. Must be a valid key name from <see cref="InputManager.ValidKeyNames"/>. Can also be null, in which case it'll be unbound by default.</param>
        /// <returns>True if the action did not already exist. Note that action names are unique per plugin. Two different plugins can use the same name.</returns>
        /// <exception cref="ArgumentException">Thrown if defaultKey is not part of <see cref="InputManager.ValidKeyNames"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if this method is called after the plugin has been initialized.</exception>
        public bool RegisterAction(string name, string uiName, string? defaultKey)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (uiName == null) throw new ArgumentNullException(nameof(uiName));
            ThrowIfFinalized();
            
            return InputManager.RegisterAction(owner, name, uiName, defaultKey);
        }

        /// <summary>
        /// A delegate for handling a bound action's pressed/released event.
        /// </summary>
        public delegate void BindActionCallback(bool pressed);

        /// <summary>
        /// Binds the given action name so that the callback will be invoked when the key is pressed or released.
        /// </summary>
        /// <param name="name">The name of the action. Note that it must be registered beforehand or an exception will be thrown.</param>
        /// <param name="callback">The callback to invoke when the action is pressed or released.</param>
        /// <exception cref="ArgumentException">Thrown if there is no registered action matching the specified name.</exception>
        /// <exception cref="ArgumentNullException">Thrown if name or callback is null.</exception>
        public void BindAction(string name, BindActionCallback callback)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            InputManager.BindAction(owner, name, callback);
        }

        /// <summary>
        /// Removes a binding from the given action name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <returns>True if the callback was successfully removed.</returns>
        /// <exception cref="ArgumentException">Thrown if there is no registered action matching the specified name.</exception>
        /// <exception cref="ArgumentNullException">Thrown if name or callback is null.</exception>
        public bool UnbindAction(string name, BindActionCallback callback)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            return InputManager.UnbindAction(owner, name, callback);
        }

        /// <summary>
        /// Locks the input so that no more actions can be registered.
        /// Ensures that no new actions will be added after loading saved keybindings and that they're configurable from the settings menu.
        /// Binding and unbinding can still be done at any time.
        /// </summary>
        internal void FinalizeActions()
        {
            finalized = true;
        }

        private void ThrowIfFinalized()
        {
            if (finalized)
                throw new InvalidOperationException("RegisterAction can only be called during initialization. The ideal place to call it is by overriding CreateInputActions in your plugin.");
        }
    }
}