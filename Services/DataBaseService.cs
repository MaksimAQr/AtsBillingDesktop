using Microsoft.EntityFrameworkCore;
using ATS_Desktop.Data;
using ATS_Desktop.Models;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using ATS_Desktop.Services;

namespace ATS_Desktop.Services;

public class DatabaseService : IDisposable
{
    private readonly AppDbContext _context;
    
    public DatabaseService()
    {
        _context = new AppDbContext();
        
        try
        {
            // Создаем БД и таблицы если их нет
            _context.Database.EnsureCreated();
            Console.WriteLine($"База данных создана/подключена: {_context.DbPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка создания БД: {ex.Message}");
        }
    }
    
      public async Task<Consumer> SaveConsumerAsync(Consumer consumer)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            Console.WriteLine($"Сохранение потребителя '{consumer.Name}' в БД...");
            Console.WriteLine($"Количество тарифов у потребителя: {consumer.Tariffs.Count}");
            
            // Проверяем, существует ли уже такой Consumer
            var existingConsumer = await _context.Consumers
                .Include(c => c.Tariffs)
                .AsNoTracking() // Для чтения без отслеживания
                .FirstOrDefaultAsync(c => c.Name == consumer.Name);
            
            if (existingConsumer != null)
            {
                Console.WriteLine($"Потребитель '{consumer.Name}' уже существует в БД, обновляем...");
                
                // Получаем с отслеживанием для обновления
                var trackedConsumer = await _context.Consumers
                    .Include(c => c.Tariffs)
                    .FirstOrDefaultAsync(c => c.Id == existingConsumer.Id);
                
                if (trackedConsumer == null)
                {
                    throw new Exception($"Не удалось найти отслеживаемого потребителя с ID {existingConsumer.Id}");
                }
                
                // Обновляем Consumer
                trackedConsumer.TotalCost = consumer.TotalCost;
                
                // Удаляем старые тарифы
                trackedConsumer.Tariffs.Clear();
                _context.ConsumerTariffs.RemoveRange(
                    _context.ConsumerTariffs.Where(ct => ct.ConsumerId == trackedConsumer.Id));
                
                // Добавляем новые тарифы
                foreach (var tariff in consumer.Tariffs)
                {
                    Console.WriteLine($"Добавление тарифа '{tariff.TariffName}' для потребителя '{consumer.Name}'");
                    
                    // Находим Tariff в БД
                    var dbTariff = await _context.Tariffs
                        .FirstOrDefaultAsync(t => t.Name == tariff.TariffName);
                    
                    if (dbTariff == null)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: Тариф '{tariff.TariffName}' не найден в БД, пропускаем");
                        continue;
                    }
                    
                    var dbConsumerTariff = new ConsumerTariff
                    {
                        TariffName = tariff.TariffName,
                        Minutes = tariff.Minutes,
                        Cost = tariff.Cost,
                        ConsumerId = trackedConsumer.Id,
                        TariffId = dbTariff.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    
                    trackedConsumer.Tariffs.Add(dbConsumerTariff);
                }
                
                _context.Consumers.Update(trackedConsumer);
            }
            else
            {
                Console.WriteLine($"Создание нового потребителя '{consumer.Name}' в БД...");
                
                // Создаем нового Consumer
                var dbConsumer = new Consumer
                {
                    Name = consumer.Name,
                    TotalCost = consumer.TotalCost,
                    CreatedAt = DateTime.UtcNow
                };
                
                // Добавляем связи с тарифами
                foreach (var tariff in consumer.Tariffs)
                {
                    Console.WriteLine($"Добавление тарифа '{tariff.TariffName}' для нового потребителя '{consumer.Name}'");
                    
                    // Находим Tariff в БД
                    var dbTariff = await _context.Tariffs
                        .FirstOrDefaultAsync(t => t.Name == tariff.TariffName);
                    
                    if (dbTariff == null)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: Тариф '{tariff.TariffName}' не найден в БД, пропускаем");
                        continue;
                    }
                    
                    var dbConsumerTariff = new ConsumerTariff
                    {
                        TariffName = tariff.TariffName,
                        Minutes = tariff.Minutes,
                        Cost = tariff.Cost,
                        ConsumerId = dbConsumer.Id, // Пока 0, установится после SaveChanges
                        TariffId = dbTariff.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    
                    dbConsumer.Tariffs.Add(dbConsumerTariff);
                }
                
                _context.Consumers.Add(dbConsumer);
            }
            await UpdateAllTariffConsumerCountsAsync();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            Console.WriteLine($"Потребитель '{consumer.Name}' успешно сохранен в БД");
            
            return existingConsumer ?? await _context.Consumers
                .FirstOrDefaultAsync(c => c.Name == consumer.Name);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"ОШИБКА сохранения потребителя '{consumer.Name}': {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
    
