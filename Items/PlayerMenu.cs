using HarmonyLib;
using System.Drawing;
using UnityEngine;

namespace TheBetterRoles;

[HarmonyPatch(typeof(ShapeshifterMinigame))]
public class PlayerMenu
{
    public static List<PlayerMenu> AllPlayerMenus { get; set; } = new();
    public ShapeshifterMinigame? PlayerMinigame { get; set; }
    public int Id { get; private set; }
    public CustomRoleBehavior? Role { get; set; }
    public bool ShowDeadWithBodys { get; set; } = false;
    public bool ShowDead { get; set; } = false;
    public bool ShowSelf { get; set; } = false;

    public PlayerMenu Create(int Id, CustomRoleBehavior Role, bool ShowDeadWithBodys = true, bool ShowDead = false, bool ShowSelf = false)
    {
        this.Id = Id;
        this.Role = Role;
        this.ShowDeadWithBodys = ShowDeadWithBodys;
        this.ShowDead = ShowDead;
        this.ShowSelf = ShowSelf;
        PlayerMinigame = UnityEngine.Object.Instantiate(GamePrefabHelper.GetRolePrefab<ShapeshifterRole>(AmongUs.GameOptions.RoleTypes.Shapeshifter).ShapeshifterMenu);
        PlayerMinigame.transform.SetParent(Camera.main.transform, false);
        PlayerMinigame.transform.localPosition = new Vector3(0f, 0f, -50f);

        var Phone = PlayerMinigame.transform.Find("PhoneUI/Background").GetComponent<SpriteRenderer>();
        if (Phone != null)
        {
            Phone.material?.SetColor(PlayerMaterial.BodyColor, Utils.HexToColor32(Role.RoleColor));
            Phone.material?.SetColor(PlayerMaterial.BackColor, Utils.HexToColor32(Role.RoleColor) - new UnityEngine.Color(0.25f, 0.25f, 0.25f));
        }
        var PhoneButton = PlayerMinigame.transform.Find("PhoneUI/UI_Phone_Button").GetComponent<SpriteRenderer>();
        if (PhoneButton != null)
        {
            PhoneButton.material?.SetColor(PlayerMaterial.BodyColor, Utils.HexToColor32(Role.RoleColor));
            PhoneButton.material?.SetColor(PlayerMaterial.BackColor, Utils.HexToColor32(Role.RoleColor) - new UnityEngine.Color(0.25f, 0.25f, 0.25f));
        }

        PlayerMinigame.StartCoroutine(PlayerMinigame.CoAnimateOpen());
        List<byte> dead = Main.AllDeadBodys.Select(d => d.ParentId).ToList();
        List<NetworkedPlayerInfo> players = GameData.Instance.AllPlayers.ToArray()
            .Where(d =>
                (d.IsDead == ShowDead) ||
                (ShowDeadWithBodys && d.IsDead && dead.Contains(d.PlayerId)) ||
                (!d.IsDead && ShowSelf == d.AmOwner && !d.Disconnected)
            ).ToList();

        PlayerMinigame.potentialVictims = new Il2CppSystem.Collections.Generic.List<ShapeshifterPanel>();
        Il2CppSystem.Collections.Generic.List<UiElement> list = new();
        for (int i = 0; i < players.Count; i++)
        {
            NetworkedPlayerInfo player = players[i];
            int num = i % 3;
            int num2 = i / 3;
            ShapeshifterPanel shapeshifterPanel = UnityEngine.Object.Instantiate(PlayerMinigame.PanelPrefab, PlayerMinigame.transform);
            shapeshifterPanel.transform.localPosition = new Vector3(PlayerMinigame.XStart + num * PlayerMinigame.XOffset, PlayerMinigame.YStart + num2 * PlayerMinigame.YOffset, -1f);
            shapeshifterPanel.SetPlayer(i, player, (Action)(() =>
            {
                PlayerControl.LocalPlayer.PlayerMenuSync(Id, (int)Role.RoleType, player, this, shapeshifterPanel, false);
            }));
            PlayerMinigame.potentialVictims.Add(shapeshifterPanel);
            shapeshifterPanel.Background.gameObject.GetComponent<ButtonRolloverHandler>().OverColor = Utils.HexToColor32(Role.RoleColor);
            shapeshifterPanel.Background.transform.Find("Highlight/ShapeshifterIcon").gameObject.SetActive(false);
            list.Add(shapeshifterPanel.Button);
        }
        ControllerManager.Instance.OpenOverlayMenu(PlayerMinigame.name, PlayerMinigame.BackButton, PlayerMinigame.DefaultButtonSelected, list, false);

        AllPlayerMenus.Add(this);
        return this;
    }

    [HarmonyPatch(nameof(ShapeshifterMinigame.Begin))]
    [HarmonyPrefix]
    public static bool Begin_Prefix(ShapeshifterMinigame __instance)
    {
        return false;
    }
}

[HarmonyPatch(typeof(Minigame))]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch("Close", new Type[] { })]  // No parameters
public class MinigamePatch
{
    [HarmonyPostfix]
    public static void Close_Postfix(Minigame __instance)
    {
        if (__instance is ShapeshifterMinigame shapeshifterInstance)
        {
            var menu = PlayerMenu.AllPlayerMenus.FirstOrDefault(m => m.PlayerMinigame == shapeshifterInstance);
            if (menu != null)
            {
                PlayerControl.LocalPlayer.PlayerMenuSync(menu.Id, (int)menu.Role.RoleType, null, menu, null, true);
                PlayerMenu.AllPlayerMenus.Remove(menu);
            }
        }
    }
}

