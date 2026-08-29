using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Core.Services.Insight;

/// <summary>
/// A flat, provider-agnostic snapshot of the inventory state an AI briefing summarises: which
/// items are in Shortage, the most recent StockMovements, and Waste totalled by reason over a
/// recent window. Decoupling this from the EF-loaded aggregates keeps prompt construction
/// (<see cref="InsightPrompt"/>) a pure, testable function of plain data, and documents exactly
/// what leaves the app for the LLM.
/// </summary>
public sealed record InventoryStateReport(
    IReadOnlyList<ShortageLine> Shortages,
    IReadOnlyList<MovementLine> RecentMovements,
    IReadOnlyList<WasteByReason> WasteByReason,
    TimeSpan WasteWindow);

/// <summary>One item currently in Shortage — its quantity against the level that triggered it.</summary>
public sealed record ShortageLine(string Name, decimal QuantityOnHand, decimal ReorderLevel, UnitOfMeasure Unit);

/// <summary>One recent StockMovement, flattened with its item's name and unit for the narrative.</summary>
public sealed record MovementLine(
    DateTimeOffset Timestamp,
    string ItemName,
    MovementType Type,
    decimal Quantity,
    UnitOfMeasure Unit,
    string? Detail);
