using System;
using System.ComponentModel;
using System.Windows;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class AddStudentWindow : Window
    {
        private readonly AddStudentViewModel viewModel;

        public int? CreatedStudentID =>
            viewModel.CreatedStudentID;

        public AddStudentWindow()
        {
            InitializeComponent();

            viewModel = new AddStudentViewModel();

            DataContext = viewModel;

            viewModel.SaveSucceeded += ViewModel_SaveSucceeded;

            Loaded += AddStudentWindow_Loaded;
            Closed += AddStudentWindow_Closed;
        }

        private async void AddStudentWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= AddStudentWindow_Loaded;

            await viewModel.LoadDepartmentsAsync();
        }

        private void ViewModel_SaveSucceeded(
            object? sender,
            EventArgs e)
        {
            DialogResult = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Chặn đóng form khi đang tải hoặc lưu dữ liệu.
            if (viewModel.IsBusy)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        private void AddStudentWindow_Closed(
            object? sender,
            EventArgs e)
        {
            viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

            Loaded -= AddStudentWindow_Loaded;
            Closed -= AddStudentWindow_Closed;
        }
    }
}