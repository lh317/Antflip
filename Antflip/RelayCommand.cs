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
