namespace RestaurantInventory.Tests.Integration;

/// <summary>A <see cref="TimeProvider"/> whose UTC now is fixed and hand-advanced by tests.</summary>
internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestClock(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;
}
