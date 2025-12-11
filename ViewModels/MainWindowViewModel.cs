using System.Collections.ObjectModel;
using System.Windows.Input;
using ATS_Desktop.Models;
using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATS_Desktop.Services;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ATS_Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ATS _ats;
    private readonly DataService _dataService;
    private readonly DatabaseService _dbService;
    private bool _isLoading = false;
    
    public UserViewModel UserVM {get;}
    public TariffViewModel TariffVM { get; }
    public ImportExportViewModel ImportExportVM { get; }

   
    public ICommand RefreshDataCommand { get; }
    public ICommand ImportTariffsCommand { get; }
    public ICommand ExportTariffsCommand { get; }

    
    private string _dataStatus = "Загрузка данных...";
    public string DataStatus
    {
        get => _dataStatus;
        set => SetProperty(ref _dataStatus, value);
    }
    public event PropertyChangedEventHandler PropertyChanged;


    public MainWindowViewModel()
    {
        _ats = new ATS();
        _dataService = new DataService();
        _dbService = new DatabaseService();
        
        TariffVM = new TariffViewModel(
        _ats, 
        () => UserVM.Tariffs,
        () => UserVM.Consumers,
        () => 
        {
            RefreshData();
            AutoSaveData();
            TariffVM.UpdateSortedTariffs();
        },
        async (tariff) => 
        {
            await SaveTariffToDatabaseAsync(tariff);
        });
        UserVM = new UserViewModel(
            _ats, 
            _dataService, 
            TariffVM,
            async (consumer) => 
            {
                await SaveConsumerToDatabaseAsync(consumer);
            },
            async (consumerName) => 
            {
                await DeleteConsumerFromDatabaseAsync(consumerName);
            },
            async (consumerName, tariffName, minutes) => 
            {
                await AddConsumerTariffToDatabaseAsync(consumerName, tariffName, minutes);
            });
        ImportExportVM = new ImportExportViewModel(
            _ats, 
            _dataService, 
            RefreshData,
            async (tariff) => 
            {
                await SaveTariffToDatabaseAsync(tariff);
            });


        RefreshDataCommand = new RelayCommand(RefreshData);
        ImportTariffsCommand = new RelayCommand(
            execute: () => 
            {
                ImportExportVM.ExecuteImport();
            },
            canExecute: () => 
            {
                return !string.IsNullOrWhiteSpace(ImportExportVM.ImportFilePath) 
                    && !ImportExportVM.HasImportError;
            });
        
        ExportTariffsCommand = new RelayCommand(
            execute: () => 
            {
                ImportExportVM.ExecuteExport();
            },
            canExecute: () => 
            {
                return !string.IsNullOrWhiteSpace(ImportExportVM.ExportFilePath) 
                    && !ImportExportVM.HasExportError;
            });
        
        // Подписка на изменения свойств в ImportExportVM для обновления состояния команд
        ImportExportVM.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImportExportVM.ImportFilePath) ||
                e.PropertyName == nameof(ImportExportVM.HasImportError) ||
                e.PropertyName == nameof(ImportExportVM.ImportError))
            {
                // Обновляем состояние команды импорта
                var importCommand = ImportTariffsCommand as RelayCommand;
                importCommand?.RaiseCanExecuteChanged();
            }
            
            if (e.PropertyName == nameof(ImportExportVM.ExportFilePath) ||
                e.PropertyName == nameof(ImportExportVM.HasExportError) ||
                e.PropertyName == nameof(ImportExportVM.ExportError))
            {
                // Обновляем состояние команды экспорта
                var exportCommand = ExportTariffsCommand as RelayCommand;
                exportCommand?.RaiseCanExecuteChanged();
            }
        };
        _ats.DataChanged += async (sender, e) => await AutoSaveDataAsync();

        InitializeDataAsync();
        RefreshData();
    }

    private void InitializeTestData()
    {
    _ats.AddSimpleTariff("Базовый", 0.10, 
        "Базовый тариф для домашнего использования. Хорошее качество связи по доступной цене.");
    
    _ats.AddPreferentialTariff("Премиум", 0.15, 20,
        "Премиальный тариф с повышенным качеством связи и приоритетным обслуживанием. Включает дополнительные услуги.");
    
    _ats.AddPreferentialTariff("Корпоративный", 0.12, 15,
        "Специальный тариф для бизнес-клиентов. Групповые скидки, расширенная поддержка, индивидуальные условия.");
    
    _ats.AddSimpleTariff("Эконом", 0.08);
        
        _ats.AddConsumer("Иван Иванов", 100, "Базовый");
        _ats.AddConsumer("Петр Петров", 200, "Премиум");
        
        RefreshData();

    }

