using System.Windows;

namespace TestingCsharpGUI;

public partial class InterviewDetailsWindow : Window
{
    public event Action<JobApplication, Action<bool>>? RemoveRequested;

    private readonly JobApplication _job;

    public InterviewDetailsWindow(JobApplication job)
    {
        InitializeComponent();
        _job = job;
        DataContext = job;
    }

    private void RemoveInterview_Click(object sender, RoutedEventArgs e)
    {
        RemoveRequested?.Invoke(_job, removed =>
        {
            if (removed)
            {
                DialogResult = true;
            }
        });
    }
}
