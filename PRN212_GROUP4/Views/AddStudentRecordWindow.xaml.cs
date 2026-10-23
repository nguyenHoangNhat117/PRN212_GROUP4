using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace PRN212_GROUP4.Views
{
    public partial class AddStudentRecordWindow : Window
    {
        private readonly AddStudentRecordViewModel viewModel;

        private bool isSaving;

        public int? CreatedRecordID { get; private set; }

        public AddStudentRecordWindow(
            Student student,
            int? createdByUserID = null)
        {
            InitializeComponent();

            viewModel = new AddStudentRecordViewModel(
                student,
                createdByUserID);

            DataContext = viewModel;
        }

        private async void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isSaving)
            {
                return;
            }

            isSaving = true;

            FormPanel.IsEnabled = false;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            OperationStatus.Text = "Saving...";

            try
            {
                CreatedRecordID = await viewModel.SaveAsync();

                isSaving = false;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                OperationStatus.Text = "Record was not saved.";

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Không thể thêm hồ sơ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                isSaving = false;

                FormPanel.IsEnabled = true;
                SaveButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (isSaving)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}
