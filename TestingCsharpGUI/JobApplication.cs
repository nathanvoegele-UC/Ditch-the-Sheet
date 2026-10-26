namespace TestingCsharpGUI;

public sealed class JobApplication
{
    public long Id { get; init; }
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string WorkMode { get; init; } = string.Empty;
    public decimal? PayAmount { get; init; }
    public string PayType { get; init; } = "Yearly";
    public DateTime? DateApplied { get; init; }
    public string Status { get; set; } = "Applied";
    public DateTime? InterviewDate { get; init; }
    public TimeSpan? InterviewTime { get; init; }
    public long? NetworkContactId { get; set; }
    public string NetworkContactName { get; set; } = string.Empty;
    public bool HasLinkedContact => !string.IsNullOrWhiteSpace(NetworkContactName);

    public string LocationDisplay => string.IsNullOrWhiteSpace(Location) ? "—" : Location;
    public string PayDisplay => PayAmount.HasValue
        ? PayType == "Hourly"
            ? $"{PayAmount.Value:C2}/hour"
            : $"{PayAmount.Value:C0}/year"
        : "—";
    public string DateAppliedDisplay => DateApplied?.ToShortDateString() ?? "—";
    public string InterviewDateDisplay => InterviewDate?.ToString("ddd, MMM d") ?? "Date not set";
    public string InterviewTimeDisplay =>
        InterviewTime.HasValue
            ? DateTime.Today.Add(InterviewTime.Value).ToString("h:mm tt")
            : "Time not set";
}
