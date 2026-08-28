namespace PCEdit.SaveFileHandler.Models;

public sealed class PlanetTerraformation
{
    public required string PlanetId { get; init; }

    public decimal UnitOxygenLevel { get; init; }

    public decimal UnitHeatLevel { get; init; }

    public decimal UnitPressureLevel { get; init; }

    public decimal UnitPlantsLevel { get; init; }

    public decimal UnitInsectsLevel { get; init; }

    public decimal UnitAnimalsLevel { get; init; }

    public decimal UnitPurificationLevel { get; init; }
}
