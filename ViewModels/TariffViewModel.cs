using System.Windows.Input;
using ATS_Desktop.Models;
using ATS_Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ATS_Desktop.ViewModels;

public class TariffViewModel : ViewModelBase
{
    private readonly Func<ObservableCollection<TariffInfo>> _getTariffs;
    private readonly Func<ObservableCollection<Consumer>> _getConsumers;
    private readonly Action _onTariffAdded;
    private readonly ATS _ats; // Добавляем ссылку на ATS
    private bool _showAddTariffForm;
    private readonly Func<TariffInfo, Task> _saveTariffToDb;
    
    public ObservableCollection<TariffInfo> SortedTariffs { get; } = new ObservableCollection<TariffInfo>();
    
    private bool _sortDescending = true;
    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (SetProperty(ref _sortDescending, value))
            {
                UpdateSortedTariffs();
                OnPropertyChanged(nameof(SortDirectionText));
            }
        }
    }
    public string SortDirectionText => SortDescending ? "По убыванию ▼" : "По возрастанию ▲";
    public ICommand ToggleSortCommand { get; }
    
    public bool ShowAddTariffForm
    {
        get => _showAddTariffForm;
        set
        {
            _showAddTariffForm = value;
            OnPropertyChanged();
            ((RelayCommand)AddTariffCommand).RaiseCanExecuteChanged();
        }
    }
    
    public TariffEditViewModel TariffEditVM { get; }
    
    public ICommand ShowAddTariffFormCommand { get; }
    public ICommand AddTariffCommand { get; }
    public ICommand CancelAddTariffCommand { get; }
    public ICommand ToggleDescriptionCommand { get; }


    public TariffViewModel(
        ATS ats, // Добавляем параметр ATS
        Func<ObservableCollection<TariffInfo>> getTariffs, 
        Func<ObservableCollection<Consumer>> getConsumers, 
        Action onTariffAdded = null,
        Func<TariffInfo, Task> saveTariffToDb = null)
    {
        _ats = ats; // Сохраняем ATS
        _getTariffs = getTariffs;
        _getConsumers = getConsumers;
        _onTariffAdded = onTariffAdded;
        _saveTariffToDb = saveTariffToDb;
        TariffEditVM = new TariffEditViewModel();   
             
        ShowAddTariffFormCommand = new RelayCommand(() => ShowAddTariffForm = true);
        AddTariffCommand = new RelayCommand(async () => await AddTariffAsync(), CanAddTariff);        
        CancelAddTariffCommand = new RelayCommand(() => ShowAddTariffForm = false);
        ToggleSortCommand = new RelayCommand(ToggleSort);
        ToggleDescriptionCommand = new RelayCommand(() => 
        {
            TariffEditVM.ToggleDescriptionField();
            Console.WriteLine($"Поле описания: {(TariffEditVM.ShowDescriptionField ? "показано" : "скрыто")}");
        });
        TariffEditVM.PropertyChanged += (s, e) => 
        {
            ((RelayCommand)AddTariffCommand).RaiseCanExecuteChanged();
        };
    }
    
    public void UpdateSortedTariffs()
    {
        var tariffs = _getTariffs?.Invoke() ?? new ObservableCollection<TariffInfo>();
        var consumers = _getConsumers?.Invoke() ?? new ObservableCollection<Consumer>();
        
        // Сбрасываем счётчики
        foreach (var tariff in tariffs)
        {
            tariff.ConsumerCount = 0;
        }
        
        // Считаем потребителей для каждого тарифа
        foreach (var consumer in consumers)
        {
            foreach (var consumerTariff in consumer.Tariffs)
            {
                var tariff = tariffs.FirstOrDefault(t => t.Name == consumerTariff.TariffName);
                if (tariff != null)
                {
                    tariff.ConsumerCount++;
                }
            }
        }
        
        // Сортируем тарифы
        var sortedList = SortDescending
            ? tariffs.OrderByDescending(t => t.ConsumerCount).ThenBy(t => t.Name).ToList()
            : tariffs.OrderBy(t => t.ConsumerCount).ThenBy(t => t.Name).ToList();
        
        // Обновляем ObservableCollection
        SortedTariffs.Clear();
        foreach (var tariff in sortedList)
        {
            SortedTariffs.Add(tariff);
        }
    }
    
    private void ToggleSort()
    {
        SortDescending = !SortDescending;
    }
    
    private async Task AddTariffAsync()
    {
        if (string.IsNullOrWhiteSpace(TariffEditVM.Name) || TariffEditVM.Cost <= 0)
        {
            return;
        }
        
        try
        {
            TariffInfo newTariff;
            
            if (TariffEditVM.IsPreferential)
            {
                // Вызываем метод ATS для добавления льготного тарифа
                _ats.AddPreferentialTariff(
                    TariffEditVM.Name, 
                    TariffEditVM.Cost, 
                    TariffEditVM.Discount, 
                    TariffEditVM.Description);
                
                newTariff = new TariffInfo
                {
                    Name = TariffEditVM.Name,
                    BaseCost = TariffEditVM.Cost,
                    StrategyName = $"Preferential ({TariffEditVM.Discount}%)",
                    Description = TariffEditVM.Description,
                    ConsumerCount = 0
                };
                
                Console.WriteLine($"Добавлен льготный тариф: {TariffEditVM.Name}, цена: {TariffEditVM.Cost}, скидка: {TariffEditVM.Discount}%");
            }
            else
            {
                // Вызываем метод ATS для добавления простого тарифа
                _ats.AddSimpleTariff(
                    TariffEditVM.Name, 
                    TariffEditVM.Cost, 
                    TariffEditVM.Description);
                
                newTariff = new TariffInfo
                {
                    Name = TariffEditVM.Name,
                    BaseCost = TariffEditVM.Cost,
                    StrategyName = "Simple",
                    Description = TariffEditVM.Description,
                    ConsumerCount = 0
                };
                
                Console.WriteLine($"Добавлен простой тариф: {TariffEditVM.Name}, цена: {TariffEditVM.Cost}");
            }
            
            // Сохраняем в БД если передан делегат
            if (_saveTariffToDb != null)
            {
                await _saveTariffToDb(newTariff);
            }
            
            // Уведомляем о добавлении тарифа
            _onTariffAdded?.Invoke();
            
            ClearForm();
            
            // Обновляем список
            UpdateSortedTariffs();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления тарифа: {ex.Message}");
        }
    }
    
    private bool CanAddTariff()
    {
        return !string.IsNullOrWhiteSpace(TariffEditVM.Name) && 
               !TariffEditVM.HasCostError &&
               (!TariffEditVM.IsPreferential || (TariffEditVM.Discount > 0 && TariffEditVM.Discount <= 100));
    }

        private void ClearForm()
    {
        TariffEditVM.Name = string.Empty;
        TariffEditVM.Cost = 0;
        TariffEditVM.Discount = 10;
        TariffEditVM.IsPreferential = false;
        TariffEditVM.Description = string.Empty; // Очищаем описание
        TariffEditVM.ShowDescriptionField = false; // Скрываем поле
        
        Console.WriteLine("Форма очищена");
    }
}