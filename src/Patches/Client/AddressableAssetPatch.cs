using BepInEx.Unity.IL2CPP.Hook;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using System.Runtime.InteropServices;
using TheBetterRoles.Items;
using UnityEngine.AddressableAssets;

namespace TheBetterRoles.Patches.Client;

internal static unsafe class AddressableAssetPatch
{
    internal static class LoadAssetPatch
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LoadAssetAsyncDel(IntPtr intPtr, IntPtr keyPtr, Il2CppMethodInfo* methodInfo);

        private static INativeDetour? detour;
        private static LoadAssetAsyncDel? originalMethod;

        internal static void Patch()
        {
            var methodInfoPtr = (Il2CppMethodInfo*)(IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(Addressables).GetMethod(nameof(Addressables.LoadAssetAsync), AccessTools.all, [typeof(UnityEngine.Object)])!
                .MakeGenericMethod(typeof(UnityEngine.Object)))
                .GetValue(null)!;

            var methodInfo = UnityVersionHandler.Wrap(methodInfoPtr);
            var methodPtr = methodInfo.MethodPointer;

            detour = INativeDetour.CreateAndApply(methodPtr, Detour, out originalMethod);
        }


        private static IntPtr Detour(IntPtr intPtr, IntPtr keyPtr, Il2CppMethodInfo* methodInfo)
        {
            var keyClassPtr = IL2CPP.il2cpp_object_get_class(keyPtr);

            string assetGuid = null;

            if (keyClassPtr == Il2CppClassPointerStore<AssetReference>.NativeClassPtr)
            {
                assetGuid = new AssetReference(keyPtr).AssetGUID;
            }
            else if (keyClassPtr == Il2CppClassPointerStore<string>.NativeClassPtr)
            {
                assetGuid = IL2CPP.Il2CppStringToManaged(keyPtr);
            }

            if (assetGuid == null) return originalMethod!.Invoke(intPtr, keyPtr, methodInfo);

            if (!assetGuid.StartsWith(CustomAddressables.GUID_PREFIX)) return originalMethod!.Invoke(intPtr, keyPtr, methodInfo);

            var addressable = CustomAddressables.GetCustomAddressablesByGuid(assetGuid);
            if (addressable == null) return originalMethod!.Invoke(intPtr, keyPtr, methodInfo);

            var operation = addressable.LoadAssetAsync();
            if (operation.IsValid())
            {
                return IL2CPP.il2cpp_object_unbox(operation.Pointer);
            }

            return originalMethod!.Invoke(intPtr, keyPtr, methodInfo);
        }
    }
}

internal static class RuntimeKeyPatch
{
    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
    private static class AssetReferencePatch
    {
        private static bool Prefix(AssetReference __instance, ref bool __result)
        {
            __result = RuntimeKeyIsValidOriginal(__instance) || CustomAddressables.IsValid(__instance);

            return false;
        }

        [HarmonyReversePatch]
        private static bool RuntimeKeyIsValidOriginal(AssetReference instance) => throw new NotSupportedException();
    }
}
