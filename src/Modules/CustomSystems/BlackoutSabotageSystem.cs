using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;
using System.Collections;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Modules;

[RegisterInIl2Cpp(typeof(ISystemType), typeof(IActivatable))]
internal class BlackoutSabotageSystem : BaseSystem, IISystemType, IIActivatable
{
    internal static CustomSystemTypes Type => CustomSystemTypes.Blackout;
    public bool IsDirty { get; private set; }
    public bool IsActive { get; private set; }
    internal bool DirtyClear { get; private set; }

    internal static BlackoutSabotageSystem? Instance { get; private set; }

    /// <summary>
    /// Integrate custom system into AU systems.
    /// </summary>
    internal static void AddSystem()
    {
        if (Instance != null) return;
        var blackoutSabotage = ShipStatus.Instance.gameObject.AddComponent<BlackoutSabotageSystem>();
        ShipStatus.Instance.Systems.Add(Type, blackoutSabotage.Cast<ISystemType>());
        Instance = blackoutSabotage;
    }

    /// <summary>
    /// Activates blackout sabotage.
    /// </summary>
    internal static void ActivateSabotage()
    {
        ShipStatus.Instance.RpcUpdateSystem(Type, 16);
    }

    /// <summary>
    /// Deactivates blackout sabotage if active.
    /// </summary>
    internal static void DeactivateSabotage() => Instance?.ClearSabotage();

    internal bool IsDecreasingVision { get; private set; }
    internal bool IsIncreasingVision { get; private set; }
    private float blackTime => 5f;
    private float outTime => 10f;
    internal float Timer { get; private set; }
    internal float VisionSize { get; private set; } = 1f;

    internal override void SetUp()
    {
        SetAsSabotage();
    }

    internal override void Destroy()
    {
        Instance = null;
    }

    internal override void OnMeetingStart()
    {
        if (IsActive)
        {
            DeactivateSabotage();
        }
    }

    public void Deteriorate(float deltaTime)
    {
        if (IsActive)
        {
            if (!IsDecreasingVision)
            {
                IsDecreasingVision = true;
                FlickerVisionCoroutine = this.StartCoroutine(CoFlickerVision());
            }

            Timer += deltaTime;

            if (Timer > blackTime)
            {
                if (!IsIncreasingVision)
                {
                    IsIncreasingVision = true;
                    IncreaseVisionCoroutine = this.StartCoroutine(CoIncreaseVision());
                }
            }
        }
    }

    public void UpdateSystem(PlayerControl player, MessageReader msgReader)
    {
        byte count = msgReader.ReadByte();
        if (!IsActive && count == 16)
        {
            InitiateBlackout();
        }
    }

    private void InitiateBlackout()
    {
        Timer = 0f;
        IsActive = true;
        IsDirty = true;
    }

    private Coroutine? FlickerVisionCoroutine;

    [HideFromIl2Cpp]
    private IEnumerator CoFlickerVision()
    {
        float wait = 0.1f;
        VisionSize = 0.8f;
        yield return new WaitForSeconds(wait);
        VisionSize = 0.6f;
        yield return new WaitForSeconds(wait);
        VisionSize = 0.4f;
        yield return new WaitForSeconds(wait);
        VisionSize = 0.2f;
        yield return new WaitForSeconds(wait);
        VisionSize = 0f;

        FlickerVisionCoroutine = null;
    }

    private Coroutine? IncreaseVisionCoroutine;

    [HideFromIl2Cpp]
    private IEnumerator CoIncreaseVision()
    {
        float startVision = VisionSize;
        float endVision = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < outTime)
        {
            VisionSize = Mathf.Lerp(startVision, endVision, elapsedTime / outTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        VisionSize = endVision;
        IncreaseVisionCoroutine = null;
        ClearSabotage();
    }

    internal void ClearSabotage()
    {
        if (FlickerVisionCoroutine != null) StopCoroutine(FlickerVisionCoroutine);
        if (IncreaseVisionCoroutine != null) StopCoroutine(IncreaseVisionCoroutine);
        VisionSize = 1f;
        IsIncreasingVision = false;
        IsDecreasingVision = false;
        IsActive = false;

        if (GameState.IsHost)
        {
            DirtyClear = true;
            IsDirty = true;
        }
    }

    public void Serialize(MessageWriter writer, bool initialState)
    {
        writer.Write(IsActive);
        writer.Write(DirtyClear);
        DirtyClear = false;
        IsDirty = false;
    }

    public void Deserialize(MessageReader reader, bool initialState)
    {
        var active = IsActive;
        IsActive = reader.ReadBoolean();
        if (!active && IsActive)
        {
            Timer = 0;
        }
        bool dirtyFix = reader.ReadBoolean();
        if (dirtyFix)
        {
            ClearSabotage();
        }
    }
}
