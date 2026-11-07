using System.Windows;
using System.Windows.Controls;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views;

public partial class ReportsView : Page
{
    private readonly ReportsViewModel vm = new();

    public ReportsView()
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += Initialize;
    }

    private async void Initialize(object sender, RoutedEventArgs e)
    {
        Loaded -= Initialize;
        await vm.InitializeAsync();
    }
}