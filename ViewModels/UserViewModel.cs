
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using ATS_Desktop.Models;
using ATS_Desktop.Services;
using System.Threading.Tasks;
using System.Linq;


namespace ATS_Desktop.ViewModels;

public class UserViewModel : INotifyPropertyChanged
{
    private readonly ATS _ats;
    private readonly TariffViewModel _tariffVM;
    private readonly DataService _dataService;

    private readonly Func<Consumer, Task> _saveConsumerToDb;
    private readonly Func<string, Task> _deleteConsumerFromDb;
    private readonly Func<string, string, int, Task> _addConsumerTariffToDb;

    
    private string _newConsumerName = string.Empty;
    private int _newConsumerMinutes = 100;
    private string _selectedTariff = string.Empty;
    private TariffInfo _selectedTariffItem;
    private string _selectedConsumerForTariff = string.Empty;
    private int _minutesToAdd = 100;
    private TariffInfo _selectedTariffToAdd;
    
    public ObservableCollection<Consumer> Consumers { get; }
    public ObservableCollection<TariffInfo> Tariffs { get; }
    
    // Properties
    public string NewConsumerName
    {
        get => _newConsumerName;
        set
        {
            if (_newConsumerName != value)
            {
                _newConsumerName = value;
                OnPropertyChanged();
                ((RelayCommand)AddConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public int NewConsumerMinutes
    {
        get => _newConsumerMinutes;
        set
        {
            if (_newConsumerMinutes != value)
            {
                _newConsumerMinutes = value;
                OnPropertyChanged();
                ((RelayCommand)AddConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public string SelectedTariff
    {
        get => _selectedTariff;
        private set
        {
            if (_selectedTariff != value)
            {
                _selectedTariff = value;
                OnPropertyChanged();
            }
        }
    }
    
    public TariffInfo SelectedTariffItem
    {
        get => _selectedTariffItem;
        set
        {
            if (_selectedTariffItem != value)
            {
                _selectedTariffItem = value;
                SelectedTariff = value?.Name ?? string.Empty;
                OnPropertyChanged();
                ((RelayCommand)AddConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public string SelectedConsumerForTariff
    {
        get => _selectedConsumerForTariff;
        set
        {
            if (_selectedConsumerForTariff != value)
            {
                _selectedConsumerForTariff = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAddTariffDialogVisible));
                ((RelayCommand)AddTariffToConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public int MinutesToAdd
    {
        get => _minutesToAdd;
        set
        {
            if (_minutesToAdd != value)
            {
                _minutesToAdd = value;
                OnPropertyChanged();
                ((RelayCommand)AddTariffToConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public TariffInfo SelectedTariffToAdd
    {
        get => _selectedTariffToAdd;
        set
        {
            if (_selectedTariffToAdd != value)
            {
                _selectedTariffToAdd = value;
                OnPropertyChanged();
                ((RelayCommand)AddTariffToConsumerCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public bool IsAddTariffDialogVisible => !string.IsNullOrEmpty(SelectedConsumerForTariff);
    
    // Commands
    public ICommand AddConsumerCommand { get; }
    public ICommand DeleteConsumerCommand { get; }
    public ICommand AddTariffToConsumerCommand { get; }
    public ICommand ShowAddTariffDialogCommand { get; }
    public ICommand CancelAddTariffDialogCommand { get; }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public UserViewModel(
        ATS ats, 
        DataService dataService, 
        TariffViewModel tariffVM,
        Func<Consumer, Task> saveConsumerToDb = null,
        Func<string, Task> deleteConsumerFromDb = null,
        Func<string, string, int, Task> addConsumerTariffToDb = null)
    {
        _ats = ats;
        _dataService = dataService;
        _tariffVM = tariffVM;
        _saveConsumerToDb = saveConsumerToDb;
        _deleteConsumerFromDb = deleteConsumerFromDb;
        _addConsumerTariffToDb = addConsumerTariffToDb;

        
        Consumers = new ObservableCollection<Consumer>();
        Tariffs = new ObservableCollection<TariffInfo>();
        
        // Инициализация команд
        AddConsumerCommand = new RelayCommand(async () => await AddNewConsumerAsync(), CanAddNewConsumer);
        DeleteConsumerCommand = new RelayCommand(async (param) => 
        {
            if (param is string consumerName)
                await DeleteConsumerAsync(consumerName);
        });        
        AddTariffToConsumerCommand = new RelayCommand(async () => await AddTariffToConsumerAsync(), CanAddTariffToConsumer);
        ShowAddTariffDialogCommand = new RelayCommand((param) =>
        {
            if (param is string consumerName && !string.IsNullOrEmpty(consumerName))
                ShowAddTariffDialog(consumerName);
        });
        CancelAddTariffDialogCommand = new RelayCommand(CancelAddTariffDialog);
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private async Task AddNewConsumerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewConsumerName) || SelectedTariffItem == null)
            return;
        
        try
        {
            // Добавляем в ATS
            _ats.AddConsumer(NewConsumerName, NewConsumerMinutes, SelectedTariff);
            
            // Получаем созданного Consumer
            var consumer = _ats.GetConsumers().LastOrDefault();
            
            // Сохраняем в БД если передан делегат
            if (consumer != null && _saveConsumerToDb != null)
            {
                await _saveConsumerToDb(consumer);
            }
            
            RefreshData();
            
            // Очищаем поля
            NewConsumerName = string.Empty;
            NewConsumerMinutes = 100;
            SelectedTariffItem = null;
            
            Console.WriteLine($"Потребитель '{consumer?.Name}' успешно добавлен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления потребителя: {ex.Message}");
        }
    }
    
    private bool CanAddNewConsumer()
    {
        return !string.IsNullOrWhiteSpace(NewConsumerName) && 
               SelectedTariffItem != null && 
               NewConsumerMinutes > 0;
    }
    
    private async Task DeleteConsumerAsync(string consumerName)
    {
        if (string.IsNullOrWhiteSpace(consumerName))
            return;
        
        try
        {
            // Удаляем из ATS
            bool success = _ats.RemoveConsumer(consumerName);
            
            if (success)
            {
                // Удаляем из БД если передан делегат
                if (_deleteConsumerFromDb != null)
                {
                    await _deleteConsumerFromDb(consumerName);
                }
                
                Console.WriteLine($"Пользователь {consumerName} успешно удален");
                RefreshData();
                _tariffVM.UpdateSortedTariffs();
            }
            else
            {
                Console.WriteLine($"Пользователь {consumerName} не найден");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления потребителя: {ex.Message}");
        }
    }
    
    private bool CanDeleteConsumer(object parameter)
    {
        return parameter is string consumerName && !string.IsNullOrWhiteSpace(consumerName);
    }
    
    private async Task AddTariffToConsumerAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedConsumerForTariff) || 
            SelectedTariffToAdd == null || 
            MinutesToAdd <= 0)
        {
            return;
        }
        
        try
        {
            // Добавляем в ATS
            _ats.AddTariffForConsumer(SelectedConsumerForTariff, MinutesToAdd, SelectedTariffToAdd.Name);
            
            // Сохраняем в БД если передан делегат
            if (_addConsumerTariffToDb != null)
            {
                await _addConsumerTariffToDb(
                    SelectedConsumerForTariff, 
                    SelectedTariffToAdd.Name, 
                    MinutesToAdd);
            }
            
            RefreshData();
            _tariffVM.UpdateSortedTariffs();
            
            Console.WriteLine($"Тариф '{SelectedTariffToAdd.Name}' добавлен потребителю '{SelectedConsumerForTariff}'");
            
            // Сбрасываем значения
            MinutesToAdd = 100;
            SelectedConsumerForTariff = string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления тарифа потребителю: {ex.Message}");
        }
    }
    
    private bool CanAddTariffToConsumer()
    {
        return !string.IsNullOrWhiteSpace(SelectedConsumerForTariff) && 
               SelectedTariffToAdd != null && 
               MinutesToAdd > 0;
    }
    
    private void ShowAddTariffDialog(string consumerName)
    {
        if (!string.IsNullOrEmpty(consumerName))
        {
            SelectedConsumerForTariff = consumerName;
            Console.WriteLine($"Добавление тарифа для пользователя: {consumerName}");
        }
    }
    
    private void CancelAddTariffDialog()
    {
        SelectedConsumerForTariff = string.Empty;
    }
    
    public void RefreshData()
    {
        Consumers.Clear();
        Tariffs.Clear();
        NewConsumerName = string.Empty;
        NewConsumerMinutes = 100;
        var newConsumers = _ats.GetConsumers();
        foreach (var consumer in newConsumers)
        {
            Consumers.Add(consumer);
        }
        
        foreach (var tariff in _ats.GetTariffs())
        {
            Tariffs.Add(tariff);
        }
        
        // Обновляем команды
        ((RelayCommand)AddConsumerCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddTariffToConsumerCommand).RaiseCanExecuteChanged();
        
        _tariffVM.UpdateSortedTariffs();
        
        // Устанавливаем выбранный тариф по умолчанию
        
        if (Tariffs.Count > 0 && string.IsNullOrEmpty(SelectedTariff))
        {
            SelectedTariff = Tariffs[0].Name;
            SelectedTariffItem = Tariffs[0];
        }
    }
    
    public void InitializeData(ObservableCollection<TariffInfo> tariffs)
    {
        Tariffs.Clear();
        foreach (var tariff in tariffs)
        {
            Tariffs.Add(tariff);
        }
    }
}