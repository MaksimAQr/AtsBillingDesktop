using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ATS_Desktop.Models;
using ATS_Desktop.Services;

namespace ATS_Desktop.ViewModels;

public class ImportExportViewModel : INotifyPropertyChanged
{
    private readonly ATS _ats;
    private readonly DataService _dataService;
    private readonly Action _refreshUIAction;
    
    private string _status = "";
    private string _importFilePath = "import_tariffs.json";
    private string _exportFilePath = "exported_tariffs.json";
    private string _importError = "";
    private string _exportError = "";
    
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    
    public string ImportFilePath
    {
        get => _importFilePath;
        set
        {
            if (SetProperty(ref _importFilePath, value))
            {
                ValidateImportPath();
                // Вызываем PropertyChanged для зависимых свойств
                RaisePropertyChanged(nameof(HasImportError));
            }
        }
    }
    
    public string ExportFilePath
    {
        get => _exportFilePath;
        set
        {
            if (SetProperty(ref _exportFilePath, value))
            {
                ValidateExportPath();
                // Вызываем PropertyChanged для зависимых свойств
                RaisePropertyChanged(nameof(HasExportError));
            }
        }
    }
    
    public string ImportError
    {
        get => _importError;
        private set
        {
            if (SetProperty(ref _importError, value))
            {
                // Вызываем PropertyChanged для зависимых свойств
                RaisePropertyChanged(nameof(HasImportError));
            }
        }
    }
    
    public string ExportError
    {
        get => _exportError;
        private set
        {
            if (SetProperty(ref _exportError, value))
            {
                // Вызываем PropertyChanged для зависимых свойств
                RaisePropertyChanged(nameof(HasExportError));
            }
        }
    }
    
    // Вычисляемые свойства
    public bool HasImportError => !string.IsNullOrEmpty(ImportError);
    public bool HasExportError => !string.IsNullOrEmpty(ExportError);
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public ImportExportViewModel(ATS ats, DataService dataService, Action refreshUIAction)
    {
        _ats = ats;
        _dataService = dataService;
        _refreshUIAction = refreshUIAction;
    }
    
