using System;
using System.ComponentModel;
using System.Windows;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class EditStudentRecordWindow : Window
    {
        private readonly EditStudentRecordViewModel viewModel;

        public EditStudentRecordWindow(
            Student student,
            StudentRecord record)
        {
            InitializeComponent();

            viewModel = new EditStudentRecordViewModel(
                student,
                record);

            DataContext = viewModel;

            viewModel.SaveSucceeded += ViewModel_SaveSucceeded;

            Closed += EditStudentRecordWindow_Closed;
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

        private void EditStudentRecordWindow_Closed(
            object? sender,
            EventArgs e)
        {
            viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

            Closed -= EditStudentRecordWindow_Closed;
        }
    }
}