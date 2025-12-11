using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System;

namespace ATS_Desktop.Models;
[Table("ConsumerTariffs")]
public class ConsumerTariff
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TariffName { get; set; } = string.Empty;
    
    [Required]
    public int Minutes { get; set; }
    
    [Required]
    [Column(TypeName = "REAL")]
    public double Cost { get; set; }
    
    // Внешний ключ для Consumer
    public int ConsumerId { get; set; }
    public int? TariffId { get; set; }    
    [ForeignKey("ConsumerId")]
    public virtual Consumer Consumer { get; set; } = null!;
    
    [ForeignKey("TariffId")]
    public virtual TariffInfo? Tariff { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    [NotMapped]
    public string displayInfo => $"{TariffName}: {Minutes}min - {Cost:C}";

    // Метод для преобразования в ViewModel модель
    public ConsumerTariff ToViewModel()
    {
        return new ConsumerTariff
        {
            TariffName = this.TariffName,
            Minutes = this.Minutes,
            Cost = this.Cost
        };
    }
    
    // Метод для создания из ViewModel модели
    public static ConsumerTariff FromViewModel(ConsumerTariff tariff)
    {
        return new ConsumerTariff
        {
            TariffName = tariff.TariffName,
            Minutes = tariff.Minutes,
            Cost = tariff.Cost,
            AssignedAt = DateTime.UtcNow
        };
    }
}