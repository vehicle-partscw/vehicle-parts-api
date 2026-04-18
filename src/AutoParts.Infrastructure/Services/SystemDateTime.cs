using AutoParts.Application.Common.Interfaces;

namespace AutoParts.Infrastructure.Services;

public class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