    // Получить Consumer по имени
    public async Task<Consumer?> GetConsumerAsync(string name)
    {
        var dbConsumer = await _context.Consumers
            .Include(c => c.Tariffs)
            .FirstOrDefaultAsync(c => c.Name == name);
        
        return dbConsumer?.ToViewModel();
    }
    
    // Получить всех Consumer
    public async Task<List<Consumer>> GetAllConsumersAsync()
    {
        var dbConsumers = await _context.Consumers
            .Include(c => c.Tariffs)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        return dbConsumers.Select(c => c.ToViewModel()).ToList();
    }
    
    // Удалить Consumer (тарифы удалятся каскадно, но это тарифы потребителя, не общие тарифы)
    public async Task<bool> DeleteConsumerAsync(string name)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var consumer = await _context.Consumers
                .Include(c => c.Tariffs)
                .FirstOrDefaultAsync(c => c.Name == name);
                
            if (consumer == null)
                return false;
            
            // Получаем ID тарифов для обновления счетчиков
            var tariffIds = consumer.Tariffs
                .Where(ct => ct.TariffId.HasValue)
                .Select(ct => ct.TariffId.Value)
                .Distinct()
                .ToList();
            
            // Удаляем Consumer (ConsumerTariff удалятся каскадно)
            _context.Consumers.Remove(consumer);
            await _context.SaveChangesAsync();
            
            // Обновляем счетчики потребителей у тарифов
            await UpdateAllTariffConsumerCountsAsync();
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // === Вспомогательные методы ===
    
    // Проверить существование Consumer
    public async Task<bool> ConsumerExistsAsync(string name)
    {
        return await _context.Consumers
            .AnyAsync(c => c.Name == name);
    }
    
    // Получить количество Consumer
    public async Task<int> GetConsumerCountAsync()
    {
        return await _context.Consumers.CountAsync();
    }
    
    // === Методы для TariffInfo (пока простые) ===
    public async Task<ConsumerTariff> AddConsumerTariffAsync(string consumerName, ConsumerTariff consumerTariff)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Находим Consumer
            var consumer = await _context.Consumers
                .Include(c => c.Tariffs)
                .FirstOrDefaultAsync(c => c.Name == consumerName);
            
            if (consumer == null)
                throw new Exception($"Consumer '{consumerName}' не найден в БД");
            
            // Находим Tariff в БД
            var tariff = await GetTariffByNameFromDbAsync(consumerTariff.TariffName);
            if (tariff == null)
                throw new Exception($"Tariff '{consumerTariff.TariffName}' не найден в БД");
            
            // Проверяем, есть ли уже такой тариф у Consumer
            var existingTariff = consumer.Tariffs
                .FirstOrDefault(t => t.TariffName == consumerTariff.TariffName);
            
            if (existingTariff != null)
            {
                // Обновляем существующий тариф
                existingTariff.Minutes += consumerTariff.Minutes;
                existingTariff.Cost = consumerTariff.Cost;
                _context.ConsumerTariffs.Update(existingTariff);
            }
            else
            {
                // Создаем новый ConsumerTariff
                var dbConsumerTariff = ConsumerTariff.FromViewModel(consumerTariff);
                dbConsumerTariff.ConsumerId = consumer.Id;
                dbConsumerTariff.TariffId = tariff.Id;
                consumer.Tariffs.Add(dbConsumerTariff);
            }
            
            // Пересчитываем общую стоимость Consumer
            consumer.TotalCost = consumer.Tariffs.Sum(t => t.Cost);
            _context.Consumers.Update(consumer);
            
