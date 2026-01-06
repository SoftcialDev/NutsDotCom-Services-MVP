namespace ComplaintAnalysis.Domain.Entities;

public class Complaint
{
    public Guid Id { get; private set; }
    public string Text { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    private Complaint(Guid id, string text, DateTime submittedAt)
    {
        Id = id;
        Text = text;
        SubmittedAt = submittedAt;
    }

    public static Complaint Create(string text)
    {
        return new Complaint(
            id: Guid.NewGuid(),
            text: text,
            submittedAt: DateTime.UtcNow
        );
    }
}

