using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;

namespace TheBetterRoles;

class ArrowLocator
{
    public static List<ArrowLocator> allArrows { get; set; } = new List<ArrowLocator>(); // Corrected initialization
    public ArrowBehaviour? Arrow { get; set; }

    public ArrowLocator Create(Vector3 pos = default, float maxScale = 1f, float minDistance = 0.5f, Color color = default)
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
        Arrow.image.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Arrow.png", 100f);
        Arrow.image.color = color;
        Arrow.target = pos;

        allArrows.Add(this);
        return this;
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
                __instance.gameObject.transform.position = new Vector3(__instance.gameObject.transform.position.x, __instance.gameObject.transform.position.y, -100f);
            }
        }
    }
    }
