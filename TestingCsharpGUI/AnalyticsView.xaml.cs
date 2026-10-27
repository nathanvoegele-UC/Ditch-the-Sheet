using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TestingCsharpGUI;

public partial class AnalyticsView : UserControl
{
    private List<JobApplication> _applications = [];
    private int _weeklyGoal = 5;

    public event Action<int>? WeeklyGoalChangeRequested;

    public AnalyticsView()
    {
        InitializeComponent();
    }

    public void SetApplications(IEnumerable<JobApplication> applications)
    {
        _applications = applications.ToList();
        RenderChart();
    }

    public void SetWeeklyGoal(int goal)
    {
        _weeklyGoal = goal;
        if (WeeklyGoalText is not null)
        {
            WeeklyGoalText.Text = $"Weekly goal: {goal} applications";
        }
    }

    private void ChangeWeeklyGoal_Click(object sender, RoutedEventArgs e)
    {
        var editor = new WeeklyGoalWindow(_weeklyGoal)
        {
            Owner = Window.GetWindow(this)
        };
        if (editor.ShowDialog() == true)
        {
            WeeklyGoalChangeRequested?.Invoke(editor.WeeklyGoal);
        }
    }

    private void PeriodSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RenderChart();
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderChart();
    }

    private void RenderChart()
    {
        if (ChartCanvas is null || PeriodSelector is null || ChartSummary is null || EmptyState is null)
        {
            return;
        }

        string period = (PeriodSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "By day";
        DateTime today = DateTime.Today;
        List<(DateTime Start, string Label)> buckets = CreateBuckets(period, today);
        int[] counts = new int[buckets.Count];
        var countedIds = new HashSet<long>();

        foreach (JobApplication application in _applications)
        {
            if (application.Id <= 0
                || application.Status == "Saved"
                || !application.DateApplied.HasValue
                || !countedIds.Add(application.Id))
            {
                continue;
            }

            DateTime appliedDate = application.DateApplied.Value.Date;
            int bucketIndex = FindBucket(buckets, period, appliedDate);
            if (bucketIndex >= 0)
            {
                counts[bucketIndex]++;
            }
        }

        int total = counts.Sum();
        ChartSummary.Text = period switch
        {
            "By week" => $"Applications per week · last 12 weeks · {total} total",
            "By month" => $"Applications per month · last 12 months · {total} total",
            _ => $"Applications per day · last 14 days · {total} total"
        };
        EmptyState.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;
        DrawBars(buckets, counts);
    }

    private static List<(DateTime Start, string Label)> CreateBuckets(string period, DateTime today)
    {
        if (period == "By week")
        {
            DateTime currentWeek = StartOfWeek(today);
            return Enumerable.Range(0, 12)
                .Select(index =>
                {
                    DateTime start = currentWeek.AddDays((index - 11) * 7);
                    return (start, start.ToString("MMM d", CultureInfo.CurrentCulture));
                })
                .ToList();
        }

        if (period == "By month")
        {
            DateTime currentMonth = new(today.Year, today.Month, 1);
            return Enumerable.Range(0, 12)
                .Select(index =>
                {
                    DateTime start = currentMonth.AddMonths(index - 11);
                    return (start, start.ToString("MMM", CultureInfo.CurrentCulture));
                })
                .ToList();
        }

        return Enumerable.Range(0, 14)
            .Select(index =>
            {
                DateTime start = today.AddDays(index - 13);
                return (start, start.ToString("MMM d", CultureInfo.CurrentCulture));
            })
            .ToList();
    }

    private static int FindBucket(
        IReadOnlyList<(DateTime Start, string Label)> buckets,
        string period,
        DateTime appliedDate)
    {
        DateTime key = period switch
        {
            "By week" => StartOfWeek(appliedDate),
            "By month" => new DateTime(appliedDate.Year, appliedDate.Month, 1),
            _ => appliedDate
        };

        for (int index = 0; index < buckets.Count; index++)
        {
            if (buckets[index].Start == key)
            {
                return index;
            }
        }

        return -1;
    }

    private void DrawBars(IReadOnlyList<(DateTime Start, string Label)> buckets, IReadOnlyList<int> counts)
    {
        ChartCanvas.Children.Clear();
        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;
        if (width <= 0 || height <= 0 || buckets.Count == 0)
        {
            return;
        }

        const double left = 44;
        const double right = 12;
        const double top = 24;
        const double bottom = 38;
        double plotWidth = Math.Max(1, width - left - right);
        double plotHeight = Math.Max(1, height - top - bottom);
        int maxCount = counts.Count == 0 ? 0 : counts.Max();
        int step = Math.Max(1, (int)Math.Ceiling(maxCount / 4d));
        int maxValue = Math.Max(step * 4, 4);

        for (int tick = 0; tick <= 4; tick++)
        {
            double y = top + plotHeight - plotHeight * tick / 4d;
            var line = new Line
            {
                X1 = left,
                X2 = left + plotWidth,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(226, 231, 236)),
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(line);
            AddLabel((maxValue * tick / 4).ToString(CultureInfo.CurrentCulture), 0, y - 9, left - 8, 18, TextAlignment.Right);
        }

        double slotWidth = plotWidth / buckets.Count;
        double barWidth = Math.Min(34, slotWidth * 0.62);
        for (int index = 0; index < buckets.Count; index++)
        {
            double barHeight = plotHeight * counts[index] / maxValue;
            double x = left + slotWidth * index + (slotWidth - barWidth) / 2;
            double y = top + plotHeight - barHeight;
            if (counts[index] > 0)
            {
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(2, barHeight),
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new SolidColorBrush(Color.FromRgb(40, 120, 208))
                };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                ChartCanvas.Children.Add(bar);
                AddLabel(counts[index].ToString(CultureInfo.CurrentCulture), x - 8, y - 20, barWidth + 16, 18, TextAlignment.Center);
            }

            AddLabel(buckets[index].Label, left + slotWidth * index, top + plotHeight + 8, slotWidth, 20, TextAlignment.Center);
        }
    }

    private void AddLabel(string text, double left, double top, double width, double height, TextAlignment alignment)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            Height = height,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 133, 148)),
            TextAlignment = alignment,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        ChartCanvas.Children.Add(label);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}