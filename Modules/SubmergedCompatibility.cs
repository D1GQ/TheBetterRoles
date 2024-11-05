using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using UnityEngine;

namespace TheBetterRoles.Modules;

[HarmonyPatch(typeof(ShipStatus))]
class SubmergedShipStatusPatch
{
    [HarmonyPatch(nameof(ShipStatus.Awake))]
    [HarmonyPostfix]
    public static void Awake_Postfix(ShipStatus __instance)
    {
        SubmergedCompatibility.SetupMap(__instance);
    }

    [HarmonyPatch(nameof(ShipStatus.OnDestroy))]
    [HarmonyPostfix]
    public static void OnDestroy_Postfix(/*ShipStatus __instance*/)
    {
        SubmergedCompatibility.SetupMap(null);
    }
}

public static class SubmergedCompatibility
{
    public static class Classes
    {
        public const string ElevatorMover = "ElevatorMover";
    }

    public const string SUBMERGED_GUID = "Submerged";
    public const ShipStatus.MapType SUBMERGED_MAP_TYPE = (ShipStatus.MapType)6;

    public static SemanticVersioning.Version? Version { get; private set; }
    public static bool Loaded { get; private set; }
    public static bool LoadedExternally { get; private set; }
    public static BasePlugin? Plugin { get; private set; }
    public static Assembly? Assembly { get; private set; }
    public static Type[]? Types { get; private set; }
    public static Dictionary<string, Type>? InjectedTypes { get; private set; }
    public static MonoBehaviour? SubmarineStatus { get; private set; }
    public static bool IsSubmerged { get; private set; }

    // Cached Types and Methods
    private static Type? SubmarineStatusType;
    private static MethodInfo? CalculateLightRadiusMethod;
    private static MethodInfo? RpcRequestChangeFloorMethod;
    private static Type? FloorHandlerType;
    private static MethodInfo? GetFloorHandlerMethod;
    private static Type? VentPatchDataType;
    private static PropertyInfo? InTransitionProperty;
    private static Type? CustomTaskTypesType;
    private static FieldInfo? RetrieveOxygenMaskField;
    public static TaskTypes? RetrieveOxygenMask;
    private static Type? SubmarineOxygenSystemType;
    private static MethodInfo? SubmarineOxygenSystemInstanceMethod;
    private static MethodInfo? RepairDamageMethod;

    public static void SetupMap(ShipStatus? map)
    {
        if (map == null)
        {
            IsSubmerged = false;
            SubmarineStatus = null;
            return;
        }

        IsSubmerged = map.Type == SUBMERGED_MAP_TYPE;

        if (IsSubmerged)
        {
            SubmarineStatus = map.GetComponent(Il2CppType.From(SubmarineStatusType))?.TryCast<MonoBehaviour>();
        }
    }

