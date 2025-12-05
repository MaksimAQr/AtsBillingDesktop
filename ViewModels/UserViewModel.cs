
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using ATS_Desktop.Models;
using ATS_Desktop.Services;


namespace ATS_Desktop.ViewModels;

public class UserViewModel : INotifyPropertyChanged
{
    private readonly ATS _ats;
    private readonly TariffViewModel _tariffVM;
    private readonly DataService _dataService;
    
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
    
    public UserViewModel(ATS ats, DataService dataService, TariffViewModel tariffVM)
    {
        _ats = ats;
        _dataService = dataService;
        _tariffVM = tariffVM;
        
        Consumers = new ObservableCollection<Consumer>();
        Tariffs = new ObservableCollection<TariffInfo>();
        
        // Инициализация команд
        AddConsumerCommand = new RelayCommand(AddNewConsumer, CanAddNewConsumer);
        DeleteConsumerCommand = new RelayCommand((param) => 
        {
            if (param is string consumerName)
                DeleteConsumer(consumerName);
        });        
        AddTariffToConsumerCommand = new RelayCommand(AddTariffToConsumer, CanAddTariffToConsumer);
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
    
    private void AddNewConsumer()
    {
        if (string.IsNullOrWhiteSpace(NewConsumerName) || SelectedTariffItem == null)
            return;
        
        _ats.AddConsumer(NewConsumerName, NewConsumerMinutes, SelectedTariff);
        
        RefreshData();
        
        // Очищаем поля
        NewConsumerName = string.Empty;
        NewConsumerMinutes = 100;
        SelectedTariffItem = null;
    }
    
    private bool CanAddNewConsumer()
    {
        return !string.IsNullOrWhiteSpace(NewConsumerName) && 
               SelectedTariffItem != null && 
               NewConsumerMinutes > 0;
    }
    
    private void DeleteConsumer(object parameter)
    {
        if (parameter is not string consumerName || string.IsNullOrWhiteSpace(consumerName))
            return;
        
        bool success = _ats.RemoveConsumer(consumerName);
        
        if (success)
        {
            Console.WriteLine($"Пользователь {consumerName} успешно удален");
            RefreshData();
            _tariffVM.UpdateSortedTariffs();
        }
    }
    
    private bool CanDeleteConsumer(object parameter)
    {
        return parameter is string consumerName && !string.IsNullOrWhiteSpace(consumerName);
    }
    
    private void AddTariffToConsumer()
    {
        if (string.IsNullOrWhiteSpace(SelectedConsumerForTariff) || 
            SelectedTariffToAdd == null || 
            MinutesToAdd <= 0)
        {
            return;
        }
        
        _ats.AddTariffForConsumer(SelectedConsumerForTariff, MinutesToAdd, SelectedTariffToAdd.Name);
        RefreshData();
        _tariffVM.UpdateSortedTariffs();
        
        // Сбрасываем значения
        MinutesToAdd = 100;
        SelectedConsumerForTariff = string.Empty;
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