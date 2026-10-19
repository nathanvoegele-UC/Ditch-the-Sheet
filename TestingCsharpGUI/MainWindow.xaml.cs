using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Windows;

namespace TestingCsharpGUI;

public sealed record JobApplication(string Company, string Role, string DateApplied, string Location);

public partial class MainWindow : Window
{
    private readonly string _databasePath;

    public MainWindow()
    {
        InitializeComponent();
        MyDatePicker.SelectedDate = DateTime.Today;

        string databaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TestingCsharpGUI");
        Directory.CreateDirectory(databaseDirectory);
        _databasePath = Path.Combine(databaseDirectory, "submissions.db");
        InitializeDatabase();
        LoadJobs();
    }

    private void btnSubmit_Click(object sender, RoutedEventArgs e)
    {
        string userName = txtCompany.Text;
        string userRole = txtRole.Text;
        string userLocation = txtLocation.Text;
        DateTime? selectedDate = MyDatePicker.SelectedDate;

        if (string.IsNullOrWhiteSpace(userName))
        {
            MessageBox.Show("Please type a company before submitting.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Submissions (Company, Role, SelectedDate, Location)
                VALUES ($company, $role, $selectedDate, $location);
                """;
            command.Parameters.AddWithValue("$company", userName);
            command.Parameters.AddWithValue("$role", userRole);
            command.Parameters.AddWithValue(
                "$selectedDate",
                selectedDate.HasValue ? selectedDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            command.Parameters.AddWithValue("$location", userLocation);
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The information could not be saved to the database. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        LoadJobs();
        MessageBox.Show("The job application was saved.", "Saved");
    }

    private void btnRefreshJobs_Click(object sender, RoutedEventArgs e)
    {
        LoadJobs();
    }

    private void LoadJobs()
    {
        try
        {
            var jobs = new List<JobApplication>();
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Company, Role, SelectedDate, Location
                FROM Submissions
                ORDER BY Id DESC;
                """;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string dateApplied = reader.IsDBNull(2)
                    ? string.Empty
                    : DateTime.ParseExact(
                        reader.GetString(2),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture).ToShortDateString();

                jobs.Add(new JobApplication(
                    reader.GetString(0),
                    reader.GetString(1),
                    dateApplied,
                    reader.GetString(3)));
            }

            JobsDataGrid.ItemsSource = jobs;
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException or FormatException)
        {
            MessageBox.Show(
                $"The saved job applications could not be loaded. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Submissions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Company TEXT NOT NULL,
                Role TEXT NOT NULL,
                SelectedDate TEXT,
                Location TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}