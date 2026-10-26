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
        DateAppliedInput.SelectedDate = DateTime.Today;
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

        int? salary = null;
        if (!string.IsNullOrWhiteSpace(SalaryInput.Text))
        {
            string salaryText = SalaryInput.Text.Trim().Replace(",", string.Empty).Replace("$", string.Empty);
            if (!int.TryParse(salaryText, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedSalary)
                || parsedSalary < 0)
            {
                MessageBox.Show("Enter a valid annual salary as a non-negative whole number.",
                    "Invalid salary", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            salary = parsedSalary;
        }

        Application = new JobApplication
        {
            Company = company,
            Role = role,
            Location = LocationInput.Text.Trim(),
            WorkMode = WorkModeInput.SelectedItem as string ?? string.Empty,
            Salary = salary,
            DateApplied = DateAppliedInput.SelectedDate,
            Status = StatusInput.SelectedItem as string ?? "Applied",
            InterviewDate = InterviewDateInput.SelectedDate
        };
        DialogResult = true;
    }
}
