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
    public partial class EditStudentWindow : Window
    {
        private readonly EditStudentViewModel viewModel;

        private bool isSaving;

        public EditStudentWindow(Student student)
        {
            InitializeComponent();

            viewModel = new EditStudentViewModel(student);

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
                await viewModel.SaveAsync();

                isSaving = false;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                OperationStatus.Text = "Changes were not saved.";

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Không thể cập nhật",
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
