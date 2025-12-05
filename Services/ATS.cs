using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using ATS_Desktop.Models.BillingStrategy;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ATS_Desktop.Models;

public class ATS
{
    private List<Tariff> tariffs;
    private Dictionary<string, List<string>> tariffs_map;
    private Dictionary<string, List<KeyValuePair<string, int>>> consumers;
    public event EventHandler DataChanged;

    public ATS()
    {
        tariffs = new List<Tariff>();
        tariffs_map = new Dictionary<string, List<string>>();
        consumers = new Dictionary<string, List<KeyValuePair<string, int>>>();
    }

    public void OnDataChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddSimpleTariff(string name, double cost, string description = "")
    {
        // Проверяем, нет ли уже такого тарифа
        if (!tariffs.Any(t => t.name == name))
        {
            tariffs.Add(new Tariff(name, cost, new RegularBilling(), description));
            tariffs_map.Add(name, new List<string>());
            OnDataChanged();
            Console.WriteLine($"Добавлен простой тариф '{name}' с описанием: {(!string.IsNullOrEmpty(description) ? "Да" : "Нет")}");
        }
        else
        {
            Console.WriteLine($"Тариф '{name}' уже существует");
        }
    }

    public void AddPreferentialTariff(string name, double cost, double discount, string description = "")
    {
        // Проверяем, нет ли уже такого тарифа
        if (!tariffs.Any(t => t.name == name))
        {
            double discountedCost = cost * (100 - discount) / 100;
            tariffs.Add(new Tariff(name, discountedCost, new PreferentialBilling(discount), description));
            tariffs_map.Add(name, new List<string>());
            OnDataChanged();
            Console.WriteLine($"Добавлен льготный тариф '{name}' со скидкой {discount}% и описанием: {(!string.IsNullOrEmpty(description) ? "Да" : "Нет")}");
        }
        else
        {
            Console.WriteLine($"Тариф '{name}' уже существует");
        }
    }
        public void AddDescriptionToTariff(string tariffName, string description)
    {
        var tariff = tariffs.FirstOrDefault(t => t.name == tariffName);
        if (tariff != null)
        {
            tariff.description = description;
            Console.WriteLine($"Добавлено описание к тарифу '{tariffName}'");
        }
        else
        {
            Console.WriteLine($"Тариф '{tariffName}' не найден");
        }
    }
        public void UpdateTariffDescription(string tariffName, string description)
    {
        var tariff = tariffs.FirstOrDefault(t => t.name == tariffName);
        if (tariff != null)
        {
            tariff.description = description;
            Console.WriteLine($"Обновлено описание тарифа '{tariffName}'");
        }
        else
        {
            Console.WriteLine($"Тариф '{tariffName}' не найден");
        }
    }

    public void AddConsumer(string consumerName, int minutes, string tariffName)
    {
        
        if(!TariffCheck(tariffName))
        {
            return;
        }
        
        // Проверяем, нет ли уже такого пользователя
        if (consumers.ContainsKey(consumerName))
        {
        
            return;
        }
        
        consumers.Add(consumerName, new List<KeyValuePair<string, int>>());
        consumers[consumerName].Add(new KeyValuePair<string, int>(tariffName, minutes));
        tariffs_map[tariffName].Add(consumerName);
        OnDataChanged();
    }

    public bool RemoveConsumer(string consumerName)
    {
        if (!consumers.ContainsKey(consumerName)) return false;
        
        // Удаляем потребителя из tariffs_map
        foreach (var tariffUsage in consumers[consumerName])
        {
            if (tariffs_map.ContainsKey(tariffUsage.Key))
            {
                tariffs_map[tariffUsage.Key].Remove(consumerName);
            }
        }
        OnDataChanged();
        return consumers.Remove(consumerName);
    }

    private bool TariffCheck(string tariffName)
    {
        return tariffs.Exists(tariff => tariff.getName() == tariffName);
    }

    public void AddTariffForConsumer(string consumerName, int minutes, string tariffName)
    {
        if(!TariffCheck(tariffName) || !consumers.ContainsKey(consumerName))
        {
            return;
        }
        consumers[consumerName].Add(new KeyValuePair<string, int>(tariffName, minutes));
        tariffs_map[tariffName].Add(consumerName);
        OnDataChanged();
    }

    public void AddConsumerMinutes(string consumerName, int minutes, string tariffName)
    {
        if(!TariffCheck(tariffName) || !consumers.ContainsKey(consumerName))
        {
            return;
        }
        
        var tariffIndex = consumers[consumerName].FindIndex(tariff => tariff.Key == tariffName);
        if(tariffIndex == -1)
        {
            return;
        }
        
        var currentPair = consumers[consumerName][tariffIndex];
        consumers[consumerName][tariffIndex] = new KeyValuePair<string, int>(currentPair.Key, currentPair.Value + minutes);
    }

    public double CalculateTotalRevenue(string consumerName)
    {
        if(!consumers.ContainsKey(consumerName))
        {
            return double.NaN;
        }
        
        double totalCost = 0.0;
        foreach(var tariffUsage in consumers[consumerName])
        {
            var tariff = tariffs.Find(t => t.getName() == tariffUsage.Key);
            totalCost += tariff?.calculateCost(tariffUsage.Value) ?? 0;
        }
        return totalCost;
    }

