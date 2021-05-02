// Copyright (c) .NET Foundation; 2021 lh317
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
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
