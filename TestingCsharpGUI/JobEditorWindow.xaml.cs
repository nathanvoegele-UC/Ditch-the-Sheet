using System.Globalization;
using System.Windows;

namespace TestingCsharpGUI;

public partial class JobEditorWindow : Window
{
    public JobApplication? Application { get; private set; }

    public JobEditorWindow()
    {
        InitializeComponent();
        WorkModeInput.ItemsSource = new[] { "On-site", "Hybrid", "Remote" };
        WorkModeInput.SelectedIndex = 0;
        StatusInput.ItemsSource = MainWindow.ApplicationStatusOptions;
        StatusInput.SelectedItem = "Applied";
        PayTypeInput.ItemsSource = new[] { "Yearly", "Hourly" };
        PayTypeInput.SelectedItem = "Yearly";
        DateAppliedInput.SelectedDate = DateTime.Today;
    }

    private void PayTypeInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PayAmountLabel.Text = PayTypeInput.SelectedItem as string == "Hourly"
            ? "Hourly rate (USD)"
            : "Yearly salary (USD)";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string company = CompanyInput.Text.Trim();
        string role = RoleInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(role))
        {
            MessageBox.Show("Enter both a company and a role.", "Required information",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal? payAmount = null;
        if (!string.IsNullOrWhiteSpace(PayAmountInput.Text))
        {
            string payText = PayAmountInput.Text.Trim().Replace(",", string.Empty).Replace("$", string.Empty);
            if (!decimal.TryParse(payText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedPay)
                || parsedPay < 0)
            {
                MessageBox.Show("Enter a valid non-negative pay amount, such as 65000 or 24.50.",
                    "Invalid pay amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            payAmount = parsedPay;
        }

        Application = new JobApplication
        {
            Company = company,
            Role = role,
            Location = LocationInput.Text.Trim(),
            WorkMode = WorkModeInput.SelectedItem as string ?? string.Empty,
            PayAmount = payAmount,
            PayType = PayTypeInput.SelectedItem as string ?? "Yearly",
            DateApplied = DateAppliedInput.SelectedDate,
            Status = StatusInput.SelectedItem as string ?? "Applied",
            InterviewDate = InterviewDateInput.SelectedDate
        };
        DialogResult = true;
    }
}
