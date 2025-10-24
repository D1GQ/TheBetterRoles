using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Managers;

class TBRNotificationManager
{
    internal static GameObject? TBRNotificationManagerObj;
    internal static TextMeshPro? NameText;
    internal static TextMeshPro? TextArea => TBRNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>();
    internal static Dictionary<string, float> NotifyQueue = [];
    internal static float showTime = 0f;
    private static Camera? localCamera;
    internal static bool Notifying = false;

    internal static void Notify(string text, float Time = 5f)
    {
        if (TBRNotificationManagerObj != null)
        {
            if (Notifying)
            {
                if (text == TBRNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text)
                    return;
                NotifyQueue[text] = Time;
                return;
            }

            showTime = Time;
            TBRNotificationManagerObj.SetActive(true);
            NameText.text = $"<color=#00ff44>{Translator.GetString("SystemNotification")}</color>";
            TextArea.text = text;
            SoundManager.Instance.PlaySound(HudManager.Instance.TaskCompleteSound, false, 1f);
            Notifying = true;
        }
    }

    internal static void LateUpdate()
    {
        if (TBRNotificationManagerObj != null)
        {
            if (!localCamera)
            {
                if (HudManager.InstanceExists)
                {
                    localCamera = HudManager.Instance.GetComponentInChildren<Camera>();
                }
                else
                {
                    localCamera = Camera.main;
                }
            }

            TBRNotificationManagerObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.Bottom, new Vector3(-1.3f, 0.7f, localCamera.nearClipPlane + 0.1f));

            showTime -= Time.deltaTime;
            if (showTime <= 0f && GameState.IsInGame)
            {
                TBRNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text = "";
                TBRNotificationManagerObj.SetActive(false);
                Notifying = false;

                CheckNotifyQueue();
            }

            if (!GameState.IsInGame)
            {
                TBRNotificationManagerObj.SetActive(false);
                showTime = 0f;
            }
        }
    }

    private static void CheckNotifyQueue()
    {
        if (NotifyQueue.Any())
        {
            var key = NotifyQueue.Keys.First();
            var value = NotifyQueue[key];
            Notify(key, value);
            NotifyQueue.Remove(key);
        }
    }
}
