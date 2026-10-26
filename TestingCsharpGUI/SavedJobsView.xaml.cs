using System.Windows;
using System.Windows.Controls;

namespace TestingCsharpGUI;

public partial class SavedJobsView : UserControl
{
    public event Action<ComboBox, SelectionChangedEventArgs>? StatusChanged;

    public SavedJobsView()
    {
        InitializeComponent();
    }

    public void SetJobs(IEnumerable<JobApplication> jobs)
    {
        List<JobApplication> savedJobs = jobs.ToList();
        SavedJobsDataGrid.ItemsSource = savedJobs;
        SavedCountText.Text = $"{savedJobs.Count} saved";
        NoSavedJobsText.Visibility = savedJobs.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && e.AddedItems.Count > 0)
        {
            StatusChanged?.Invoke(comboBox, e);
        }
    }
}
