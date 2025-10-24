using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using TheBetterRoles.Helpers;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class TipTracker : MonoBehaviour
{
    internal static TipTracker Instance { get; private set; }
    internal GameObject TipContainer { get; private set; }
    internal TextMeshPro TipText { get; private set; }
    internal AspectPosition AspectPosition { get; private set; }

    private Coroutine slideRoutine;
    private bool isTipVisible;

    private float lastYOffset = 0f;
    private float YOffset = 0f;
    internal static Vector3 TipVisiblePosition => new(0, 0.8f, 0);
    internal static Vector3 TipHiddenPosition => new(0, -1f, 0);

    internal void Start()
    {
        Instance = this;

        TipContainer = new GameObject("TipContainer");
        TipContainer.transform.SetParent(transform);

        AspectPosition = TipContainer.AddComponent<AspectPosition>();
        AspectPosition.DistanceFromEdge = Vector3.zero;
        AspectPosition.Alignment = AspectPosition.EdgeAlignments.Bottom;

        TipText = Instantiate(HudManager.Instance.roomTracker.text, TipContainer.transform);
        TipText.enableAutoSizing = false;
        TipText.fontSize = 2f;
        TipText.gameObject.GetComponent<RoomTracker>().DestroyMono();
        TipText.gameObject.name = "TipTrackerText";
        TipText.text = string.Empty;

        TipContainer.SetActive(false);
    }

    internal void SetTip(string tip, bool Override = false, float offset = 0f)
    {
        if (!Override && isTipVisible) return;

        lastYOffset = YOffset;
        YOffset = offset;
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
        }
        slideRoutine = this.StartCoroutine(UpdateTipRoutine(tip));
    }

    internal void ClearTip()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
        }
        slideRoutine = this.StartCoroutine(CoSlideOut());
    }

    [HideFromIl2Cpp]
    private IEnumerator UpdateTipRoutine(string tip)
    {
        if (isTipVisible)
        {
            yield return CoSlideOut();
        }

        TipText.text = tip;
        TipContainer.SetActive(true);
        yield return CoSlideIn();
    }

    [HideFromIl2Cpp]
    private IEnumerator CoSlideIn()
    {
        yield return CoSlide(TipHiddenPosition, new(TipVisiblePosition.x, TipVisiblePosition.y + YOffset, TipVisiblePosition.z));
        isTipVisible = true;
    }

    [HideFromIl2Cpp]
    private IEnumerator CoSlideOut()
    {
        yield return CoSlide(new(TipVisiblePosition.x, TipVisiblePosition.y + lastYOffset, TipVisiblePosition.z), TipHiddenPosition);
        TipContainer.SetActive(false);
        isTipVisible = false;
    }

    [HideFromIl2Cpp]
    private IEnumerator CoSlide(Vector3 start, Vector3 end)
    {
        float duration = 0.25f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            TipContainer.transform.localPosition = Vector3.Lerp(start, end, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        TipContainer.transform.localPosition = end;
    }
}