using ATS_Desktop.Models;
using System;

namespace ATS_Desktop.ViewModels
{
    public class TariffDetailsViewModel : ViewModelBase
    {
        private readonly TariffInfo _tariff;
        private readonly string _description;
        private readonly bool _isStatistics;
        
        public string WindowTitle { get; }
        public string Name => _tariff.Name;
        public string TariffType => GetTariffType(_tariff);
        public string Price => $"{_tariff.BaseCost:C2}/минуту";
        public string Consumers => $"{_tariff.ConsumerCount} потребителей";
        public string Description => _description;
        public bool HasDescription => !string.IsNullOrEmpty(_description);
        public bool IsStatistics => _isStatistics;
        
        // Конструктор 1: С описанием
        public TariffDetailsViewModel(TariffInfo tariff, string description)
        {
            _tariff = tariff;
            _description = description;
            _isStatistics = false;
            WindowTitle = $"Тариф: {tariff.Name} (с описанием)";
        }
        
        // Конструктор 2: Без описания
        public TariffDetailsViewModel(TariffInfo tariff) : this(tariff, string.Empty)
        {
            WindowTitle = $"Тариф: {tariff.Name}";
        }
        
        // Конструктор 3: Только статистика
        public TariffDetailsViewModel(string tariffName, int consumerCount, double price)
        {
            _tariff = new TariffInfo
            {
                Name = tariffName,
                ConsumerCount = consumerCount,
                BaseCost = price,
                StrategyName = "Simple"
            };
            _description = $"Статистика: {consumerCount} потребителей, средняя цена {price:C2}";
            _isStatistics = true;
            WindowTitle = $"Статистика: {tariffName}";
        }
        
        private string GetTariffType(TariffInfo tariff)
        {
            if (tariff.StrategyName.Contains("Preferential"))
                return "Льготный тариф";
            else if (tariff.StrategyName == "Regular")
                return "Обычный тариф";
            else
                return tariff.StrategyName;
        }
    }
}