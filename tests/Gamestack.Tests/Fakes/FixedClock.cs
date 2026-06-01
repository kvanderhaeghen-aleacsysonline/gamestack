using Gamestack.Core.Abstractions;

namespace Gamestack.Tests.Fakes;

/// <summary>A clock that returns a fixed, controllable time.</summary>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
