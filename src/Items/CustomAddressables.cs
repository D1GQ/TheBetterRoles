using Reactor.Utilities.Extensions;
using TheBetterRoles.Items.Interfaces;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TheBetterRoles.Items;

internal class CustomAddressables
{
    internal const string GUID_PREFIX = "asset/guid:";
    internal static Dictionary<string, ICustomAddressables> Resources { get; set; } = [];

    internal static ICustomAddressables? GetCustomAddressablesByGuid(string guid)
    {
        if (Resources.TryGetValue(guid, out var addressables))
        {
            return addressables;
        }

        return null;
    }

    internal static bool IsValid(AssetReference asset) => asset.AssetGUID.StartsWith(GUID_PREFIX);
}

internal class CustomAddressables<T> : ICustomAddressables where T : UnityEngine.Object
{
    internal CustomAddressables(T item, string guid)
    {
        guid = guid.ToLower().Trim();
        Item = item;
        Item.DontDestroy();
        Guid = $"{CustomAddressables.GUID_PREFIX}{guid}";
        AssetReference = new AssetReference(Guid);
        CustomAddressables.Resources[Guid] = this;
    }

    internal T Item { get; set; }
    public string Guid { get; private set; }
    internal AssetReference AssetReference { get; private set; }

    private AsyncOperationHandle operation = default;

    public AsyncOperationHandle LoadAssetAsync()
    {
        if (Item != null)
        {
            operation = Addressables.ResourceManager.CreateCompletedOperation(Item, string.Empty);
            return operation;
        }

        Logger.Error("Item is null: LoadAssetAsync for CustomAddressables");

        return Addressables.ResourceManager.CreateCompletedOperation<T>(null, "Asset not found");
    }
}