namespace BizAnalytics.Api.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public Guid OwnerUserId { get; set; }

    public User Owner { get; set; } = null!;
}