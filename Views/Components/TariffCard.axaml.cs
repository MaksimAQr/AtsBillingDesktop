using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using ATS_Desktop.Models;
using ATS_Desktop.Views;
using ATS_Desktop.Views.Pages;
using System;

namespace ATS_Desktop.Views.Components;

// TariffCard.axaml.cs
public partial class TariffCard : UserControl
{
    public TariffCard()
    {
        InitializeComponent();
    }
    
private void OnTariffCardClick(object sender, RoutedEventArgs e)
{
    if (DataContext is TariffInfo tariff)
    {
        // Проверяем, есть ли описание
        if (!string.IsNullOrEmpty(tariff.Description))
        {
            // Используем конструктор с описанием
            new TariffDetailsWindow(tariff, tariff.Description).Show();
        }
        else
        {
            // Используем конструктор без описания
            new TariffDetailsWindow(tariff).Show();
        }
    }
}
}