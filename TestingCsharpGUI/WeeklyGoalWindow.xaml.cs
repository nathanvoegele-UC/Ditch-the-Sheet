using System.Globalization;
using System.Windows;

namespace TestingCsharpGUI;

public partial class WeeklyGoalWindow : Window
{
    public int WeeklyGoal { get; private set; }

    public WeeklyGoalWindow(int currentGoal)
    {
        InitializeComponent();
        GoalInput.Text = currentGoal.ToString(CultureInfo.CurrentCulture);
        GoalInput.SelectAll();
        Loaded += (_, _) => GoalInput.Focus();
    }

    private void SaveGoal_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(GoalInput.Text, NumberStyles.None, CultureInfo.CurrentCulture, out int goal)
            || goal is < 1 or > 1000)
        {
            MessageBox.Show(
                "Enter a whole number from 1 to 1000.",
                "Invalid weekly goal",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            GoalInput.Focus();
            GoalInput.SelectAll();
            return;
        }

        WeeklyGoal = goal;
        DialogResult = true;
    }
}
