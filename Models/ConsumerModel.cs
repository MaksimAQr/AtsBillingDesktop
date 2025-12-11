using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ATS_Desktop.Models;

[Table("Consumers")]
public class Consumer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "REAL")]
    public double TotalCost { get; set; }
    
    // Навигационное свойство для тарифов потребителя
    public virtual ICollection<ConsumerTariff> Tariffs { get; set; } = new List<ConsumerTariff>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [NotMapped]
    public string displayInfo => $"{Name} - {TotalCost:C}";

    // Метод для преобразования в ViewModel модель
    public Consumer ToViewModel()
    {
        var consumer = new Consumer
        {
            Name = this.Name,
            TotalCost = (double)this.TotalCost,
            Tariffs = new ObservableCollection<ConsumerTariff>()
        };
        
        foreach (var tariff in this.Tariffs)
        {
            consumer.Tariffs.Add(tariff.ToViewModel());
        }
        
        return consumer;
    }
    
    // Метод для создания из ViewModel модели
    public static Consumer FromViewModel(Consumer consumer)
    {
        var dbModel = new Consumer
        {
            Name = consumer.Name,
            TotalCost = consumer.TotalCost,
            CreatedAt = DateTime.UtcNow
        };
        
        foreach (var tariff in consumer.Tariffs)
        {
            dbModel.Tariffs.Add(ConsumerTariff.FromViewModel(tariff));
        }
        
        return dbModel;
    }
}