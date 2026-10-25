using System;
using System.ComponentModel;
using System.Windows;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class EditStudentWindow : Window
    {
        private readonly EditStudentViewModel viewModel;

        public EditStudentWindow(Student student)
        {
            InitializeComponent();

            viewModel = new EditStudentViewModel(student);

            DataContext = viewModel;

            viewModel.SaveSucceeded += ViewModel_SaveSucceeded;

            Closed += EditStudentWindow_Closed;
        }

        private void ViewModel_SaveSucceeded(
            object? sender,
            EventArgs e)
        {
            DialogResult = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (viewModel.IsBusy)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        private void EditStudentWindow_Closed(
            object? sender,
            EventArgs e)
        {
            viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

            Closed -= EditStudentWindow_Closed;
        }
    }
}