    // Ваши существующие методы остаются без изменений
    public void ExecuteImport()
    {
        try
        {
            if (HasImportError)
            {
                Status = "Исправьте ошибки перед импортом";
                return;
            }
            
            if (!File.Exists(ImportFilePath))
            {
                CreateSampleImportFile(ImportFilePath);
                Status = $"Создан пример файла: {ImportFilePath}";
                ImportError = "";
                return;
            }
            
            // Проверка расширения перед выполнением
            var extension = Path.GetExtension(ImportFilePath);
            if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                ImportError = $"Неверное расширение файла: {extension}. Используйте .json";
                Status = "Ошибка: неверное расширение файла";
                return;
            }
            
            Status = "Импорт тарифов...";
            
            var importedTariffs = _dataService.ImportTariffsFromFile(ImportFilePath);
            
            if (importedTariffs.Count == 0)
            {
                Status = "Не удалось импортировать тарифы (файл пустой или неверный формат)";
                return;
            }
            
            var existingTariffs = _ats.GetTariffs().ToList();
            int addedCount = 0;
            
            foreach (var tariffInfo in importedTariffs)
            {
                if (!existingTariffs.Any(t => t.Name == tariffInfo.Name))
                {
                    if (tariffInfo.StrategyName.Contains("Preferential") && 
                        TryParseDiscount(tariffInfo.StrategyName, out double discount))
                    {
                        _ats.AddPreferentialTariff(tariffInfo.Name, tariffInfo.BaseCost, 
                                                   discount, tariffInfo.Description);
                        addedCount++;
                    }
                    else
                    {
                        _ats.AddSimpleTariff(tariffInfo.Name, tariffInfo.BaseCost, tariffInfo.Description);
                        addedCount++;
                    }
                }
            }
            
            _refreshUIAction?.Invoke();
            ImportError = "";
            Status = $"Импортировано {addedCount} новых тарифов из {importedTariffs.Count}";
        }
        catch (Exception ex)
        {
            ImportError = $"Ошибка импорта: {ex.Message}";
            Status = "Ошибка импорта";
        }
    }
    
    public void ExecuteExport()
    {
        try
        {
            if (HasExportError)
            {
                Status = "Исправьте ошибки перед экспортом";
                return;
            }
            
            var tariffs = _ats.GetTariffs();
            
            if (tariffs.Count == 0)
            {
                Status = "Нет тарифов для экспорта";
                ExportError = "Нет данных для экспорта";
                return;
            }
            
            // Проверка расширения перед выполнением
            var extension = Path.GetExtension(ExportFilePath);
            if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                ExportError = $"Неверное расширение файла: {extension}. Используйте .json";
                Status = "Ошибка: неверное расширение файла";
                return;
            }
            
            Status = "Экспорт тарифов...";
            _dataService.ExportTariffsToFile(tariffs.ToList(), ExportFilePath);
            
            ExportError = "";
            Status = $"Экспортировано {tariffs.Count} тарифов в: {ExportFilePath}";
        }
        catch (Exception ex)
        {
            ExportError = $"Ошибка экспорта: {ex.Message}";
            Status = "Ошибка экспорта";
        }
    }
    
    private void ValidateImportPath()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath))
        {
            ImportError = "Введите путь к файлу";
            return;
        }
        
        // Проверка расширения файла
        var extension = Path.GetExtension(ImportFilePath);
        if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            ImportError = $"Файл должен иметь расширение .json (текущее: {extension})";
            return;
        }
        
        // Для импорта файл должен существовать
        if (!File.Exists(ImportFilePath))
        {
            ImportError = "Файл не существует";
            return;
        }
        
        ImportError = "";
    }
    
    private void ValidateExportPath()
    {
        if (string.IsNullOrWhiteSpace(ExportFilePath))
        {
            ExportError = "Введите путь для файла";
            return;
        }
        
        // Проверка расширения файла
        var extension = Path.GetExtension(ExportFilePath);
        if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            ExportError = $"Файл должен иметь расширение .json (текущее: {extension})";
            return;
        }
        
        // Проверка директории
        var directory = Path.GetDirectoryName(ExportFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
                ExportError = "";
            }
            catch
            {
                ExportError = "Невозможно создать указанную директорию";
            }
        }
        else
        {
            ExportError = "";
        }
    }
    
    // Метод для ручного вызова валидации (например, при потере фокуса)
    public void ValidateAll()
    {
        ValidateImportPath();
        ValidateExportPath();
    }
    
    // Ваши существующие вспомогательные методы
    private bool TryParseDiscount(string strategyName, out double discount)
    {
        discount = 0;
        
        try
        {
            if (strategyName.Contains('(') && strategyName.Contains(')'))
            {
                int start = strategyName.IndexOf('(') + 1;
                int end = strategyName.IndexOf(')');
                
                if (start > 0 && end > start)
                {
                    string discountStr = strategyName.Substring(start, end - start).Replace("%", "");
                    return double.TryParse(discountStr, out discount);
                }
            }
        }
        catch
        {
        }
        
        return false;
    }
    
    private void CreateSampleImportFile(string filePath)
    {
        try
        {
            var sampleTariffs = new List<TariffInfo>
            {
                new TariffInfo
                {
                    Name = "Импортированный Базовый",
                    BaseCost = 0.09,
                    StrategyName = "Simple",
                    Description = "Импортированный базовый тариф"
                },
                new TariffInfo
                {
                    Name = "Импортированный Премиум",
                    BaseCost = 0.14,
                    StrategyName = "Preferential (25%)",
                    Description = "Импортированный премиальный тариф со скидкой 25%"
                },
                new TariffInfo
                {
                    Name = "Ночной",
                    BaseCost = 0.05,
                    StrategyName = "Simple",
                    Description = "Тариф для ночных звонков"
                }
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            string json = JsonSerializer.Serialize(sampleTariffs, options);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            ImportError = $"Ошибка создания файла: {ex.Message}";
        }
    }
    
    // Добавьте этот метод для уведомления об изменении свойств
    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // Ваш существующий SetProperty метод
    protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}