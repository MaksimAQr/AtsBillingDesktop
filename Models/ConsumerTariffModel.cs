
namespace ATS_Desktop.Models;
public class ConsumerTariff
{
    public string TariffName { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public double Cost { get; set; }
    
    public string displayInfo => $"{TariffName}: {Minutes}min - {Cost:C}";
}