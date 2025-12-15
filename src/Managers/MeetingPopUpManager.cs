using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core.Interfaces;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Managers;

/// <summary>
/// Manages all meeting-related pop-up text displays with animations and sound effects.
/// Handles the collection, prioritization, and sequential display of role-specific messages
/// during meetings with fly-in and fade-out animations.
/// </summary>
internal static class MeetingPopUpManager
{
    private static Coroutine _displayCoroutine;
    private static readonly List<TextMeshPro> _textPros = new();
    private static TextMeshPro _textTemplate;
    private static MeetingHud _currentMeeting;

    internal static void Start(MeetingHud meetingHud)
    {
        if (_displayCoroutine != null && _currentMeeting != null)
        {
            _currentMeeting.StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }

        ClearTexts();

        _currentMeeting = meetingHud;

        CreateTextTemplate(meetingHud);

        var texts = CollectRoleTexts();

        if (texts.Count > 0)
        {
            _displayCoroutine = meetingHud.StartCoroutine(CoDisplayTextsQueue(meetingHud, texts));
        }
    }

    private static void Stop()
    {
        if (_displayCoroutine != null && _currentMeeting != null)
        {
            _currentMeeting.StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }

        ClearTexts();
        _currentMeeting = null;
    }

    private static void CreateTextTemplate(MeetingHud meetingHud)
    {
        if (_textTemplate != null)
        {
            if (_textTemplate.gameObject != null)
                UnityEngine.Object.Destroy(_textTemplate.gameObject);
            _textTemplate = null;
        }

        _textTemplate = UnityEngine.Object.Instantiate(meetingHud.TitleText, meetingHud.transform);
        _textTemplate.name = "MeetingTextTemplate";

        var pos = _textTemplate.gameObject.AddComponent<AspectPosition>();
        pos.Alignment = AspectPosition.EdgeAlignments.Center;
        pos.DistanceFromEdge = new Vector3(0f, -2f, -10f);

        _textTemplate.DestroyTextTranslators();
        _textTemplate.enableAutoSizing = false;
        _textTemplate.fontSize = 2f;
        _textTemplate.text = "Text Here";
        _textTemplate.gameObject.SetActive(false);
    }

    private static Dictionary<string, CustomClip?> CollectRoleTexts()
    {
        var texts = new Dictionary<string, CustomClip?>();

        // Collect role texts with priority
        var roleTexts = new List<(string text, CustomClip? clip, uint priority)>();

        RoleListener.InvokeRoles<IRoleMeetingAction>(role =>
        {
            CustomClip? clip = null;
            string text = role.AddMeetingText(ref clip, out uint priority);
            if (!string.IsNullOrEmpty(text))
            {
                roleTexts.Add((text, clip, priority));
            }
        });

        // Sort by priority (descending)
        roleTexts.Sort((a, b) => b.priority.CompareTo(a.priority));

        foreach (var (sortedText, sortedClip, _) in roleTexts)
        {
            texts[sortedText] = sortedClip;
        }

        return texts;
    }

    private static IEnumerator CoDisplayTextsQueue(MeetingHud meetingHud, Dictionary<string, CustomClip?> texts)
    {
        // Wait for vote animation to complete
        while (true)
        {
            if (meetingHud == null || meetingHud.state != MeetingHud.VoteStates.Animating)
            {
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Display each text with its associated sound
        foreach (var kvp in texts)
        {
            var textPro = CreateTextObject(meetingHud, kvp.Key);
            _textPros.Add(textPro);

            PlayClip(kvp.Value);

            yield return meetingHud.StartCoroutine(CoAnimateText(textPro));

            RemoveTextObject(textPro);
        }

        Stop();
    }

    private static TextMeshPro CreateTextObject(MeetingHud meetingHud, string textContent)
    {
        var textPro = UnityEngine.Object.Instantiate(_textTemplate, meetingHud.transform);
        textPro.name = $"MeetingText({_textPros.Count})";
        textPro.gameObject.SetActive(true);
        textPro.text = textContent;
        return textPro;
    }

    private static void RemoveTextObject(TextMeshPro textPro)
    {
        _textPros.Remove(textPro);
        if (textPro != null && textPro.gameObject != null)
        {
            UnityEngine.Object.Destroy(textPro.gameObject);
        }
    }

    private static void PlayClip(CustomClip? clip)
    {
        if (clip == null) return;

        if (!string.IsNullOrEmpty(clip.ClipName))
        {
            CustomSoundsManager.Instance.Play(clip.ClipName, clip.Volume);
        }
        else if (clip.Clip != null)
        {
            SoundManager.Instance.PlaySound(clip.Clip, false, clip.Volume);
        }
    }

    private static IEnumerator CoAnimateText(TextMeshPro text)
    {
        const float DISPLAY_DURATION = 6f;
        const float FADE_DURATION = 1.5f;
        const float ANIMATION_DURATION = 0.15f;

        Vector3 originalScale = text.transform.localScale;
        Vector3 enlargedScale = originalScale * 10f;
        Color originalColor = text.color;

        // Fly-in animation
        text.transform.localScale = enlargedScale;

        float elapsed = 0f;
        while (elapsed < ANIMATION_DURATION)
        {
            if (text == null) yield break;

            text.transform.localScale = Vector3.Lerp(
                enlargedScale,
                originalScale,
                elapsed / ANIMATION_DURATION
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (text != null)
        {
            text.transform.localScale = originalScale;
        }

        // Display duration
        yield return new WaitForSeconds(DISPLAY_DURATION);

        // Fade-out animation
        if (text == null) yield break;

        elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            if (text == null) yield break;

            float alpha = Mathf.Lerp(1, 0, elapsed / FADE_DURATION);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (text != null)
        {
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }
    }

    private static void ClearTexts()
    {
        foreach (var textPro in _textPros.ToArray())
        {
            RemoveTextObject(textPro);
        }
        _textPros.Clear();
    }

    public static bool IsActive()
    {
        return _displayCoroutine != null;
    }
}