using System.Windows;
using System.Windows.Controls;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views;

public partial class EnrollmentView : Page
{
    private readonly EnrollmentViewModel vm = new();

    public EnrollmentView()
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