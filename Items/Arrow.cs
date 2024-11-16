using HarmonyLib;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items;

class ArrowLocator
{
    public static List<ArrowLocator> allArrows { get; set; } = [];
    public bool HasPlayerTarget { get; set; }
    public PlayerControl? PlayerTarget { get; set; }
    public ArrowBehaviour? Arrow { get; set; }
    public SpriteRenderer? SpriteRenderer { get; set; }

    public ArrowLocator Create(Vector3 pos = default, Sprite? sprite = null, Color color = default, float maxScale = 1f, float minDistance = 0.5f)
    {
        pos = pos == default ? new Vector3(0, 0, 0) : pos;
        color = color == default ? Color.white : color;

        var obj = new GameObject
        {
            name = "ArrowLocator"
        };
        obj.transform.SetParent(HudManager.Instance.transform);
        obj.AddComponent<SpriteRenderer>();
        ArrowBehaviour arrow = obj.AddComponent<ArrowBehaviour>();
        Arrow = arrow;
        Arrow.MaxScale = maxScale;
        Arrow.minDistanceToShowArrow = minDistance;
        SpriteRenderer = arrow.image;
        if (sprite == null)
        {
            Arrow.image.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Icons.Arrow.png", 100f);
        }
        else
        {
            Arrow.image.sprite = sprite;
        }
        Arrow.image.color = color;
        Arrow.target = pos;

        allArrows.Add(this);
        return this;
    }

    public void SetPlayer(PlayerControl player)
    {
        HasPlayerTarget = true;
        PlayerTarget = player;
    }

    public void Remove()
    {
        allArrows.Remove(this);
        Arrow.DestroyObj();
    }

    [HarmonyPatch(typeof(ArrowBehaviour))]
    class ArrowBehaviourPatch
    {
        [HarmonyPatch(nameof(ArrowBehaviour.UpdatePosition))]
        [HarmonyPostfix]
        public static void UpdatePosition_Postfix(ArrowBehaviour __instance)
        {
            if (allArrows.Select(a => a.Arrow).Contains(__instance))
            {
                foreach (var arrow in allArrows)
                {
                    if (arrow.PlayerTarget != null && arrow.HasPlayerTarget && arrow.PlayerTarget.IsAlive())
                    {
                        arrow.Arrow.target = arrow.PlayerTarget.GetTruePosition() + new Vector2(0f, 0.25f);
                    }
                    else if (arrow.HasPlayerTarget)
                    {
                        arrow.Remove();
                    }
                }
                __instance.gameObject.transform.position = new Vector3(__instance.gameObject.transform.position.x, __instance.gameObject.transform.position.y, -100f);
            }
        }
    }
}
