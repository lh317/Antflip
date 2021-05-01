using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Antflip
{
    /// <summary>Simplifies implementation of <see cref="INotifyPropertyChanged" />.</summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Sets the <paramref name="target" /> field to <paramref name="value">
        /// and fires a <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <example>
        /// <code>
        /// private int quantity;
        /// public int Quantity
        /// {
        ///     get => quantity;
        ///     set => Set(ref quantity, value);
        /// }
        /// </code>
        /// </example>
        /// <param name="target">Reference to field to set.</param>
        /// <param name="value">Value to set on target field.</param>
        /// <param name="propertyName">
        /// Name of the property being set, defaults to caller's name.
        /// </param>
        protected void Set<T>(ref T target, T value, [CallerMemberName] string propertyName = "") {
            target = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
