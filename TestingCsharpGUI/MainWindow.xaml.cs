using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestingCsharpGUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void btnSubmit_Click(object sender, RoutedEventArgs e)
{
    string userName = txtName.Text;
    if (!string.IsNullOrWhiteSpace(userName))
    {
        MessageBox.Show($"Hello, {userName}! Welcome to your first C# WPF app.", "Greetings!");
    }
    else
    {
        MessageBox.Show("Please type a name before clicking.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

}