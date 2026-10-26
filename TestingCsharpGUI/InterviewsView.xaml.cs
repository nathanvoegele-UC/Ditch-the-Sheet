using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TestingCsharpGUI;

public partial class InterviewsView : UserControl
{
    private List<JobApplication> _jobs = [];

    public event Action<JobApplication, DateTime, TimeSpan>? ScheduleRequested;
    public event Action<JobApplication>? RemoveRequested;
    public event Action<JobApplication>? DetailsRequested;

    public InterviewsView()
    {
        InitializeComponent();
        InterviewCalendar.SelectedDate = DateTime.Today;
        InterviewTimeSelector.ItemsSource = Enumerable.Range(0, 48)
            .Select(slot => DateTime.Today.AddMinutes(slot * 30)
                .ToString("h:mm tt", CultureInfo.CurrentCulture))
            .ToList();
        InterviewTimeSelector.SelectedItem = DateTime.Today.AddHours(9)
            .ToString("h:mm tt", CultureInfo.CurrentCulture);
    }

    public void SetJobs(IEnumerable<JobApplication> jobs)
    {
        _jobs = jobs.ToList();
        InterviewJobSelector.ItemsSource = _jobs
            .Where(job => job.Status is not ("Offer" or "Rejected"))
            .OrderBy(job => job.Company, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        UpdateInterviewLists();
    }

    private void InterviewCalendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateInterviewLists();
    }

    private void UpdateInterviewLists()
    {
        if (InterviewCalendar.SelectedDate is not DateTime selectedDate)
        {
            return;
        }

        List<JobApplication> interviewsForDay = _jobs
            .Where(job => job.Status == "Interview" && job.InterviewDate?.Date == selectedDate.Date)
            .OrderBy(job => job.InterviewTime)
            .ThenBy(job => job.Company, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        InterviewDayHeading.Text = selectedDate.ToString("dddd, MMMM d, yyyy", CultureInfo.CurrentCulture);
        InterviewList.ItemsSource = interviewsForDay;
        NoInterviewsText.Visibility = interviewsForDay.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        List<JobApplication> upcomingInterviews = _jobs
            .Where(job => job.Status == "Interview"
                && job.InterviewDate.HasValue
                && job.InterviewDate.Value.Date >= DateTime.Today)
            .OrderBy(job => job.InterviewDate)
            .ThenBy(job => job.InterviewTime)
            .Take(10)
            .ToList();
        UpcomingInterviewsList.ItemsSource = upcomingInterviews;
        UpcomingInterviewCountText.Text = $"{upcomingInterviews.Count} shown";
        NoUpcomingInterviewsText.Visibility = upcomingInterviews.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ScheduleInterview_Click(object sender, RoutedEventArgs e)
    {
        if (InterviewJobSelector.SelectedItem is not JobApplication job
            || InterviewCalendar.SelectedDate is not DateTime interviewDate
            || InterviewTimeSelector.SelectedItem is not string interviewTimeText
            || !DateTime.TryParseExact(
                interviewTimeText,
                "h:mm tt",
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out DateTime parsedInterviewTime))
        {
            MessageBox.Show("Select a job, date, and time to schedule an interview.",
                "Interview details required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ScheduleRequested?.Invoke(job, interviewDate, parsedInterviewTime.TimeOfDay);
    }

    private void RemoveInterview_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: JobApplication job })
        {
            RemoveRequested?.Invoke(job);
        }
    }

    private void OpenInterviewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: JobApplication job })
        {
            DetailsRequested?.Invoke(job);
        }
    }
}
