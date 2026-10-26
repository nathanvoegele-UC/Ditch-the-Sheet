namespace TestingCsharpGUI;

public sealed class JobApplication
{
    public long Id { get; init; }
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string WorkMode { get; init; } = string.Empty;
    public int? Salary { get; init; }
    public DateTime? DateApplied { get; init; }
    public string Status { get; set; } = "Applied";
    public DateTime? InterviewDate { get; init; }
    public TimeSpan? InterviewTime { get; init; }

    public string LocationDisplay => string.IsNullOrWhiteSpace(Location) ? "—" : Location;
    public string SalaryDisplay => Salary.HasValue ? $"${Salary.Value:N0}" : "—";
    public string DateAppliedDisplay => DateApplied?.ToShortDateString() ?? "—";
    public string InterviewTimeDisplay =>
        InterviewTime.HasValue
            ? DateTime.Today.Add(InterviewTime.Value).ToString("h:mm tt")
            : "Time not set";
}
