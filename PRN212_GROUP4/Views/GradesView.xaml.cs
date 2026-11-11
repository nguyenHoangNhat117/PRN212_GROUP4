using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views;

public partial class GradesView : Page
{
    private readonly GradeCenterViewModel vm = new();

    public GradesView()
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
