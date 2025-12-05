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
        
        TariffVM = new TariffViewModel(
        _ats, 
        () => UserVM.Tariffs,
        () => UserVM.Consumers,
        () => 
        {
            RefreshData();
            AutoSaveData();
            TariffVM.UpdateSortedTariffs();
        }
        );
        UserVM = new UserViewModel(_ats, _dataService, TariffVM);
        ImportExportVM = new ImportExportViewModel(_ats, _dataService, RefreshData);


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
        private async void InitializeDataAsync()
        {
            _isLoading = true;
            DataStatus = "Загрузка данных...";
            
            await LoadDataAsync();
            
            if (UserVM.Consumers.Count == 0 && UserVM.Tariffs.Count == 0)
            {
                RefreshData();
                DataStatus = "Созданы тестовые данные";
            }
            else
            {
                DataStatus = $"Загружено: {UserVM.Consumers.Count} потребителей, {UserVM.Tariffs.Count} тарифов";
            }
            
            _isLoading = false;
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

