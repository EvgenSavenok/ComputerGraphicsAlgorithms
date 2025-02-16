using System.Windows.Input;

namespace ACG;

public class Command(Action<object?> execute, Predicate<object?>? canExecute = null)
    : ICommand
{
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object? parameter) => _execute(parameter);
}