    public double CalculateConsumerTariffCost(string consumerName, string tariffName)
    {
        if(!consumers.ContainsKey(consumerName))
        {
            return double.NaN;
        }
        
        var tariffUsage = consumers[consumerName].Find(tariff => tariff.Key == tariffName);
        if(tariffUsage.Equals(default(KeyValuePair<string, int>)))
        {
            return double.NaN;
        }
        
        var tariff = tariffs.Find(t => t.getName() == tariffName);
        return tariff?.calculateCost(tariffUsage.Value) ?? double.NaN;
    }

public ObservableCollection<Consumer> GetConsumers()
{
    var consumerCollection = new ObservableCollection<Consumer>();
    
    foreach(var consumer in consumers)
    {
    
        var consumerModel = new Consumer
        {
            Name = consumer.Key,
            TotalCost = CalculateTotalRevenue(consumer.Key),
            Tariffs = new ObservableCollection<ConsumerTariff>(
                consumer.Value.Select(t => new ConsumerTariff
                {
                    TariffName = t.Key,
                    Minutes = t.Value,
                    Cost = CalculateConsumerTariffCost(consumer.Key, t.Key)
                })
            )
        };
        
        consumerCollection.Add(consumerModel);
    
    }
    return consumerCollection;
}
    public Dictionary<string, List<string>> GetTariffsMap()
    {
        return new Dictionary<string, List<string>>(tariffs_map);
    }
        
    public Dictionary<string, List<KeyValuePair<string, int>>> GetConsumersTariffs()
    {
        return new Dictionary<string, List<KeyValuePair<string, int>>>(consumers);
    }

    public ObservableCollection<string> GetConsumerNames()
    {
        return new ObservableCollection<string>(consumers.Keys);
    }

    public int GetConsumersCount() => consumers.Count;

    public List<int> GetConsumerMinutes(string consumerName)
    {
        if(!consumers.ContainsKey(consumerName))
        {
            return new List<int>();
        }
        
        return consumers[consumerName].Select(tariffUsage => tariffUsage.Value).ToList();
    }

    public ObservableCollection<TariffInfo> GetTariffs()
    {
        var tariffCollection = new ObservableCollection<TariffInfo>();
        
        foreach(var tariff in tariffs)
        {
            var tariffInfo = new TariffInfo
            {
                Name = tariff.getName(),
                BaseCost = tariff.getBaseCost(),
                StrategyName = tariff.getStrategyName(),
                ConsumerCount = tariffs_map[tariff.getName()]?.Count ?? 0,
                Description = tariff.description
            };
            
            tariffCollection.Add(tariffInfo);
        }
        
        return tariffCollection;
    }

    public void DisplayTariffsInfo()
    {
        Console.WriteLine("\n=== ALL TARIFFS ===");
        foreach(var tariff in tariffs)
        {
            tariff.displayInfo();
        }
    }

    public void DisplayConsumersInfo()
    {
        Console.WriteLine("\n=== ALL CONSUMERS ===");
        foreach(var consumer in consumers)
        {
            Console.WriteLine($"Consumer: {consumer.Key}");
            foreach(var tariffUsage in consumer.Value)
            {
                Console.WriteLine($"\tTariff: {tariffUsage.Key} | Minutes: {tariffUsage.Value}");
            }
        }
    }

public void LoadFromATSData(ATSData atsData)
{
    // Очищаем текущие данные
    tariffs.Clear();
    tariffs_map.Clear();
    consumers.Clear();
    
    // Восстанавливаем тарифы
    foreach (var tariffInfo in atsData.Tariffs)
    {
        // Определяем тип тарифа по StrategyName
        if (tariffInfo.StrategyName.Contains("Preferential"))
        {
            try
            {
                // Извлекаем скидку из названия
                string discountStr = "20"; // значение по умолчанию
                if (tariffInfo.StrategyName.Contains('(') && tariffInfo.StrategyName.Contains(')'))
                {
                    int start = tariffInfo.StrategyName.IndexOf('(') + 1;
                    int end = tariffInfo.StrategyName.IndexOf(')');
                    if (start < end)
                    {
                        discountStr = tariffInfo.StrategyName.Substring(start, end - start).Replace("%", "");
                    }
                }
                
                if (double.TryParse(discountStr, out double discount))
                {
                    // Просто используем сохраненную стоимость
                    tariffs.Add(new Tariff(tariffInfo.Name, tariffInfo.BaseCost, 
                        new PreferentialBilling(discount), tariffInfo.Description));
                    
                    if (!tariffs_map.ContainsKey(tariffInfo.Name))
                    {
                        tariffs_map[tariffInfo.Name] = new List<string>();
                    }
                }
            }
            catch
            {
                // В случае ошибки добавляем как простой тариф
                tariffs.Add(new Tariff(tariffInfo.Name, tariffInfo.BaseCost, 
                    new RegularBilling(), tariffInfo.Description));
                
                if (!tariffs_map.ContainsKey(tariffInfo.Name))
                {
                    tariffs_map[tariffInfo.Name] = new List<string>();
                }
            }
        }
        else
        {
            // Простой тариф
            tariffs.Add(new Tariff(tariffInfo.Name, tariffInfo.BaseCost, 
                new RegularBilling(), tariffInfo.Description));
            
            if (!tariffs_map.ContainsKey(tariffInfo.Name))
            {
                tariffs_map[tariffInfo.Name] = new List<string>();
            }
        }
    }
    
    // Восстанавливаем связи
    foreach (var entry in atsData.TariffsMap)
    {
        if (!tariffs_map.ContainsKey(entry.Key))
        {
            tariffs_map[entry.Key] = new List<string>();
        }
        tariffs_map[entry.Key].AddRange(entry.Value);
    }
    
    // Восстанавливаем потребителей
    foreach (var entry in atsData.ConsumersTariffs)
    {
        if (!consumers.ContainsKey(entry.Key))
        {
            consumers[entry.Key] = new List<KeyValuePair<string, int>>();
        }
        consumers[entry.Key].AddRange(entry.Value);
    }
    
    Console.WriteLine($"Данные загружены: {tariffs.Count} тарифов, {consumers.Count} потребителей");
}
}
