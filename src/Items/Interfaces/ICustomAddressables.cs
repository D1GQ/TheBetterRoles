using UnityEngine.ResourceManagement.AsyncOperations;

namespace TheBetterRoles.Items.Interfaces;

internal interface ICustomAddressables
{
    string Guid { get; }
    AsyncOperationHandle LoadAssetAsync();
}