using Avalonia;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ATS_Desktop.Models;
using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATS_Desktop.Services;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ATS_Desktop.Views.Components;

public partial class AddConsumerPanel : UserControl
{
    public static readonly StyledProperty<ICommand> RefreshCommandProperty =
        AvaloniaProperty.Register<AddConsumerPanel, ICommand>(nameof(RefreshCommand));
    
    public ICommand RefreshCommand
    {
        get => GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }
    
    public AddConsumerPanel()
    {
        InitializeComponent();
    }
}