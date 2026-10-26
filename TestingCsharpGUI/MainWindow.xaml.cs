using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TestingCsharpGUI;

public partial class MainWindow : Window
{
    private static readonly string[] ApplicationStatuses =
        ["Applied", "Interview", "Offer", "Rejected", "Saved"];
    private readonly string _databasePath;
    private List<JobApplication> _allJobs = [];
    private bool _isLoading;
    private string _activeView = "All";

    public IReadOnlyList<string> StatusOptions => ApplicationStatuses;
    public static IReadOnlyList<string> ApplicationStatusOptions => ApplicationStatuses;

    public MainWindow()
    {
        InitializeComponent();

        string databaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TestingCsharpGUI");
        Directory.CreateDirectory(databaseDirectory);
        _databasePath = Path.Combine(databaseDirectory, "submissions.db");
        InitializeDatabase();
        InterviewsContent.ScheduleRequested += ScheduleInterview;
        InterviewsContent.RemoveRequested += job => RemoveInterview(job);
        InterviewsContent.DetailsRequested += OpenInterviewDetails;
        SavedJobsContent.StatusChanged += (comboBox, args) => Status_SelectionChanged(comboBox, args);

        _isLoading = true;
        LocationFilter.ItemsSource = new[] { "All locations" };
        LocationFilter.SelectedIndex = 0;
        WorkModeFilter.ItemsSource = new[] { "Any work mode", "On-site", "Hybrid", "Remote" };
        WorkModeFilter.SelectedIndex = 0;
        SalaryFilter.ItemsSource = new[]
        {
            "Any pay",
            "Annual salary: $50,000+",
            "Annual salary: $75,000+",
            "Annual salary: $100,000+"
        };
        SalaryFilter.SelectedIndex = 0;
        StatusFilter.ItemsSource = new[] { "Any status" }.Concat(ApplicationStatuses).ToArray();
        StatusFilter.SelectedIndex = 0;
        _isLoading = false;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadJobs();
    }

