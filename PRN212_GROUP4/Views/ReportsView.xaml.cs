using System.Windows;
using System.Windows.Controls;
using PRN212_GROUP4.Services;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class ReportsView : Page
    {
        private readonly ReportsViewModel viewModel;

        public ReportsView()
        {
            InitializeComponent();

            viewModel = new ReportsViewModel(
                new StudentReportService());

            DataContext = viewModel;

            Loaded += ReportsView_Loaded;
        }

        private async void ReportsView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= ReportsView_Loaded;

            await viewModel.GenerateCommand.ExecuteAsync();
        }
    }
}