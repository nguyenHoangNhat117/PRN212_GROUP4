using System.Windows.Controls;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Views
{
    public partial class StudentView : Page
    {
        public StudentView()
        {
            InitializeComponent();
        }

        public StudentView(StudentViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}