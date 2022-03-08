using System.Collections.Generic;

namespace JKMP.Core.Input
{
    /// <summary>
    /// This interface translates arbitrary input into string key names. 
    /// </summary>
    public interface IInputMapper
    {
        /// <summary>
        /// Gets a list of key names that qualify as a modifier key.
        /// These can be combined with other keys to create more advanced keybinds.
        /// For example, left control + c to copy something.
        /// </summary>
        public ISet<string> ModifierKeys { get; }
        
        /// <summary>
        /// Gets a list of key names that are valid and can be used with this input mapper.
        /// </summary>
        public ISet<string> ValidKeyNames { get; }

        /// <summary>
        /// This method returns the display name for a key.
        /// It is normally displayed in the controls menu but also potentially in other places.
        /// </summary>
        string? GetKeyDisplayName(in string keyName);

        /// <summary>
        /// This method returns keys that were just pressed this frame.
        /// </summary>
        IEnumerable<string> GetPressedKeys();
        
        /// <summary>
        /// This method returns keys that were just released this frame.
        /// </summary>
        IEnumerable<string> GetReleasedKeys();

        /// <summary>
        /// Called once per frame. Useful for querying new input states and storing old ones to compare with. 
        /// </summary>
        void Update();
    }
}