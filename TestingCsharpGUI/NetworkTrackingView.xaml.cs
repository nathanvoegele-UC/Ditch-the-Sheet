using System.Windows;
using System.Windows.Controls;

namespace TestingCsharpGUI;

public partial class NetworkTrackingView : UserControl
{
    public event Func<NetworkContact, bool>? ContactSaveRequested;

    public NetworkTrackingView()
    {
        InitializeComponent();
    }

    public void SetContacts(IEnumerable<NetworkContact> contacts)
    {
        List<NetworkContact> contactList = contacts.ToList();
        ContactsDataGrid.ItemsSource = contactList;
        ContactCountText.Text = $"{contactList.Count} connection{(contactList.Count == 1 ? string.Empty : "s")}";
        NoContactsText.Visibility = contactList.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SaveContact_Click(object sender, RoutedEventArgs e)
    {
        string name = NameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a name for this connection.", "Name required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameInput.Focus();
            return;
        }

        var contact = new NetworkContact
        {
            Name = name,
            Company = CompanyInput.Text.Trim(),
            Position = PositionInput.Text.Trim(),
            Email = EmailInput.Text.Trim(),
            PhoneNumber = PhoneInput.Text.Trim(),
            LinkedInUrl = LinkedInInput.Text.Trim(),
            Notes = NotesInput.Text.Trim()
        };

        if (ContactSaveRequested?.Invoke(contact) != true)
        {
            return;
        }

        NameInput.Clear();
        CompanyInput.Clear();
        PositionInput.Clear();
        EmailInput.Clear();
        PhoneInput.Clear();
        LinkedInInput.Clear();
        NotesInput.Clear();
        NameInput.Focus();
    }
}
