using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ATS_Desktop.Models;
using System.Linq;
using System.Collections.Generic;

namespace ATS_Desktop.Services
{
    public class DataService
    {
        private const string DataFileName = "ats_data.json";
        private readonly string _dataFilePath;
        
        public DataService()
        {
            // Сохраняем в той же папке, где находится exe файл
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _dataFilePath = Path.Combine(appFolder, DataFileName);
            
            Console.WriteLine($"Файл данных будет сохранен в: {_dataFilePath}");
        }
        
        // Асинхронное сохранение данных
        public async System.Threading.Tasks.Task SaveDataAsync(ATS ats)
        {
            try
            {
                // Создаем модель данных из текущего состояния ATS
                var atsData = new ATSData
                {
                    Consumers = ats.GetConsumers().ToList(),
                    Tariffs = ats.GetTariffs().ToList(),
                    TariffsMap = ats.GetTariffsMap(), // Нужно добавить этот метод в ATS
                    ConsumersTariffs = ats.GetConsumersTariffs() // Нужно добавить этот метод в ATS
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(atsData, options);
                await File.WriteAllTextAsync(_dataFilePath, json);
                
                Console.WriteLine($"Данные сохранены в: {_dataFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении данных: {ex.Message}");
                throw;
            }
        }
        
        // Асинхронная загрузка данных
        public async Task<ATSData> LoadDataAsync()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    Console.WriteLine("Файл данных не найден. Создаем новую базу.");
                    return new ATSData();
                }
                
                string json = await File.ReadAllTextAsync(_dataFilePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var atsData = JsonSerializer.Deserialize<ATSData>(json, options);
                
                if (atsData == null)
                {
                    Console.WriteLine("Ошибка десериализации. Создаем новую базу.");
                    return new ATSData();
                }
                
                Console.WriteLine($"Данные загружены из: {_dataFilePath}");
                Console.WriteLine($"Загружено: {atsData.Consumers.Count} потребителей, {atsData.Tariffs.Count} тарифов");
                
                return atsData;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Ошибка формата JSON: {jsonEx.Message}");
                return new ATSData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
                return new ATSData();
            }
        }
        
        // Проверка существования файла данных
        public bool DataFileExists()
        {
            return File.Exists(_dataFilePath);
        }
        
        // Получение пути к файлу данных
        public string GetDataFilePath()
        {
            return _dataFilePath;
        }
        
        // Удаление файла данных
        public void DeleteDataFile()
        {
            if (File.Exists(_dataFilePath))
            {
                File.Delete(_dataFilePath);
                Console.WriteLine("Файл данных удален");
            }
        }

        public List<TariffInfo> ImportTariffsFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл не найден: {filePath}");
                    return new List<TariffInfo>();
                }
                
                string json = File.ReadAllText(filePath);
                Console.WriteLine($"Чтение файла импорта: {filePath}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // ВАЖНО: игнорировать регистр
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                // Пробуем десериализовать как полный ATSData
                try
                {
                    var atsData = JsonSerializer.Deserialize<ATSData>(json, options);
                    if (atsData?.Tariffs != null && atsData.Tariffs.Count > 0)
                    {
                        Console.WriteLine($"Импортировано {atsData.Tariffs.Count} тарифов из ATSData формата");
                        return atsData.Tariffs;
                    }
                }
                catch
                {
                    // Если не получилось как ATSData, пробуем как список тарифов
                }
                
                // Пробуем десериализовать как список TariffInfo
                try
                {
                    var tariffs = JsonSerializer.Deserialize<List<TariffInfo>>(json, options);
                    if (tariffs != null && tariffs.Count > 0)
                    {
                        Console.WriteLine($"Импортировано {tariffs.Count} тарифов из списка");
                        return tariffs;
                    }
                }
                catch
                {
                    // Если не получилось как список тарифов
                }
                
                // Пробуем десериализовать как одиночный TariffInfo
                try
                {
                    var tariff = JsonSerializer.Deserialize<TariffInfo>(json, options);
                    if (tariff != null)
                    {
                        Console.WriteLine($"Импортирован 1 тариф");
                        return new List<TariffInfo> { tariff };
                    }

                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"Ошибка при импорте: {ex.Message}");
                }
                
                Console.WriteLine("Не удалось распознать формат файла");
                return new List<TariffInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка импорта: {ex.Message}");
                return new List<TariffInfo>();
            }
        }

        public void ExportTariffsToFile(List<TariffInfo> tariffs, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(tariffs, options);
                File.WriteAllText(filePath, json);
                
                Console.WriteLine($"Экспортировано {tariffs.Count} тарифов в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка экспорта: {ex.Message}");
                throw;
            }
        }

        public bool IsValidTariffsFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                
                string json = File.ReadAllText(filePath);
                
                // Пробуем десериализовать
                try
                {
                    var atsData = JsonSerializer.Deserialize<ATSData>(json);
                    if (atsData?.Tariffs != null) return true;
                }
                catch { }
                
                try
                {
                    var tariffs = JsonSerializer.Deserialize<List<TariffInfo>>(json);
                    if (tariffs != null) return true;
                }
                catch { }
                
                try
                {
                    var tariff = JsonSerializer.Deserialize<TariffInfo>(json);
                    if (tariff != null) return true;
                }
                catch { }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}