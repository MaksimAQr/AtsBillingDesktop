// ATS_Desktop/ViewModels/TariffEditViewModel.cs (ViewModel для редактирования)
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using ATS_Desktop.Models;
using System.ComponentModel.DataAnnotations;
using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows.Input;



namespace ATS_Desktop.ViewModels;

public class TariffEditViewModel : INotifyPropertyChanged
{
    private TariffInfo? _tariff;
    
    // Редактируемые свойства
    private string _name = string.Empty;
    private double _cost;
    private bool _isPreferential;
    private double _discount = 10;
    private string _description = string.Empty;
    private bool _showDescriptionField = false;
    private string _costError = string.Empty;
    private string _discountError = string.Empty;
    
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }
    
    public double Cost
    {
        get => _cost;
        set {
            if(SetField(ref _cost, value))
            {
                ValidateCost(value);
                OnPropertyChanged(nameof(HasCostError));
                OnPropertyChanged(nameof(CostError));
            }
        }
    }
    
    public bool IsPreferential
    {
        get => _isPreferential;
        set => SetField(ref _isPreferential, value);
    }
    
    public double Discount
    {
        get => _discount;
        set => SetField(ref _discount, value);
    }
    
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }
    
    public bool ShowDescriptionField
    {
        get => _showDescriptionField;
        set => SetField(ref _showDescriptionField, value);
    }
    public string CostError
    {
        get => _costError;
        private set => SetField(ref _costError, value);
    }
    
    public string DiscountError
    {
        get => _discountError;
        private set => SetField(ref _discountError, value);
    }
    
    public bool HasCostError => !string.IsNullOrEmpty(CostError);
    public bool HasDiscountError => !string.IsNullOrEmpty(DiscountError);
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public int DescriptionLength => Description?.Length ?? 0;
    
    // Методы
    public void LoadFromTariff(TariffInfo tariff)
    {
        _tariff = tariff;
        Name = tariff.Name;
        Cost = tariff.BaseCost;
        Description = tariff.Description;
        IsPreferential = tariff.StrategyName.Contains("Preferential");
    }
    
    public TariffInfo CreateTariff()
    {
        return new TariffInfo
        {
            Name = Name,
            BaseCost = Cost,
            Description = Description,
            StrategyName = IsPreferential ? $"Preferential ({Discount}%)" : "Simple"
        };
    }
        private void ValidateCost(double value)
    {
        // Преобразуем в строку для проверки знаков после запятой
        string strValue = value.ToString(CultureInfo.InvariantCulture);
        if (value > (double)int.MaxValue || value < 0)
        {
            CostError = "Стоимость должна быть в диапазоне от 0 до 2147483647.";
            return;
        }
        if (strValue.Contains('.'))
        {
            string decimalPart = strValue.Split('.')[1];
            if (decimalPart.Length > 7)
            {
                CostError = "Стоимость не может иметь более 7 знаков после запятой.";
                return;
            }
        }
        CostError = string.Empty;
    }
    public void Reset()
    {
        Name = string.Empty;
        Cost = 0;
        IsPreferential = false;
        Discount = 10;
        Description = string.Empty;
        ShowDescriptionField = false;
        _tariff = null;
    }
    
    public void ToggleDescriptionField()
    {
        ShowDescriptionField = !ShowDescriptionField;
    }
    
    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}