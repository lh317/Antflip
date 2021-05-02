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
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Antflip {

    public class RelayCommand<T> : ICommand {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?> _canExecute;

        public RelayCommand(Action<T?> execute) : this(execute, (o) => true) {}

        public RelayCommand(Action<T?> execute, Predicate<T?> canExecute) {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public void Execute(T? parameter) {
            this._execute(parameter);
        }

        [DebuggerStepThrough]
        public bool CanExecute(T? parameter) {
            return this._canExecute(parameter);
        }

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter)
        {
            return this.CanExecute((T?)parameter);
        }

        [DebuggerStepThrough]
        public void Execute(object? parameter)
        {
            this.Execute((T?)parameter);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
