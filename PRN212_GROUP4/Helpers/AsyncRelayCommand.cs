using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PRN212_GROUP4.Helpers
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> execute;
        private readonly Action<Exception> onError;
        private readonly Func<bool>? canExecute;

        private bool isExecuting;

        public AsyncRelayCommand(
            Func<Task> execute,
            Action<Exception> onError,
            Func<bool>? canExecute = null)
        {
            this.execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            this.onError = onError
                ?? throw new ArgumentNullException(nameof(onError));

            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !isExecuting
                && (canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
            {
                return;
            }

            isExecuting = true;

            try
            {
                RaiseCanExecuteChanged();

                await execute();
            }
            catch (Exception ex)
            {
                onError(ex);
            }
            finally
            {
                isExecuting = false;

                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
