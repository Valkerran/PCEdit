namespace PCEdit.App.Core.Models;

/// <summary>Where a placed object - a container, a machine - came from (issue #64).</summary>
public enum ContainerOrigin
{
    /// <summary>Not determinable from the save: a type that is both built and spawned in wrecks,
    /// with no wreck record either way. Deliberately not guessed.</summary>
    Unknown,

    /// <summary>Built by the player.</summary>
    Built,

    /// <summary>Placed in the game's map by the developers; there when the world began.</summary>
    Map,

    /// <summary>Spawned in a procedurally generated wreck.</summary>
    Wreck,
}
