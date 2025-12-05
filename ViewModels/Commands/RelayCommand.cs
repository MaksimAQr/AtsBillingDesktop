using System;
using System.Windows.Input;
using System.Collections.Generic;

namespace ATS_Desktop.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    // Основной конструктор
    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Удобный конструктор для команд без параметра
    public RelayCommand(Action execute, Func<bool> canExecute = null)
        : this(_ => execute(), _ => canExecute?.Invoke() ?? true)
    {
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}