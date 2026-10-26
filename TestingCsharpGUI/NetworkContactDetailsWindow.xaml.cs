using System.Windows;

namespace TestingCsharpGUI;

public partial class NetworkContactDetailsWindow : Window
{
    public NetworkContactDetailsWindow(NetworkContact contact)
    {
        InitializeComponent();
        DataContext = contact;
    }
}
