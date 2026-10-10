using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// Every key on an attribute-driven model is either one the game always writes, and then a
/// record without it is refused, or one the game may omit, and then it is nullable so an absent
/// key stays absent on save. Nothing may sit in between: a plain non-nullable property invents
/// a 0 for a missing key (issue #127). This pins the rule for every property, including ones
/// added later.
/// </summary>
public sealed class ModelRequiredKeyTests
{
    private static readonly Type[] AttributeDrivenModels =
    [
        typeof(Inventory), typeof(PlanetTerraformation), typeof(PlayerData), typeof(ProceduralInstance),
        typeof(ReadMessage), typeof(SaveFileMetadata), typeof(SaveFileStatistics), typeof(SaveFileUnlocks),
        typeof(StoryEvent)
    ];

    public static TheoryData<Type, string> KeyProperties()
    {
        var properties = new TheoryData<Type, string>();
        foreach (var model in AttributeDrivenModels)
        {
            foreach (var property in model.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonExtensionDataAttribute>() is null && property.Name != "EqualityContract")
                {
                    properties.Add(model, property.Name);
                }
            }
        }

        return properties;
    }

    [Theory]
    [MemberData(nameof(KeyProperties))]
    public void EveryKey_IsEitherRequiredOrNullable_NeverBoth(Type model, string propertyName)
    {
        var property = model.GetProperty(propertyName)!;
        var isRequired = property.GetCustomAttribute<RequiredMemberAttribute>() is not null
            || property.GetCustomAttribute<JsonRequiredAttribute>() is not null;
        var isNullable = new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;

        Assert.True(isRequired != isNullable,
            $"{model.Name}.{propertyName} must be required (always written) or nullable (may be absent), not {(isRequired ? "both" : "neither")}.");
    }
}
