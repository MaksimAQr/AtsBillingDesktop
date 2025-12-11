using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ATS_Desktop.Models;

[Table("Tariffs")]
public class TariffInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "REAL")]
    public double BaseCost { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string StrategyName { get; set; } = "Simple";
    
    public int ConsumerCount { get; set; }
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [NotMapped]
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ConsumerTariff> ConsumerTariffs { get; set; } = new List<ConsumerTariff>();

    [NotMapped]
    public string displayInfo => $"{Name} ({StrategyName}) - {BaseCost:C}/min - {ConsumerCount} users";
    // Метод для преобразования в ViewModel модель
    public TariffInfo ToViewModel()
    {
        return new TariffInfo
        {
            Name = this.Name,
            BaseCost = this.BaseCost,
            StrategyName = this.StrategyName,
            ConsumerCount = this.ConsumerCount,
            Description = this.Description
        };
    }
    
    // Метод для создания из ViewModel модели
    public static TariffInfo FromViewModel(TariffInfo tariff)
    {
        return new TariffInfo
        {
            Name = tariff.Name,
            BaseCost = (double)tariff.BaseCost,
            StrategyName = tariff.StrategyName,
            ConsumerCount = tariff.ConsumerCount,
            Description = tariff.Description,
            CreatedAt = DateTime.UtcNow
        };
    }
}