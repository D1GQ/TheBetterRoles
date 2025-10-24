using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Minigames;

[HarmonyPatch(typeof(HauntMenuMinigame))]
internal class HauntMenuMinigamePatch
{
    private static CustomHauntFilters HauntingTeam = CustomHauntFilters.All;
    private static List<PassiveButton> SetTeamButtons = [];

    [HarmonyPatch(nameof(HauntMenuMinigame.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(HauntMenuMinigame __instance)
    {
        SetTeamButtons.Clear();
        HauntingTeam = CustomHauntFilters.All;
        __instance.FilterButtons[0].gameObject.SetActive(false);
        __instance.FilterButtons[1].gameObject.SetActive(false);
        __instance.FilterButtons[2].gameObject.SetActive(false);
        foreach (var item in __instance.Arrows)
        {
            item.transform.position += new Vector3(0f, 0.2f, 0f);
            var Collider2D = item.gameObject.GetComponent<CircleCollider2D>();
            Collider2D.radius = 0.3f;
            Collider2D.offset = new Vector2(0f, -0.01f);

            var obj = UnityEngine.Object.Instantiate(item.gameObject, __instance.transform);
            var button = obj.GetComponent<PassiveButton>();
            button.OnClick = new();

            int flag = SetTeamButtons.Any() ? -1 : 1;
            button.OnClick.AddListener((Action)(() =>
            {
                UpdateTeam(__instance, flag);
            }));

            button.name = "Team Button";
            button.transform.position += new Vector3(0f, 0.6f, 0f);
            SetTeamButtons.Add(button);
        }
        __instance.FilterText.transform.position += new Vector3(0f, 0.15f, 0f);
        __instance.NameText.transform.position += new Vector3(0f, 0.15f, 0f);

        __instance.HauntingText.DestroyTextTranslators();
        __instance.HauntingText.transform.position += new Vector3(0f, 0.5f, 0f);
        __instance.HauntingText.transform.localScale += new Vector3(0.5f, 0.5f, 0.5f);
        UpdateText(__instance);
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.MatchesFilter))]
    [HarmonyPrefix]
    private static bool MatchesFilter_Prefix(HauntMenuMinigame __instance, [HarmonyArgument(0)] PlayerControl pc, ref bool __result)
    {
        if (!pc.isActiveAndEnabled)
        {
            __result = false;
            return false;
        }

        switch (HauntingTeam)
        {
            case CustomHauntFilters.Impostor:
                __result = pc.Is(RoleClassTeam.Impostor) && !pc.Data.IsDead;
                break;
            case CustomHauntFilters.Crewmate:
                __result = pc.Is(RoleClassTeam.Crewmate) && !pc.Data.IsDead;
                break;
            case CustomHauntFilters.Neutral:
                __result = pc.Is(RoleClassTeam.Neutral) && !pc.Data.IsDead;
                break;
            case CustomHauntFilters.All:
                __result = !pc.Data.IsDead;
                break;
            case CustomHauntFilters.Dead:
                __result = pc.Data.IsDead;
                break;
            default:
                __result = true;
                break;
        }

        return false;
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.FixedUpdate))]
    [HarmonyPostfix]
    private static void MatchesFilter_Postfix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && !__instance.HauntTarget.isActiveAndEnabled)
        {
            __instance.SetHauntTarget(null);
        }
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.SetFilterText))]
    [HarmonyPrefix]
    private static bool SetFilterText_Prefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget.ExtendedData().RoleInfo.RoleAssigned)
        {
            __instance.FilterText.text = __instance.HauntTarget.GetRoleName();
            __instance.FilterText.color = __instance.HauntTarget.GetRoleColor();
        }
        else
        {
            __instance.FilterText.text = "???";
        }

        return false;
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.SetFilter))]
    [HarmonyPrefix]
    private static bool SetFilter_Prefix(HauntMenuMinigame __instance, [HarmonyArgument(0)] int filterInt)
    {
        HauntMenuMinigame.HauntFilters hauntFilters = (HauntMenuMinigame.HauntFilters)(CustomHauntFilters)filterInt;
        if (hauntFilters == __instance.filterMode)
        {
            hauntFilters = HauntMenuMinigame.HauntFilters.None;
            __instance.FilterButtons[__instance.filterMode - HauntMenuMinigame.HauntFilters.Impostor].GetComponent<ButtonRolloverHandler>().WaitClickUnselect();
        }
        if (__instance.filterMode != HauntMenuMinigame.HauntFilters.None)
        {
            __instance.FilterButtons[__instance.filterMode - HauntMenuMinigame.HauntFilters.Impostor].GetComponent<ButtonRolloverHandler>().ChangeOutColor(Color.clear);
        }
        __instance.filterMode = hauntFilters;
        if (__instance.filterMode != HauntMenuMinigame.HauntFilters.None)
        {
            __instance.FilterButtons[__instance.filterMode - HauntMenuMinigame.HauntFilters.Impostor].GetComponent<ButtonRolloverHandler>().ChangeOutColor(Color.white);
        }
        __instance.ChangePick(0);

        return false;
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.ChangePick))]
    [HarmonyPostfix]
    private static void ChangePost_Postfix(HauntMenuMinigame __instance)
    {
        __instance.Arrows.ToList().ForEach(a => a.gameObject.SetActive(true));
    }

    [HarmonyPatch(nameof(HauntMenuMinigame.SetHauntTarget))]
    [HarmonyPostfix]
    private static void SetHauntTarget_Postfix(HauntMenuMinigame __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (target == null)
        {
            __instance.HauntTarget = null;
            __instance.NameText.text = $"<color=red>{Translator.GetString("Haunt.NoPlayers")}</color>";
            __instance.FilterText.text = "";
            __instance.HauntingText.enabled = true;
        }
    }

    private static void UpdateTeam(HauntMenuMinigame __instance, int Direction)
    {
        int filterCount = Enum.GetValues(typeof(CustomHauntFilters)).Length;
        int currentFilter = (int)HauntingTeam;

        if (Direction > 0)
        {
            currentFilter = (currentFilter + 1) % filterCount; // Forward (increment)
        }
        else if (Direction < 0)
        {
            currentFilter = (currentFilter - 1 + filterCount) % filterCount; // Backward (decrement)
        }

        HauntingTeam = (CustomHauntFilters)currentFilter;
        UpdateText(__instance);
    }

    private static void UpdateText(HauntMenuMinigame __instance)
    {
        var team = Translator.GetString("Team") + ": ";
        switch (HauntingTeam)
        {
            case CustomHauntFilters.Impostor:
                __instance.HauntingText.text = team + Translator.GetString(StringNames.ImpostorsCategory);
                break;
            case CustomHauntFilters.Crewmate:
                __instance.HauntingText.text = team + Translator.GetString(StringNames.Crewmates);
                break;
            case CustomHauntFilters.Neutral:
                __instance.HauntingText.text = team + Translator.GetString("Neutrals");
                break;
            case CustomHauntFilters.All:
                __instance.HauntingText.text = Translator.GetString(StringNames.RoleSettingsAll);
                break;
            case CustomHauntFilters.Dead:
                __instance.HauntingText.text = Translator.GetString("Dead");
                break;
        }
    }
}