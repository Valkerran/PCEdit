namespace PCEdit.SaveFileHandler;

/// <summary>
/// The game's Unity <c>GetStableHashCode</c> of a planet id. This equals the integer the game
/// writes as <see cref="Models.WorldObject.Planet"/> (<c>"planet"</c>), so it is the bridge
/// between the string <c>PlanetId</c> space (<see cref="Models.PlayerData"/> /
/// <see cref="Models.PlanetTerraformation"/> / <see cref="Models.SaveFileMetadata"/>) and the
/// world-object planet hint.
/// </summary>
/// <remarks>
/// Two-accumulator djb2 over UTF-16 code units, stopping at a NUL. Verified against real saves:
/// <c>Prime</c> → <c>-1140328421</c>, <c>Selenea</c> → <c>-1016990411</c>,
/// <c>Aqualis</c> → <c>-1291310150</c>.
/// </remarks>
public static class PlanetHash
{
    public static int Of(string planetId)
    {
        ArgumentNullException.ThrowIfNull(planetId);

        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < planetId.Length && planetId[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ planetId[i];

                if (i == planetId.Length - 1 || planetId[i + 1] == '\0')
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ planetId[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