    private void AddJob_Click(object sender, RoutedEventArgs e)
    {
        var editor = new JobEditorWindow { Owner = this };
        if (editor.ShowDialog() != true || editor.Application is not JobApplication job)
        {
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Submissions
                    (Company, Role, SelectedDate, Location, WorkMode, Salary, PayAmount, PayType, Status, InterviewDate)
                VALUES
                    ($company, $role, $selectedDate, $location, $workMode, NULL, $payAmount, $payType, $status, $interviewDate);
                """;
            command.Parameters.AddWithValue("$company", job.Company);
            command.Parameters.AddWithValue("$role", job.Role);
            command.Parameters.AddWithValue("$selectedDate", ToDatabaseDate(job.DateApplied));
            command.Parameters.AddWithValue("$location", job.Location);
            command.Parameters.AddWithValue("$workMode", job.WorkMode);
            command.Parameters.AddWithValue("$payAmount", job.PayAmount.HasValue ? job.PayAmount.Value : DBNull.Value);
            command.Parameters.AddWithValue("$payType", job.PayType);
            command.Parameters.AddWithValue("$status", job.Status);
            command.Parameters.AddWithValue("$interviewDate", ToDatabaseDate(job.InterviewDate));
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The job application could not be saved. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        LoadJobs();
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string view })
        {
            return;
        }

        _activeView = view is "Interviews" or "Saved" ? view : "All";
        bool showInterviews = _activeView == "Interviews";
        bool showSavedJobs = _activeView == "Saved";
        AllJobsContent.Visibility = !showInterviews && !showSavedJobs
            ? Visibility.Visible
            : Visibility.Collapsed;
        SavedJobsContent.Visibility = showSavedJobs ? Visibility.Visible : Visibility.Collapsed;
        InterviewsContent.Visibility = showInterviews ? Visibility.Visible : Visibility.Collapsed;
        if (!string.Equals(_activeView, "All", StringComparison.Ordinal))
        {
            HideJobDetails();
        }
        PageTitle.Text = _activeView switch
        {
            "Interviews" => "Interviews",
            "Saved" => "Saved for later",
            _ => "All jobs"
        };
        AllJobsNavButton.Background = _activeView == "All" ? new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(234, 243, 252)) : System.Windows.Media.Brushes.Transparent;
        AllJobsNavButton.Foreground = _activeView == "All"
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 120, 208))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 66, 85));
        InterviewsNavButton.Background = showInterviews
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 243, 252))
            : System.Windows.Media.Brushes.Transparent;
        InterviewsNavButton.Foreground = showInterviews
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 120, 208))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 66, 85));
        SavedNavButton.Background = _activeView == "Saved"
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 243, 252))
            : System.Windows.Media.Brushes.Transparent;
        SavedNavButton.Foreground = _activeView == "Saved"
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 120, 208))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 66, 85));
        ExportButton.Visibility = showInterviews || showSavedJobs
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (showInterviews)
        {
            InterviewsContent.SetJobs(_allJobs);
        }
        else if (!showSavedJobs)
        {
            ApplyFilters();
        }
    }

    private void JobsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, JobsDataGrid))
        {
            return;
        }

        if (JobsDataGrid.SelectedItem is not JobApplication job)
        {
            HideJobDetails();
            return;
        }

        JobDetailsPanel.DataContext = job;
        JobDetailsPanel.Visibility = Visibility.Visible;
        JobsContentGrid.ColumnDefinitions[1].Width = new GridLength(330);
    }

    private void CloseJobDetails_Click(object sender, RoutedEventArgs e)
    {
        JobsDataGrid.SelectedItem = null;
        HideJobDetails();
    }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        if (JobDetailsPanel.DataContext is not JobApplication job)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Permanently delete the job application for {job.Company} — {job.Role}? This cannot be undone.",
            "Delete job application",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Submissions WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", job.Id);

            if (command.ExecuteNonQuery() != 1)
            {
                MessageBox.Show(
                    "The job application was not found and could not be deleted. The job list will be refreshed.",
                    "Delete failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                LoadJobs();
                return;
            }
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The job application could not be deleted. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        JobsDataGrid.SelectedItem = null;
        HideJobDetails();
        LoadJobs();
    }

    private void HideJobDetails()
    {
        JobDetailsPanel.DataContext = null;
        JobDetailsPanel.Visibility = Visibility.Collapsed;
        JobsContentGrid.ColumnDefinitions[1].Width = new GridLength(0);
    }

    private void OpenInterviewDetails(JobApplication job)
    {
        var detailsWindow = new InterviewDetailsWindow(job)
        {
            Owner = this
        };
        detailsWindow.RemoveRequested += (selectedJob, complete) =>
            complete(RemoveInterview(selectedJob));
        detailsWindow.ShowDialog();
    }

    private void ScheduleInterview(JobApplication job, DateTime interviewDate, TimeSpan interviewTime)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE Submissions
                SET Status = $status, InterviewDate = $interviewDate, InterviewTime = $interviewTime
                WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$status", "Interview");
            command.Parameters.AddWithValue("$interviewDate", ToDatabaseDate(interviewDate));
            command.Parameters.AddWithValue("$interviewTime", ToDatabaseTime(interviewTime));
            command.Parameters.AddWithValue("$id", job.Id);
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The interview could not be scheduled. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        LoadJobs();
    }

    private bool RemoveInterview(JobApplication job)
    {
        MessageBoxResult result = MessageBox.Show(
            $"Remove the interview for {job.Company} — {job.Role}? The job application will remain and its status will be reset to Applied.",
            "Remove interview",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE Submissions
                SET Status = $status, InterviewDate = NULL, InterviewTime = NULL
                WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$status", "Applied");
            command.Parameters.AddWithValue("$id", job.Id);
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The interview could not be removed. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
        LoadJobs();
        return true;
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoading)
        {
            ApplyFilters();
        }
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading && IsLoaded)
        {
            ApplyFilters();
        }
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        SearchTextBox.Clear();
        LocationFilter.SelectedIndex = 0;
        WorkModeFilter.SelectedIndex = 0;
        SalaryFilter.SelectedIndex = 0;
        StatusFilter.SelectedIndex = 0;
        _activeView = "All";
        PageTitle.Text = "All jobs";
        _isLoading = false;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (!IsLoaded || LocationFilter.SelectedItem is not string location
            || WorkModeFilter.SelectedItem is not string workMode
            || SalaryFilter.SelectedItem is not string salaryFilter
            || StatusFilter.SelectedItem is not string status)
        {
            return;
        }

        string search = SearchTextBox.Text.Trim();
        int? minimumSalary = salaryFilter switch
        {
            "Annual salary: $50,000+" => 50000,
            "Annual salary: $75,000+" => 75000,
            "Annual salary: $100,000+" => 100000,
            _ => null
        };

        List<JobApplication> filtered = _allJobs.Where(job =>
            (string.IsNullOrEmpty(search)
                || job.Company.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || job.Role.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || job.Location.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            && (location == "All locations" || job.Location == location)
            && (workMode == "Any work mode" || job.WorkMode == workMode)
            && (!minimumSalary.HasValue
                || (job.PayType == "Yearly" && job.PayAmount >= minimumSalary))
            && (status == "Any status" || job.Status == status))
            .ToList();

        JobsDataGrid.ItemsSource = filtered;
        ResultsCountText.Text = $"{filtered.Count} result{(filtered.Count == 1 ? string.Empty : "s")}";
    }

    private void Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading
            || sender is not ComboBox { DataContext: JobApplication job, SelectedItem: string status }
            || e.AddedItems.Count == 0)
        {
            return;
        }

        if (job.Status == status)
        {
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Submissions SET Status = $status WHERE Id = $id;";
            command.Parameters.AddWithValue("$status", status);
            command.Parameters.AddWithValue("$id", job.Id);
            command.ExecuteNonQuery();
            job.Status = status;
            if (ReferenceEquals(JobDetailsPanel.DataContext, job))
            {
                JobDetailsPanel.DataContext = null;
                JobDetailsPanel.DataContext = job;
            }
            UpdateDashboardMetrics();
            ApplyFilters();
            SavedJobsContent.SetJobs(_allJobs.Where(job => job.Status == "Saved"));
            InterviewsContent.SetJobs(_allJobs);
        }
        catch (Exception exception) when (
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The application status could not be updated. {exception.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LoadJobs();
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (JobsDataGrid.ItemsSource is not IEnumerable<JobApplication> jobs)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export job applications",
            FileName = "job-applications.csv",
            Filter = "CSV file (*.csv)|*.csv"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Company,Role,Location,Work Mode,Pay Amount,Pay Type,Date Applied,Status,Interview Date/Time");
            foreach (JobApplication job in jobs)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    CsvEscape(job.Company),
                    CsvEscape(job.Role),
                    CsvEscape(job.Location),
                    CsvEscape(job.WorkMode),
                    CsvEscape(job.PayAmount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                    CsvEscape(job.PayType),
                    CsvEscape(job.DateApplied?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
                    CsvEscape(job.Status),
                    CsvEscape(job.InterviewDate.HasValue
                        ? $"{job.InterviewDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {job.InterviewTime?.ToString(@"hh\:mm", CultureInfo.InvariantCulture)}".TrimEnd()
                        : string.Empty)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
            MessageBox.Show("The visible job listings were exported.", "Export complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The job listings could not be exported. {exception.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
                SELECT Id, Company, Role, Location, WorkMode, Salary, SelectedDate, Status, InterviewDate, InterviewTime, PayAmount, PayType
                FROM Submissions
                ORDER BY Id DESC;
                """;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                jobs.Add(new JobApplication
                {
                    Id = reader.GetInt64(0),
                    Company = reader.GetString(1),
                    Role = reader.GetString(2),
                    Location = reader.GetString(3),
                    WorkMode = reader.GetString(4),
                    PayAmount = reader.IsDBNull(10)
                        ? reader.IsDBNull(5) ? null : reader.GetDecimal(5)
                        : reader.GetDecimal(10),
                    PayType = reader.GetString(11),
                    DateApplied = ReadDatabaseDate(reader, 6),
                    Status = reader.GetString(7),
                    InterviewDate = ReadDatabaseDate(reader, 8),
                    InterviewTime = ReadDatabaseTime(reader, 9)
                });
            }

            _allJobs = jobs;
            UpdateLocationFilter();
            UpdateDashboardMetrics();
            ApplyFilters();
            SavedJobsContent.SetJobs(_allJobs.Where(job => job.Status == "Saved"));
            InterviewsContent.SetJobs(_allJobs);
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

    private void UpdateLocationFilter()
    {
        string? selectedLocation = LocationFilter.SelectedItem as string;
        string[] locations = new[] { "All locations" }
            .Concat(_allJobs
                .Select(job => job.Location)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase))
            .ToArray();

        _isLoading = true;
        LocationFilter.ItemsSource = locations;
        LocationFilter.SelectedItem = locations.Contains(selectedLocation) ? selectedLocation : "All locations";
        _isLoading = false;
    }

