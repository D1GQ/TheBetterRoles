using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class SabotageAbilityButton : BaseButton
{
    public SabotageAbilityButton Create(int id, string name, CustomRoleBehavior role, bool Right = true, int index = -1)
    {
        Role = role;
        Id = id;
        Name = name;
        Visible = true;

        if (!_player.IsLocalPlayer()) return this;

        var buttonObj = UnityEngine.Object.Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        if (buttonObj != null)
        {
            Button = buttonObj.GetComponent<PassiveButton>();
            ActionButton = buttonObj.GetComponent<ActionButton>();
            Text = ActionButton.buttonLabelText;
            buttonObj.SetActive(true);

            ActionButton.graphic.sprite = HudManager._instance.SabotageButton.graphic.sprite;
            ActionButton.graphic.SetCooldownNormalizedUvs();

            Button.OnClick.RemoveAllListeners();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (ActionButton.canInteract)
                {
                    if (Role.CanSabotage)
                    {
                        DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
                        {
                            Mode = MapOptions.Modes.Sabotage
                        });
                    }
                }
            }));
        }

        ActionButton.transform.Find("CommsDown").GetComponent<SpriteRenderer>().sprite = new();
        ActionButton.OverrideText(name);
        ActionButton.buttonLabelText.fontSizeMin = 4f;
        ActionButton.buttonLabelText.enableWordWrapping = false;
        ActionButton.buttonLabelText.SetOutlineColor(Utils.GetCustomRoleColor(Role.RoleType));

        ActionButton.SetInfiniteUses();

        allButtons.Add(this);
        return this;
    }

    public override void FixedUpdate()
    {
        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        if (BaseInteractable())
        {
            ActionButton.SetEnabled();
        }
        else
        {
            ActionButton.SetDisabled();
        }

        ActionButton.gameObject.SetActive(Visible);
    }
}
