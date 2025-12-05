using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace ATS_Desktop.Views.Components;

public partial class ImportExportPanel : UserControl
{
    public static readonly StyledProperty<ICommand?> ImportCommandProperty =
        AvaloniaProperty.Register<ImportExportPanel, ICommand?>(
            nameof(ImportCommand));
    
    public static readonly StyledProperty<ICommand?> ExportCommandProperty =
        AvaloniaProperty.Register<ImportExportPanel, ICommand?>(
            nameof(ExportCommand));
    
    public ICommand? ImportCommand
    {
        get => GetValue(ImportCommandProperty);
        set => SetValue(ImportCommandProperty, value);
    }
    
    public ICommand? ExportCommand
    {
        get => GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }
    
    
    public ImportExportPanel()
    {
        InitializeComponent();
    }
}