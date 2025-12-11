using Microsoft.EntityFrameworkCore;
using ATS_Desktop.Models;
using System;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ATS_Desktop.Data;

public class AppDbContext : DbContext
{
    public DbSet<Consumer> Consumers { get; set; }
    public DbSet<TariffInfo> Tariffs { get; set; }
    public DbSet<ConsumerTariff> ConsumerTariffs { get; set; }
    
    public string DbPath { get; }
    
    public AppDbContext()
    {
        // Сохраняем БД в папке приложения
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "ATS_Desktop");
        
        // Создаем папку, если не существует
        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);    
        
        DbPath = Path.Combine(appFolder, "ats_database.db");
        Console.WriteLine($"База данных будет создана по пути: {DbPath}");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
        
        #if DEBUG
        options.EnableSensitiveDataLogging()
               .EnableDetailedErrors();
        #endif
    }
    
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Настройка ConsumerDbModel
    modelBuilder.Entity<Consumer>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.TotalCost).HasColumnType("REAL");
        entity.HasIndex(e => e.Name).IsUnique();
        
        // Связь с ConsumerTariffDbModel (один ко многим)
        entity.HasMany(c => c.Tariffs)
              .WithOne(ct => ct.Consumer)
              .HasForeignKey(ct => ct.ConsumerId)
              .OnDelete(DeleteBehavior.Cascade);
    });
    
    // Настройка TariffInfoDbModel
    modelBuilder.Entity<TariffInfo>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.BaseCost).HasColumnType("REAL");
        entity.Property(e => e.StrategyName).HasMaxLength(50);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.HasIndex(e => e.Name).IsUnique();
        
        // Связь с ConsumerTariffDbModel (один ко многим)
        entity.HasMany(t => t.ConsumerTariffs)
              .WithOne(ct => ct.Tariff)
              .HasForeignKey(ct => ct.TariffId)
              .OnDelete(DeleteBehavior.SetNull);
    });
    
    // Настройка ConsumerTariffDbModel
    modelBuilder.Entity<ConsumerTariff>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TariffName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Cost).HasColumnType("REAL");
        
        // Составной индекс для предотвращения дублирования тарифа у одного потребителя
        entity.HasIndex(e => new { e.ConsumerId, e.TariffName }).IsUnique();
        
        // Индексы для быстрого поиска
        entity.HasIndex(e => e.ConsumerId);
        entity.HasIndex(e => e.TariffId);
        entity.HasIndex(e => e.TariffName);
    });
}
}