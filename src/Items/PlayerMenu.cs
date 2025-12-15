using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Items
{
    /// <summary>
    /// Manages the player menu for the Shapeshifter Minigame.
    /// </summary>
    [HarmonyPatch(typeof(ShapeshifterMinigame))]
    internal class PlayerMenu
    {
        internal static List<PlayerMenu> AllPlayerMenus { get; set; } = new List<PlayerMenu>();

        internal ShapeshifterMinigame? PlayerMinigame { get; set; }
        internal int Id { get; private set; }
        internal RoleClass? Role { get; set; }
        internal bool ShowDeadWithBodys { get; set; } = false;
        internal bool ShowDead { get; set; } = false;
        internal bool ShowSelf { get; set; } = false;

        /// <summary>
        /// Creates a new player menu for the shapeshifter minigame.
        /// </summary>
        /// <param name="Id">The ID for the player.</param>
        /// <param name="Role">The role associated with the player.</param>
        /// <param name="ShowDeadWithBodys">Whether to show dead bodies (default true).</param>
        /// <param name="ShowDead">Whether to show dead players (default false).</param>
        /// <param name="ShowSelf">Whether to show the player's own menu (default false).</param>
        /// <returns>The newly created PlayerMenu.</returns>
        internal PlayerMenu Create(int Id, RoleClass Role, bool ShowDeadWithBodys = true, bool ShowDead = false, bool ShowSelf = false)
        {
            this.Id = Id;
            this.Role = Role;
            this.ShowDeadWithBodys = ShowDeadWithBodys;
            this.ShowDead = ShowDead;
            this.ShowSelf = ShowSelf;

            // Instantiate the shapeshifter minigame UI
            var rolePrefab = Prefab.GetCachedPrefab<ShapeshifterRole>();
            PlayerMinigame = UnityEngine.Object.Instantiate(rolePrefab.ShapeshifterMenu);
            PlayerMinigame.transform.SetParent(Camera.main.transform, false);
            PlayerMinigame.transform.localPosition = new Vector3(0f, 0f, -50f);

            // Set color for the phone UI components
            var Phone = PlayerMinigame.transform.Find("PhoneUI/Background").GetComponent<SpriteRenderer>();
            if (Phone != null)
            {
                Phone.material?.SetColor(PlayerMaterial.BodyColor, Role.RoleColor);
                Phone.material?.SetColor(PlayerMaterial.BackColor, Role.RoleColor - new Color(0.25f, 0.25f, 0.25f));
            }

            var PhoneButton = PlayerMinigame.transform.Find("PhoneUI/UI_Phone_Button").GetComponent<SpriteRenderer>();
            if (PhoneButton != null)
            {
                PhoneButton.material?.SetColor(PlayerMaterial.BodyColor, Role.RoleColor);
                PhoneButton.material?.SetColor(PlayerMaterial.BackColor, Role.RoleColor - new Color(0.25f, 0.25f, 0.25f));
            }

            // Animate opening of the menu
            PlayerMinigame.StartCoroutine(PlayerMinigame.CoAnimateOpen());

            // Get all players eligible to be shown in the menu
            List<NetworkedPlayerInfo> players = GameData.Instance.AllPlayers.ToArray()
                .Where(d =>
                    (d.IsAlive() || (this.ShowDead || this.ShowDeadWithBodys && d.DeadBody() != null))
                    && (!d.IsLocalData() || this.ShowSelf) && !d.Disconnected
                ).ToList();

            PlayerMinigame.potentialVictims = new Il2CppSystem.Collections.Generic.List<ShapeshifterPanel>();
            Il2CppSystem.Collections.Generic.List<UiElement> list = new();

            // Add player info to the menu
            for (int i = 0; i < players.Count; i++)
            {
                NetworkedPlayerInfo player = players[i];
                int num = i % 3;
                int num2 = i / 3;

                ShapeshifterPanel shapeshifterPanel = UnityEngine.Object.Instantiate(PlayerMinigame.PanelPrefab, PlayerMinigame.transform);
                shapeshifterPanel.transform.localPosition = new Vector3(PlayerMinigame.XStart + num * PlayerMinigame.XOffset, PlayerMinigame.YStart + num2 * PlayerMinigame.YOffset, -1f);
                shapeshifterPanel.SetPlayer(i, player, (Action)(() =>
                {
                    PlayerControl.LocalPlayer.InvokeRoles<IRoleMenuAction>(role => role.PlayerMenu(Id, player?.Object, player, this, shapeshifterPanel, false), role => role.RoleHash == Role.RoleHash);
                }));

                shapeshifterPanel.NameText.color = player.IsLocalData() ? player.Role().RoleColor : Color.white;
                PlayerMinigame.potentialVictims.Add(shapeshifterPanel);
                shapeshifterPanel.Background.gameObject.GetComponent<ButtonRolloverHandler>().OverColor = Role.RoleColor;
                shapeshifterPanel.Background.transform.Find("Highlight/ShapeshifterIcon").gameObject.SetActive(false);
                list.Add(shapeshifterPanel.Button);
            }

            // Open the menu
            ControllerManager.Instance.OpenOverlayMenu(PlayerMinigame.name, PlayerMinigame.BackButton, PlayerMinigame.DefaultButtonSelected, list, false);

            AllPlayerMenus.Add(this);
            return this;
        }

        [HarmonyPatch(nameof(ShapeshifterMinigame.Begin))]
        [HarmonyPrefix]
        internal static bool Begin_Prefix(ShapeshifterMinigame __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(Minigame))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch("Close", new Type[] { })]
    internal class MinigamePatch
    {
        [HarmonyPostfix]
        internal static void Close_Postfix(Minigame __instance)
        {
            if (__instance is ShapeshifterMinigame shapeshifterInstance)
            {
                var menu = PlayerMenu.AllPlayerMenus.FirstOrDefault(m => m.PlayerMinigame == shapeshifterInstance);
                if (menu != null)
                {
                    PlayerControl.LocalPlayer.InvokeRoles<IRoleMenuAction>(role => role.PlayerMenu(menu.Id, null, null, menu, null, true), role => role.RoleHash == menu.Role.RoleHash);

                    PlayerMenu.AllPlayerMenus.Remove(menu);
                }
            }
        }
    }
}