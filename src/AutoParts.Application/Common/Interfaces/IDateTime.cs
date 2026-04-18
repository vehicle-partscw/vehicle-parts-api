namespace AutoParts.Application.Common.Interfaces;

public interface IDateTime
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