// Метод для сохранения Consumer в БД (с детальным логированием)
private async Task SaveConsumerToDatabaseAsync(Consumer consumer)
{
    if (_isLoading) return;
    
    try
    {
        Console.WriteLine($"=== Начало сохранения потребителя '{consumer.Name}' в БД ===");
        Console.WriteLine($"TotalCost: {consumer.TotalCost}");
        Console.WriteLine($"Количество тарифов: {consumer.Tariffs.Count}");
        
        foreach (var tariff in consumer.Tariffs)
        {
            Console.WriteLine($"  Тариф: {tariff.TariffName}, Минут: {tariff.Minutes}, Стоимость: {tariff.Cost}");
        }
        
        await _dbService.SaveConsumerAsync(consumer);
        Console.WriteLine($"Потребитель '{consumer.Name}' успешно сохранен в БД");
        Console.WriteLine($"=== Конец сохранения ===\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ОШИБКА сохранения потребителя '{consumer.Name}' ===");
        Console.WriteLine($"Сообщение: {ex.Message}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
            
            if (ex.InnerException.InnerException != null)
            {
                Console.WriteLine($"Внутреннее-внутреннее исключение: {ex.InnerException.InnerException.Message}");
            }
        }
        
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        Console.WriteLine($"=== Конец ошибки ===\n");
    }
}
    private async Task DeleteConsumerFromDatabaseAsync(string consumerName)
    {
        if (_isLoading) return;
        
        try
        {
            bool deleted = await _dbService.DeleteConsumerAsync(consumerName);
            if (deleted)
            {
                Console.WriteLine($"Потребитель '{consumerName}' удален из БД");
            }
            else
            {
                Console.WriteLine($"Потребитель '{consumerName}' не найден в БД");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления потребителя из БД: {ex.Message}");
        }
    }
    private async Task SaveTariffToDatabaseAsync(TariffInfo tariff)
    {
        if (_isLoading) return;
        
        try
        {
            await _dbService.SaveTariffAsync(tariff);
            Console.WriteLine($"Тариф '{tariff.Name}' сохранен в БД");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения тарифа в БД: {ex.Message}");
        }
    }
    private async Task AddConsumerTariffToDatabaseAsync(string consumerName, string tariffName, int minutes)
    {
        if (_isLoading) return;
        
        try
        {
            // Находим Consumer и Tariff в ATS для расчета стоимости
            var consumer = _ats.GetConsumers().FirstOrDefault(c => c.Name == consumerName);
            var tariff = _ats.GetTariffs().FirstOrDefault(t => t.Name == tariffName);
            
            if (consumer == null || tariff == null)
            {
                Console.WriteLine($"Consumer '{consumerName}' или Tariff '{tariffName}' не найдены в ATS");
                return;
            }
            
            // Создаем ConsumerTariff
            var consumerTariff = new ConsumerTariff
            {
                TariffName = tariffName,
                Minutes = minutes,
                Cost = tariff.BaseCost * minutes
            };
            
            await _dbService.AddConsumerTariffAsync(consumerName, consumerTariff);
            
            // Обновляем счетчик в ATS
            tariff.ConsumerCount = await GetConsumerCountForTariffAsync(tariffName);
            
            Console.WriteLine($"Тариф '{tariffName}' добавлен потребителю '{consumerName}' в БД. Потребителей: {tariff.ConsumerCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления тарифа потребителю в БД: {ex.Message}");
        }
    }
    private async Task<int> GetConsumerCountForTariffAsync(string tariffName)
    {
        try
        {
            // Можно получить из БД или посчитать в ATS
            return _ats.GetConsumers()
                .Count(c => c.Tariffs.Any(t => t.TariffName == tariffName));
        }
        catch
        {
            return 0;
        }
    }
    private async void InitializeDataAsync()
    {
        _isLoading = true;
        DataStatus = "Загрузка данных...";
        
        try
        {
            // Пытаемся загрузить из БД
            await LoadDataFromDatabaseAsync();
            DataStatus = $"Данные загружены из БД: {DateTime.Now:HH:mm:ss}";
            Console.WriteLine("Данные успешно загружены из БД!");
        }
        catch (Exception dbEx)
        {
            Console.WriteLine($"Ошибка загрузки из БД: {dbEx.Message}");
            await LoadDataAsync();
        }
        finally
        {
            if (UserVM.Consumers.Count == 0 && UserVM.Tariffs.Count == 0)
            {
                RefreshData();
                DataStatus = "Созданы тестовые данные";
            }
            else
            {
                DataStatus = $"Загружено: {UserVM.Consumers.Count} потребителей, {UserVM.Tariffs.Count} тарифов";
            }
        } 

        
        _isLoading = false;
    }
    private async Task LoadDataFromDatabaseAsync()
    {
        DataStatus = "Загрузка из базы данных...";
        
        // Проверяем, есть ли данные в БД
        bool hasData = await _dbService.HasDataInDatabaseAsync();
        
        if (!hasData)
        {
            // Если данных нет, инициализируем тестовыми данными
            Console.WriteLine("No data in Db");
        }
        
        // Загружаем данные из БД
        var atsData = await _dbService.LoadAllDataFromDatabaseAsync();
        
        if (atsData != null)
        {
            _ats.LoadFromATSData(atsData);
        }
        else
        {
            throw new Exception("Не удалось загрузить данные из БД");
        }
    }
        private async System.Threading.Tasks.Task SaveDataAsync()
        {
            if (_isLoading) return;
            
            try
            {
                DataStatus = "Сохранение данных...";
                await _dataService.SaveDataAsync(_ats);
                DataStatus = $"Данные сохранены: {DateTime.Now:HH:mm:ss}";
                
                Console.WriteLine("Данные успешно сохранены!");
            }
            catch (Exception ex)
            {
                DataStatus = $"Ошибка сохранения: {ex.Message}";
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                DataStatus = "Загрузка данных...";
                var atsData = await _dataService.LoadDataAsync();
                
                if (atsData != null)
                {
                    _ats.LoadFromATSData(atsData);
                    RefreshData();
                    DataStatus = $"Данные загружены: {DateTime.Now:HH:mm:ss}";
                    
                    Console.WriteLine("Данные успешно загружены!");
                }
            }
            catch (Exception ex)
            {
                DataStatus = $"Ошибка загрузки: {ex.Message}";
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                
                InitializeTestData();
                RefreshData();
            }
        }

        private async void AutoSaveData()
        {
            if (_isLoading) return;
            
            try
            {
                await Task.Delay(1000); 
                await SaveDataAsync();
            }
            catch
            {
            }
        }

        private async System.Threading.Tasks.Task AutoSaveDataAsync()
        {
            if (_isLoading) return;
            
            try
            {
                await Task.Delay(1000);
                await _dataService.SaveDataAsync(_ats);
                Console.WriteLine("Автосохранение выполнено");
            }
            catch
            {
            }
        }

        private void ResetData()
        {
            UserVM.Consumers.Clear();
            UserVM.Tariffs.Clear();
            
            _ats.LoadFromATSData(new ATSData());
            
            InitializeTestData();
            RefreshData();
            
            AutoSaveData();
            
            DataStatus = "Данные сброшены до начального состояния";
        }
    
    public void RefreshData()
    {
        
        UserVM.RefreshData();
    }
        
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