            // Обновляем счетчик ConsumerCount у тарифа
        await UpdateAllTariffConsumerCountsAsync();
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return existingTariff ?? consumer.Tariffs
                .First(t => t.TariffName == consumerTariff.TariffName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Ошибка добавления тарифа потребителю: {ex.Message}");
            throw;
        }
    }
    
    // Метод для получения Consumer по имени (добавить если нет)
    public async Task<Consumer?> GetConsumerByNameAsync(string name)
    {
        try
        {
            var dbConsumer = await _context.Consumers
                .Include(c => c.Tariffs)
                .FirstOrDefaultAsync(c => c.Name == name);
            
            return dbConsumer?.ToViewModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения потребителя: {ex.Message}");
            return null;
        }
    }
    // Сохранить TariffInfo
    public async Task<TariffInfo> SaveTariffAsync(TariffInfo tariff)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Проверяем, существует ли уже такой тариф
            var existingTariff = await _context.Tariffs
                .FirstOrDefaultAsync(t => t.Name == tariff.Name);
            
            if (existingTariff != null)
            {
                // Обновляем существующий тариф
                existingTariff.BaseCost = tariff.BaseCost;
                existingTariff.StrategyName = tariff.StrategyName;
                existingTariff.ConsumerCount = tariff.ConsumerCount;
                existingTariff.Description = tariff.Description;
                
                _context.Tariffs.Update(existingTariff);
                Console.WriteLine($"Тариф '{tariff.Name}' обновлен в БД");
            }
            else
            {
                // Создаем новый тариф
                var dbTariff = TariffInfo.FromViewModel(tariff);
                _context.Tariffs.Add(dbTariff);
                Console.WriteLine($"Тариф '{tariff.Name}' создан в БД");
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return existingTariff ?? await _context.Tariffs
                .FirstOrDefaultAsync(t => t.Name == tariff.Name);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Ошибка сохранения тарифа: {ex.Message}");
            throw;
        }
    }
    
    // Получить все TariffInfo
    public async Task<List<TariffInfo>> GetAllTariffsAsync()
    {
        try
        {
            var dbTariffs = await _context.Tariffs
                .OrderBy(t => t.Name)
                .ToListAsync();
            
            return dbTariffs.Select(t => t.ToViewModel()).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки тарифов: {ex.Message}");
            return new List<TariffInfo>();
        }
    }
    
        private async Task UpdateTariffConsumerCountAsync(int tariffId)
    {
        var tariff = await _context.Tariffs.FindAsync(tariffId);
        if (tariff != null)
        {
            tariff.ConsumerCount = await _context.ConsumerTariffs
                .Where(ct => ct.TariffId == tariffId)
                .Select(ct => ct.ConsumerId)
                .Distinct()
                .CountAsync();
            _context.Tariffs.Update(tariff);
        }
    }
    
    // Вспомогательный метод для обновления счетчиков нескольких тарифов
    private async Task UpdateTariffConsumerCountsAsync(List<int> tariffIds)
    {
        foreach (var tariffId in tariffIds)
        {
            await UpdateTariffConsumerCountAsync(tariffId);
        }
    }
    
    // Вспомогательный метод для получения Tariff из БД по имени
    private async Task<TariffInfo?> GetTariffByNameFromDbAsync(string name)
    {
        return await _context.Tariffs
            .FirstOrDefaultAsync(t => t.Name == name);
    }
    
    // Метод для обновления счетчиков всех тарифов
    public async Task UpdateAllTariffConsumerCountsAsync()
    {
        var allTariffs = await _context.Tariffs.ToListAsync();
        
        foreach (var tariff in allTariffs)
        {
            tariff.ConsumerCount = await _context.ConsumerTariffs
                .Where(ct => ct.TariffName == tariff.Name)
                .Select(ct => ct.ConsumerId)
                .Distinct()
                .CountAsync();
            _context.Tariffs.Update(tariff);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<ATSData> LoadAllDataFromDatabaseAsync()
    {
        try
        {
            var atsData = new ATSData();
            
            // Загружаем тарифы из БД
            var dbTariffs = await _context.Tariffs.ToListAsync();
            atsData.Tariffs = dbTariffs.Select(t => t.ToViewModel()).ToList();
            
            // Загружаем потребителей из БД с их тарифами
            var dbConsumers = await _context.Consumers
                .Include(c => c.Tariffs)
                .ToListAsync();
            
            // Подготавливаем структуру для ATS
            atsData.ConsumersTariffs = new Dictionary<string, List<KeyValuePair<string, int>>>();
            
            foreach (var dbConsumer in dbConsumers)
            {
                var tariffsList = new List<KeyValuePair<string, int>>();
                
                foreach (var dbTariff in dbConsumer.Tariffs)
                {
                    tariffsList.Add(new KeyValuePair<string, int>(
                        dbTariff.TariffName, 
                        dbTariff.Minutes
                    ));
                }
                
                atsData.ConsumersTariffs[dbConsumer.Name] = tariffsList;
            }
            
            // Подготавливаем TariffsMap для ATS
            atsData.TariffsMap = new Dictionary<string, List<string>>();
            
            // Собираем информацию о том, какие потребители используют какие тарифы
            foreach (var dbTariff in dbTariffs)
            {
                var consumerNames = new List<string>();
                
                // Находим всех потребителей, у которых есть этот тариф
                foreach (var dbConsumer in dbConsumers)
                {
                    if (dbConsumer.Tariffs.Any(t => t.TariffName == dbTariff.Name))
                    {
                        consumerNames.Add(dbConsumer.Name);
                    }
                }
                
                atsData.TariffsMap[dbTariff.Name] = consumerNames;
            }
            
            Console.WriteLine($"Загружено из БД: {atsData.Tariffs.Count} тарифов, {atsData.ConsumersTariffs.Count} потребителей");
            return atsData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки данных из БД: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> HasDataInDatabaseAsync()
    {
        try
        {
            // Проверяем, есть ли хоть какие-то данные в БД
            var hasTariffs = await _context.Tariffs.AnyAsync();
            var hasConsumers = await _context.Consumers.AnyAsync();
            
            return hasTariffs || hasConsumers;
        }
        catch
        {
            return false;
        }
    }
    public void Dispose()
    {
        _context?.Dispose();
    }
}