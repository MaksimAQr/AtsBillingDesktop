using System.Collections.ObjectModel;

namespace ATS_Desktop.Models;

public class Consumer
{
    public string Name { get; set; } = string.Empty;
    public double TotalCost { get; set; }
    public ObservableCollection<ConsumerTariff> Tariffs { get; set; } = new ObservableCollection<ConsumerTariff>();
    
    public string displayInfo => $"{Name} - {TotalCost:C}";
}