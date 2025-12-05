using Avalonia.Controls;
using ATS_Desktop.Models;
using ATS_Desktop.ViewModels;
using System;

namespace ATS_Desktop.Views.Pages
{
    public partial class TariffDetailsWindow : Window
    {
        // Конструктор 1: С описанием
        public TariffDetailsWindow(TariffInfo tariff, string description)
        {
            InitializeComponent();
            DataContext = new TariffDetailsViewModel(tariff, description);
            
            Width = 500;
            Height = 450;
        }
        
        // Конструктор 2: Без описания
        public TariffDetailsWindow(TariffInfo tariff)
        {
            InitializeComponent();
            DataContext = new TariffDetailsViewModel(tariff);
            
            Width = 400;
            Height = 300;
        }
        
        // Конструктор 3: Только статистика
        public TariffDetailsWindow(string tariffName, int consumerCount, double price)
        {
            InitializeComponent();
            DataContext = new TariffDetailsViewModel(tariffName, consumerCount, price);
            
            Width = 400;
            Height = 350;
        }
        
        private void CloseButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
        
    }
}