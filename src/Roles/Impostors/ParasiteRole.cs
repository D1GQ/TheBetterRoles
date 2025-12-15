using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class ParasiteRole : ImpostorRoleTBR, IRoleUpdateAction, IRoleAbilityAction<PlayerControl>, IRoleMurderAction, IRoleDeathAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 44;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Parasite;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Experimental;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;
    internal sealed override bool IsKillingRole => true;
    internal sealed override bool CanKill => false;

    internal OptionItem? InfectCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InfectCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Parasite.Option.InfectCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private NetworkedPlayerInfo? InfectedData;
    private PlayerControl? InfectedBase;
    private bool HasInfected;
    internal PlayerAbilityButton? InfectButton;
    internal BaseAbilityButton? KillTargetButton;
    internal sealed override void OnSetUpRole()
    {
        PreloadPlayer();

        if (_player.IsLocalPlayer())
        {
            InfectButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Parasite.Ability.1"), InfectCooldown.GetFloat(), 0, 0, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
            InfectButton.VisibleCondition = () => { return InfectButton.Role is ParasiteRole parasite && parasite.HasInfected == false; };
            KillTargetButton = RoleButtons.AddButton(BaseAbilityButton.Create(6, Translator.GetString(StringNames.KillLabel), 0, 0, 0, RoleButtons.KillButton.ActionButton.graphic.sprite, this, true));
            KillTargetButton.VisibleCondition = () => { return InfectButton.Role is ParasiteRole parasite && parasite.HasInfected == true; };

            PreloadCamera();
            HideCamera();
        }
    }

    private void PreloadPlayer()
    {
        Vector2 zero = Vector2.zero;
        if (TutorialManager.InstanceExists)
        {
            zero = new Vector2(-1.9f, 3.25f);
        }
        InfectedBase = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, zero, Quaternion.identity);
        InfectedBase.gameObject.SetActive(false);
        InfectedBase.notRealPlayer = true;
        PlayerControl.AllPlayerControls.Remove(InfectedBase);
        InfectedBase.GetComponent<ExtendedPlayerControl>().IsFake = true;
        InfectedBase.CachedPlayerData = _data;
        InfectedBase.UpdateName();
        InfectedBase.OwnerId = -1;
        InfectedBase.PlayerId = (byte)(100 + _player.PlayerId);
        InfectedBase.RawSetOutfit(_player.Data.DefaultOutfit, PlayerOutfitType.Default);
        InfectedBase.SpawnLocally();
        InfectedBase.NetTransform.SpawnLocally(_data.ClientId);
        InfectedBase.NetTransform.SnapTo(new(1000f, 1000f));
        InfectedBase.name = $"ParasitePlayerUnloaded({_player.Data.PlayerName})";
    }

    private Vector2 del = new();
    void IRoleUpdateAction.Update()
    {
        if (InfectedBase != null && HasInfected)
        {
            if (_player.IsLocalPlayer() && InfectedBase.CanMove)
            {
                del.x = (del.y = 0f);
                float move = 1f;
                if (del == Vector2.zero)
                {
                    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.I)) del.y += move;
                    if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.L)) del.x += move;
                    if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.K)) del.y += -move;
                    if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.J)) del.x += -move;
                }
                del.Normalize();
            }

            if (Input.GetKey(KeyCode.Space)) KillTargetButton?.Button?.OnClick?.Invoke();
        }
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (InfectedBase != null && HasInfected && _player.IsLocalPlayer())
        {
            InfectedBase.MyPhysics.SetNormalizedVelocity(del);
        }
    }

    void IRoleUpdateAction.LateUpdate()
    {
        if (InfectedBase != null && HasInfected && _player.IsLocalPlayer())
        {
            Vector3 position = InfectedBase.transform.position;
            position.z = position.y / 1000f;
            InfectedBase.transform.position = position;
            InfectedBase.MyPhysics.HandleAnimation(false);
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    InfectedPlayer(target);
                    Networked.SendRoleSync(0, target);
                }
                break;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 6:
                {
                    KillInfectedPlayer();
                    Networked.SendRoleSync(1);
                }
                break;
        }
    }

    internal sealed override void OnDeinitialize()
    {
        KillInfectedPlayer(_player);
        DespawnInfectedPlayer();
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        KillInfectedPlayer(_player);
    }

    void IRoleGameplayAction.GameEnd()
    {
        DespawnInfectedPlayer();
    }

    bool IRoleMurderAction.CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == InfectedBase)
        {
            if (RoleListener.CheckAllRoles<IRoleMurderAction>(role => role.CheckMurderOther(_player, target, _player == target, true)))
            {
                KillInfectedPlayer(killer);
            }

            return false;
        }

        return true;
    }

    void IRoleDeathAction.OnDeath(PlayerControl player, DeathReasons reason)
    {
        if (HasInfected)
        {
            KillInfectedPlayer();
        }
    }

    private void InfectedPlayer(PlayerControl target)
    {
        if (HasInfected) return;
        HasInfected = true;

        InfectedBase.notRealPlayer = false;
        PlayerControl.AllPlayerControls.Add(InfectedBase);
        InfectedBase.gameObject.SetActive(true);
        InfectedData = target.Data;
        InfectedBase.UpdateName();
        InfectedBase.PlayerId = (byte)(100 + target.PlayerId);
        InfectedBase.MyPhysics.FlipX = target.MyPhysics.FlipX;
        InfectedBase.RawSetOutfit(_player.Data.DefaultOutfit, PlayerOutfitType.Default);
        InfectedBase.NetTransform.SnapTo(target.GetCustomPosition());
        InfectedBase.cosmetics.gameObject.SetActive(false);
        InfectedBase.cosmetics.gameObject.SetActive(true);
        target.CustomExiled();
        target.SetDeathReason(DeathReasons.Destroyed, RoleColorHex);

        if (_player.IsLocalPlayer())
        {
            TipTracker.Instance.SetTip("Kill: <#CBCBCB>[Space]</color>\nMovement: <#CBCBCB>[◀], [▲], [▼], [▶]</color> / <#CBCBCB>[J], [I], [K], [L]</color>".ToColor(RoleColorHex), true, 0.2f);
            HudManagerPatch.ButtonsLeft.SetActive(false);
            ShowCamera();
        }
    }

    private void KillInfectedPlayer(PlayerControl? killer = null)
    {
        if (!HasInfected) return;
        HasInfected = false;

        Utils.SpawnBody(InfectedBase.transform.position + _player.KillAnimations.First().BodyOffset, InfectedData.PlayerId, InfectedBase.PlayerId, InfectedData.PlayerId);
        if (_player.IsLocalPlayer() || (killer != null && killer.IsLocalPlayer()))
        {
            SoundManager.Instance.PlaySound(_player.KillSfx, false, 0.8f, null);
        }
        if (_player.IsLocalPlayer())
        {
            TipTracker.Instance.ClearTip();
            HudManagerPatch.ButtonsLeft.SetActive(true);
            HideCamera();
        }
        killer?.NetTransform?.SnapTo(InfectedBase.GetCustomPosition());
        InfectButton?.SetCooldown();
        InfectedBase.PlayerId = (byte)(100 + _player.PlayerId);
        InfectedBase.notRealPlayer = true;
        PlayerControl.AllPlayerControls.Remove(InfectedBase);
        InfectedBase.NetTransform.SnapTo(new(1000f, 1000f));
        InfectedBase.gameObject.SetActive(false);
        InfectedBase.name = $"ParasitePlayerUnloaded({_player.Data.PlayerName})";
        InfectedData = null;
    }

    private void DespawnInfectedPlayer()
    {
        if (InfectedBase == null) return;
        HasInfected = false;
        InfectedBase.DespawnLocally();
        InfectedBase.NetTransform.DespawnLocally();
        InfectedBase.DestroyObj();
        camObject?.DestroyObj();
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                InfectedPlayer(data.MessageReader.ReadFast<PlayerControl>());
                break;
            case 1:
                KillInfectedPlayer();
                break;
        }
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (target == InfectedBase && (_player.IsLocalPlayer() || !localPlayer.IsAlive()))
        {
            return $"<#FF4600>☀</color>";
        }

        return string.Empty;
    }

    private Camera? ParasiteCam;
    private GameObject camObject;

    private void ShowCamera()
    {
        if (camObject != null)
        {
            camObject.transform.localPosition = new(0f, 0f, -200f);
        }
    }

    private void HideCamera()
    {
        if (camObject != null)
        {
            camObject.transform.localPosition = new(0f, -100f, -200f);
        }
    }

    private void PreloadCamera()
    {
        camObject = new("Parasite Camera");
        camObject.transform.SetParent(Camera.main.transform, true);
        camObject.transform.localPosition = new(0f, 0f, -200f);

        GameObject ParasiteCamObject = new("Camera");
        ParasiteCam = ParasiteCamObject.AddComponent<Camera>();
        ParasiteCamObject.transform.SetParent(InfectedBase.transform, true);

        ParasiteCam.CopyFrom(Camera.main);
        string[] masks = ["Default", "Ship", "Objects", "ShortObjects", "Players", "Ghost", "IlluminatedBlocking", "Dead"];
        ParasiteCam.cullingMask = LayerMask.GetMask(masks);
        ParasiteCam.rect = new Rect(0f, 0f, 1f, 1f);
        ParasiteCam.orthographicSize = 1;
        ParasiteCam.aspect = 1f;

        RenderTexture renderTexture = new(1920, 1080, 16);
        ParasiteCam.targetTexture = renderTexture;

        GameObject viewportObject = new("Viewport");
        viewportObject.transform.SetParent(camObject.transform);

        GameObject spriteObject = new("Sprite");
        spriteObject.transform.SetParent(viewportObject.transform);
        spriteObject.transform.localPosition = new(0f, 0f, -10f);

        SpriteRenderer cameraFrame = spriteObject.AddComponent<SpriteRenderer>();
        cameraFrame.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.CameraFrame.png", 60);
        cameraFrame.gameObject.layer = LayerMask.NameToLayer("UI");

        GameObject spriteOverlayObject = new("Sprite Overlay");
        spriteOverlayObject.transform.SetParent(viewportObject.transform);
        spriteOverlayObject.transform.localPosition = new(0f, 0f, -5f);

        SpriteRenderer cameraOverlayFrame = spriteOverlayObject.AddComponent<SpriteRenderer>();
        cameraOverlayFrame.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.CameraFrameOverlay.png", 60);
        cameraOverlayFrame.gameObject.layer = LayerMask.NameToLayer("UI");

        GameObject meshObject = new("Viewport Mesh");
        meshObject.transform.SetParent(viewportObject.transform);
        meshObject.gameObject.layer = LayerMask.NameToLayer("UI");

        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateQuadMesh();

        Material material = new(Shader.Find("Unlit/Texture"))
        {
            mainTexture = renderTexture
        };

        meshRenderer.material = material;

        meshObject.transform.localScale = new(5.5f, 4f, 1f);

        var aspectPosition = viewportObject.AddComponent<AspectPosition>();
        viewportObject.transform.localScale = new(0.5f, 0.5f, 1f);
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        aspectPosition.DistanceFromEdge = new(1.25f, 1.25f, 0f);
        aspectPosition.AdjustPosition();

        ParasiteCamObject.transform.localPosition = new(0f, 0.1f, 0f);
    }

    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new();

        float xSize = 0.42f;
        float ySize = 0.6f;

        Vector3[] vertices = {
        new(-xSize, -ySize, 0f),
        new( xSize, -ySize, 0f),
        new(-xSize,  ySize, 0f),
        new( xSize,  ySize, 0f)
        };

        Vector2[] uv =
        [
            new(0f, 0f),
            new(1f, 0f),
            new(0f, 1f),
            new(1f, 1f)
        ];

        int[] triangles =
        [
            0,
            2,
            1,
            2,
            3,
            1
        ];

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
