namespace Gamestack.Core.Abstractions;

/// <summary>Abstraction over the system clock, so time-dependent logic stays testable.</summary>
public interface IClock
{
    /// <summary>The current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Default <see cref="IClock"/> backed by the system clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
