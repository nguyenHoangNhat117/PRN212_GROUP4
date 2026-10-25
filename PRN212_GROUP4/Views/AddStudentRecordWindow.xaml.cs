using System;
using System.ComponentModel;
using System.Windows;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class AddStudentRecordWindow : Window
    {
        private readonly AddStudentRecordViewModel viewModel;

        public int? CreatedRecordID =>
            viewModel.CreatedRecordID;

        public AddStudentRecordWindow(
            Student student,
            int? createdByUserID = null)
        {
            InitializeComponent();

            viewModel = new AddStudentRecordViewModel(
                student,
                createdByUserID);

            DataContext = viewModel;

            viewModel.SaveSucceeded += ViewModel_SaveSucceeded;

            Closed += AddStudentRecordWindow_Closed;
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

        private void AddStudentRecordWindow_Closed(
            object? sender,
            EventArgs e)
        {
            viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

            Closed -= AddStudentRecordWindow_Closed;
        }
    }
}