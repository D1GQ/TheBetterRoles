using TMPro;
using UnityEngine;

namespace TheBetterRoles;

class BetterNotificationManager
{
    public static GameObject? BAUNotificationManagerObj;
    public static TextMeshPro? NameText;
    public static TextMeshPro? TextArea => BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>();
    public static Dictionary<string, float> NotifyQueue = [];
    public static float showTime = 0f;
    private static Camera? localCamera;
    public static bool Notifying = false;

    public static void Notify(string text, float Time = 5f)
    {
        if (BAUNotificationManagerObj != null)
        {
            if (Notifying)
            {
                if (text == BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text)
                    return;
                NotifyQueue[text] = Time;
                return;
            }

            showTime = Time;
            BAUNotificationManagerObj.SetActive(true);
            NameText.text = $"<color=#00ff44>{Translator.GetString("SystemNotification")}</color>";
            TextArea.text = text;
            SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
            Notifying = true;
        }
    }

    public static void Update()
    {
        if (BAUNotificationManagerObj != null)
        {
            if (!localCamera)
            {
                if (DestroyableSingleton<HudManager>.InstanceExists)
                {
                    localCamera = DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<Camera>();
                }
                else
                {
                    localCamera = Camera.main;
                }
            }

            BAUNotificationManagerObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.Bottom, new Vector3(-1.3f, 0.7f, localCamera.nearClipPlane + 0.1f));

            showTime -= Time.deltaTime;
            if (showTime <= 0f && GameStates.IsInGame)
            {
                BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text = "";
                BAUNotificationManagerObj.SetActive(false);
                Notifying = false;

                CheckNotifyQueue();
            }

            if (!GameStates.IsInGame)
            {
                BAUNotificationManagerObj.SetActive(false);
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
