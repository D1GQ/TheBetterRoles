using System.Text.Json.Serialization;
using TheBetterRoles.Data;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the configuration for a custom hat, with properties for its ID, sponsor-only availability, sponsor tier, and whether it's dev-only.
/// Includes a method to check if the player meets the necessary conditions for using the hat.
/// </summary>
internal class CustomCosmeticConfig(string id, bool sponsorOnly = false, int sponsorTier = 0, bool devOnly = false)
{
    /// <summary>
    /// The unique ID of the custom hat, prefixed with "hat_".
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; } = id;

    /// <summary>
    /// Flag indicating whether the hat is only available to sponsors.
    /// </summary>
    [JsonPropertyName("sponsor")] public bool SponsorOnly { get; } = sponsorOnly;

    /// <summary>
    /// The tier of the sponsor required to use the hat.
    /// </summary>
    [JsonPropertyName("sponsorTier")] public int SponsorTier { get; } = sponsorTier;

    /// <summary>
    /// Flag indicating whether the hat is only available to developers.
    /// </summary>
    [JsonPropertyName("dev")] public bool DevOnly { get; } = devOnly;

    /// <summary>
    /// Checks if the player has permission to use the hat based on its configuration.
    /// </summary>
    /// <returns>True if the player meets the conditions, false otherwise.</returns>
    internal bool CheckPermission()
    {
        if (DevOnly) return Main.MyData.IsDev(); // Dev-only condition
        if (SponsorOnly) // Sponsor-only condition
        {
            switch (SponsorTier)
            {
                case 3:
                    return Main.MyData.IsSponsorTier3();
                case 2:
                    return Main.MyData.IsSponsorTier2();
                case 1:
                    return Main.MyData.IsSponsorTier1();
            }
        }

        return true; // Default case: no restrictions
    }
}

/// <summary>
/// Extension methods for CustomCosmeticConfig to retrieve cosmetic configurations based on the associated parent or cosmetic data.
/// </summary>
internal static class CustomCosmeticConfigurationExtension
{
    internal static CustomCosmeticConfig TryGetConfig(this SkinLayer parent) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == parent.data.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this SkinData skin) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == skin.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this HatParent parent) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == parent.Hat.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this HatData hat) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == hat.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this VisorLayer parent) =>
    ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == parent.visorData.ProductId)
    ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this VisorData visor) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == visor.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];

    internal static CustomCosmeticConfig TryGetConfig(this NamePlateData namePlate) =>
        ReadOnlyManager.AllCustomCosmeticConfigurations.FirstOrDefault(config => config.Id == namePlate.ProductId)
        ?? ReadOnlyManager.AllCustomCosmeticConfigurations[0];
}