    private void UpdateDashboardMetrics()
    {
        int applicationsThisWeek = _allJobs.Count(job =>
            job.Status != "Saved" && job.DateApplied.HasValue && IsThisWeek(job.DateApplied.Value));
        int upcomingInterviews = _allJobs.Count(job =>
            job.Status == "Interview" && job.InterviewDate.HasValue
            && job.InterviewDate.Value.Date >= DateTime.Today);

        TotalJobsText.Text = _allJobs.Count.ToString(CultureInfo.CurrentCulture);
        SavedJobsText.Text = _allJobs.Count(job => job.Status == "Saved").ToString(CultureInfo.CurrentCulture);
        ApplicationsThisWeekText.Text = applicationsThisWeek.ToString(CultureInfo.CurrentCulture);
        UpcomingInterviewsText.Text = upcomingInterviews.ToString(CultureInfo.CurrentCulture);
        WeeklyGoalText.Text = $"{applicationsThisWeek} / 5";
        WeeklyGoalProgress.Value = Math.Min(applicationsThisWeek, 5);
        AllJobsNavButton.Content = $"▤   All jobs    {_allJobs.Count}";
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS Submissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Company TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    SelectedDate TEXT,
                    Location TEXT NOT NULL,
                    WorkMode TEXT NOT NULL DEFAULT '',
                    Salary INTEGER,
                    PayAmount REAL,
                    PayType TEXT NOT NULL DEFAULT 'Yearly',
                    Status TEXT NOT NULL DEFAULT 'Applied',
                    InterviewDate TEXT,
                    InterviewTime TEXT
                );
                """;
            createCommand.ExecuteNonQuery();
        }

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = "PRAGMA table_info(Submissions);";
            using var reader = schemaCommand.ExecuteReader();
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        var migrations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WorkMode"] = "ALTER TABLE Submissions ADD COLUMN WorkMode TEXT NOT NULL DEFAULT '';",
            ["Salary"] = "ALTER TABLE Submissions ADD COLUMN Salary INTEGER;",
            ["PayAmount"] = "ALTER TABLE Submissions ADD COLUMN PayAmount REAL;",
            ["PayType"] = "ALTER TABLE Submissions ADD COLUMN PayType TEXT NOT NULL DEFAULT 'Yearly';",
            ["Status"] = "ALTER TABLE Submissions ADD COLUMN Status TEXT NOT NULL DEFAULT 'Applied';",
            ["InterviewDate"] = "ALTER TABLE Submissions ADD COLUMN InterviewDate TEXT;",
            ["InterviewTime"] = "ALTER TABLE Submissions ADD COLUMN InterviewTime TEXT;"
        };

        foreach ((string column, string migration) in migrations)
        {
            if (!existingColumns.Contains(column))
            {
                using var migrationCommand = connection.CreateCommand();
                migrationCommand.CommandText = migration;
                migrationCommand.ExecuteNonQuery();
            }
        }

        using var migratePayCommand = connection.CreateCommand();
        migratePayCommand.CommandText = """
            UPDATE Submissions
            SET PayAmount = Salary
            WHERE PayAmount IS NULL AND Salary IS NOT NULL;
            """;
        migratePayCommand.ExecuteNonQuery();
    }

    private static object ToDatabaseDate(DateTime? date) =>
        date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DBNull.Value;

    private static string ToDatabaseTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static DateTime? ReadDatabaseDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateTime.ParseExact(
                reader.GetString(ordinal),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

    private static TimeSpan? ReadDatabaseTime(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : TimeSpan.ParseExact(
                reader.GetString(ordinal),
                @"hh\:mm",
                CultureInfo.InvariantCulture);

    private static bool IsThisWeek(DateTime date)
    {
        DateTime today = DateTime.Today;
        DateTime startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        return date.Date >= startOfWeek && date.Date <= today;
    }

    private static string CsvEscape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
