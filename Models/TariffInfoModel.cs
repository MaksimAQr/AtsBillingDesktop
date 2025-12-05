using System;

namespace ATS_Desktop.Models;

public class TariffInfo
{
    public string Name { get; set; } = string.Empty;
    public double BaseCost { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public int ConsumerCount { get; set; }
    public string Description { get; set; } = string.Empty; // Новое поле
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public string displayInfo => $"{Name} ({StrategyName}) - {BaseCost:C}/min - {ConsumerCount} users";
}