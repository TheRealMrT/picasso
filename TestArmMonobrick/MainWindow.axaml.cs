using Avalonia.Controls;
using TestArmMonobrick.ViewModels;

namespace TestArmMonobrick
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}