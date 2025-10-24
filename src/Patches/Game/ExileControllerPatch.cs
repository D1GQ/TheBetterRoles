using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using System.Collections;
using System.Text;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch]
internal class ExileControllerPatch
{
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.HandleText))]
    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.HandleText))]
    [HarmonyPostfix]
    private static void HandleText_Postfix(ExileController __instance, [HarmonyArgument(0)] float firstWaitTime, [HarmonyArgument(1)] float animDuration, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        var init = __instance.initData;

        if (init != null && init.outfit != null)
        {
            if (VanillaGameSettings.ConfirmEjects.GetBool() || init.networkedPlayer.Role()?.AlwaysShowVoteOutMsg == true)
            {
                __instance.completeString = Translator.GetString("ConfirmEject", [init.outfit.PlayerName, $"{Utils.GetCustomRoleNameAndColor(init.networkedPlayer.Role().RoleType)}"]);
            }
            else
            {
                __instance.completeString = Translator.GetString(StringNames.ExileTextNonConfirm, [init.outfit.PlayerName]);
            }
        }

        __instance.ImpostorText.text = "";

        __result = CoHandleRichText(__instance, firstWaitTime, animDuration).WrapToIl2Cpp();
    }

    private static IEnumerator CoHandleRichText(ExileController __instance, float firstWaitTime, float animDuration)
    {
        yield return Effects.Wait(firstWaitTime);

        string completeString = __instance.completeString;
        string strippedString = Utils.RemoveHtmlText(completeString);
        StringBuilder animatedText = new();
        int visibleIndex = 0;
        int i = 0;
        float perCharTime = animDuration / strippedString.Length;

        while (visibleIndex < strippedString.Length)
        {
            float elapsedTime = 0f;
            while (elapsedTime < perCharTime)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (completeString[i] == '<')
            {
                int closingIndex = completeString.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    animatedText.Append(completeString.Substring(i, closingIndex - i + 1));
                    i = closingIndex + 1;
                    continue;
                }
            }

            animatedText.Append(completeString[i]);
            __instance.Text.text = animatedText.ToString();
            __instance.Text.gameObject.SetActive(true);

            if (completeString[i] != ' ')
            {
                SoundManager.Instance.PlaySoundImmediate(__instance.TextSound, false, 0.8f, 1f, null);
            }

            i++;
            visibleIndex++;
        }

        __instance.Text.text = completeString;
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    [HarmonyPrefix]
    private static bool WrapUp_Prefix(ExileController __instance)
    {
        try
        {
            RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.ExileEnd(__instance?.initData?.networkedPlayer?.Object, __instance?.initData?.networkedPlayer));
            PlayerControl.LocalPlayer.InvokeRoles(role => role.SetAllCooldowns());
            Utils.DirtyAllNames();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        if (__instance.initData.networkedPlayer != null)
        {
            PlayerControl @object = __instance.initData.networkedPlayer.Object;
            if (@object)
            {
                @object.Exiled();
            }
            __instance.initData.networkedPlayer.IsDead = true;
        }

        if (CatchedGameData.Instance.CurrentGameMode.ReEnableGameplay())
        {
            __instance.ReEnableGameplay();
            __instance.gameObject.DestroyObj();
        }

        return false;
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    [HarmonyPrefix]
    private static bool WrapUpAndSpawn_Prefix(AirshipExileController __instance)
    {
        __instance.StartCoroutine(CoCustomWrapUpAndSpawn(__instance));
        return false;
    }

    private static IEnumerator CoCustomWrapUpAndSpawn(AirshipExileController __instance)
    {
        try
        {
            RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.ExileEnd(__instance?.initData?.networkedPlayer?.Object, __instance?.initData?.networkedPlayer));
            PlayerControl.LocalPlayer.InvokeRoles(role => role.SetAllCooldowns());
            Utils.DirtyAllNames();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        if (__instance.initData.networkedPlayer != null)
        {
            PlayerControl playerData = __instance.initData.networkedPlayer.Object;
            if (playerData)
            {
                playerData.Exiled();
            }
            __instance.initData.networkedPlayer.IsDead = true;
        }

        if (CatchedGameData.Instance.CurrentGameMode.ReEnableGameplay())
        {
            yield return ShipStatus.Instance.PrespawnStep();
            __instance.ReEnableGameplay();
        }

        __instance.gameObject.DestroyObj();
    }
}
