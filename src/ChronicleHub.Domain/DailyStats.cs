namespace ChronicleHub.Domain;

public sealed class DailyStats
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    public int TotalEvents { get; set; }
    public int WorkSessions { get; set; }
    public int Workouts { get; set; }
    public int StudySessions { get; set; }
}