    public static bool TryLoadSubmerged()
    {
        try
        {
            Logger.Log("Trying to load Submerged...");
            var thisAsm = Assembly.GetCallingAssembly();
            var resourceName = thisAsm.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith("Submerged.dll"));
            if (resourceName == null) return false;

            using var submergedStream = thisAsm.GetManifestResourceStream(resourceName)!;
            var assemblyBuffer = new byte[submergedStream.Length];
            submergedStream.Read(assemblyBuffer, 0, assemblyBuffer.Length);
            Assembly = Assembly.Load(assemblyBuffer);

            var pluginType = Assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(BasePlugin)));
            if (pluginType == null) return false;

            Plugin = (BasePlugin)Activator.CreateInstance(pluginType)!;
            Plugin.Load();

            Version = pluginType.GetCustomAttribute<BepInPlugin>()?.Version.BaseVersion();

            IL2CPPChainloader.Instance.Plugins[SUBMERGED_GUID] = new();
            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return false;
        }
    }

    public static void Initialize()
    {
        Loaded = IL2CPPChainloader.Instance.Plugins.TryGetValue(SUBMERGED_GUID, out var plugin);
        if (!Loaded)
        {
            Loaded = TryLoadSubmerged();
            if (!Loaded) return;
        }
        else
        {
            LoadedExternally = true;
            Plugin = plugin!.Instance as BasePlugin;
            Version = plugin.Metadata.Version.BaseVersion();
            Assembly = Plugin?.GetType().Assembly;
        }

        Types = AccessTools.GetTypesFromAssembly(Assembly!);
        InjectedTypes = AccessTools.PropertyGetter(Types!.First(t => t.Name == "ComponentExtensions"), "RegisteredTypes")
            ?.Invoke(null, Array.Empty<object>()) as Dictionary<string, Type>;

        SubmarineStatusType = Types.First(t => t.Name == "SubmarineStatus");
        CalculateLightRadiusMethod = AccessTools.Method(SubmarineStatusType, "CalculateLightRadius");

        FloorHandlerType = Types.First(t => t.Name == "FloorHandler");
        GetFloorHandlerMethod = AccessTools.Method(FloorHandlerType, "GetFloorHandler", new[] { typeof(PlayerControl) });
        RpcRequestChangeFloorMethod = AccessTools.Method(FloorHandlerType, "RpcRequestChangeFloor");

        VentPatchDataType = Types.First(t => t.Name == "VentPatchData");
        InTransitionProperty = AccessTools.Property(VentPatchDataType, "InTransition");

        CustomTaskTypesType = Types.First(t => t.Name == "CustomTaskTypes");
        RetrieveOxygenMaskField = AccessTools.Field(CustomTaskTypesType, "RetrieveOxygenMask");
        var taskTypeField = AccessTools.Field(CustomTaskTypesType, "taskType");
        var oxygenMaskCustomTaskType = RetrieveOxygenMaskField?.GetValue(null);
        RetrieveOxygenMask = (TaskTypes?)taskTypeField?.GetValue(oxygenMaskCustomTaskType);

        SubmarineOxygenSystemType = Types.First(t => t.Name == "SubmarineOxygenSystem" && t.Namespace == "Submerged.Systems.Oxygen");
        SubmarineOxygenSystemInstanceMethod = AccessTools.PropertyGetter(SubmarineOxygenSystemType, "Instance");
        RepairDamageMethod = AccessTools.Method(SubmarineOxygenSystemType, "RepairDamage");
    }

    public static MonoBehaviour AddSubmergedComponent(this GameObject obj, string typeName)
    {
        if (!Loaded) return obj.AddComponent<MissingSubmergedBehaviour>();

        if (InjectedTypes?.TryGetValue(typeName, out var type) == true)
        {
            return obj.AddComponent(Il2CppType.From(type))?.TryCast<MonoBehaviour>()!;
        }

        return obj.AddComponent<MissingSubmergedBehaviour>();
    }

    public static float GetSubmergedNeutralLightRadius(bool isImpostor)
    {
        if (!Loaded || CalculateLightRadiusMethod == null) return 0;
        return (float)CalculateLightRadiusMethod.Invoke(SubmarineStatus, new object[] { null, true, isImpostor })!;
    }

    public static void ChangeFloor(bool toUpper)
    {
        if (!Loaded || GetFloorHandlerMethod == null || RpcRequestChangeFloorMethod == null) return;

        var floorHandler = GetFloorHandlerMethod.Invoke(null, new object[] { PlayerControl.LocalPlayer });
        if (floorHandler != null)
        {
            var mono = ((Component)floorHandler).TryCast<MonoBehaviour>();
            RpcRequestChangeFloorMethod.Invoke(mono, new object[] { toUpper });
        }
    }

    public static bool GetInTransition()
    {
        if (!Loaded || InTransitionProperty == null) return false;
        return (bool)InTransitionProperty.GetValue(null)!;
    }

    public static void RepairOxygen()
    {
        if (!Loaded || SubmarineOxygenSystemInstanceMethod == null || RepairDamageMethod == null) return;

        try
        {
            ShipStatus.Instance?.RpcUpdateSystem((SystemTypes)130, 64);
            var instance = SubmarineOxygenSystemInstanceMethod.Invoke(null, Array.Empty<object>());
            RepairDamageMethod.Invoke(instance, new object[] { PlayerControl.LocalPlayer, 64 });
        }
        catch { }
    }
}

public class MissingSubmergedBehaviour : MonoBehaviour
{
    static MissingSubmergedBehaviour() => ClassInjector.RegisterTypeInIl2Cpp<MissingSubmergedBehaviour>();
    public MissingSubmergedBehaviour(IntPtr ptr) : base(ptr) { }
}
