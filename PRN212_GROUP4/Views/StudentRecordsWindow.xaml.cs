using System.ComponentModel;
using System.Windows;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class StudentRecordsWindow : Window
    {
        private readonly StudentRecordsViewModel viewModel;

        public StudentRecordsWindow(Student student)
        {
            InitializeComponent();

            viewModel = new StudentRecordsViewModel(
                student,
                new StudentRecordDialogService(this));

            DataContext = viewModel;

            Loaded += StudentRecordsWindow_Loaded;
        }

        private async void StudentRecordsWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= StudentRecordsWindow_Loaded;

            // Dùng cùng command với nút Refresh.
            await viewModel.RefreshCommand.ExecuteAsync();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (viewModel.IsBusy)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}