namespace RestaurantInventory.Core.Domain;

/// <summary>
/// How a <see cref="UnitOfMeasure"/> is written short-hand (kg, g, L, ml, each). Lives in Core so
/// every surface — the web display helpers and the AI prompt — abbreviates units identically and
/// the table can never drift between them.
/// </summary>
public static class UnitOfMeasureExtensions
{
    public static string Abbreviate(this UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Kg => "kg",
        UnitOfMeasure.G => "g",
        UnitOfMeasure.L => "L",
        UnitOfMeasure.Ml => "ml",
        UnitOfMeasure.Each => "each",
        _ => unit.ToString(),
    };
}
