using Reactor.Networking.Rpc;
using System.Text;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network.RPCs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionAttributes
{
    internal int Amount = 0;
    internal float Cooldown = 0f;
    internal float Duration = 0f;
}

/// <summary>
/// Base Option class.
/// </summary>
internal abstract class OptionItem
{
    internal static int MaskLayer => 20;
    internal static List<OptionItem> AllTBROptions = [];
    internal static List<OptionItem> AllTBROptionsTemp = [];
    internal const string InfiniteIcon = "<b>∞</b>";
    internal virtual bool CanLoad => true;
    internal virtual bool IsOption => true;
    internal string Name => Translation != null ? Translator.GetString(Translation, showInvalid: false) : "None";
    internal int Id => _id ?? -1;
    protected int? _id { get; set; } = null;
    protected string? Translation { get; set; } = null;
    internal OptionTab? Tab { get; set; }
    internal OptionBehaviour? Option { get; set; }
    internal GameObject? Obj { get; set; }
    internal OptionItem? Parent { get; set; }
    internal bool IsParent => Children.Count > 0;
    internal List<OptionItem?> Children { get; set; } = [];
    internal virtual bool Show => ShowCondition.Invoke();
    internal virtual bool ShowChildren => Show;
    internal Func<bool>? ShowCondition = () => { return true; };
    internal bool Hide => !Show || GetParents().Any(opt => !opt.ShowChildren);
    internal static OptionItem? GetOptionById(int id) => AllTBROptions.FirstOrDefault(opt => opt._id == id);
    internal virtual void UpdateVisuals(bool updateTabVisuals = true) { }
    internal abstract string ValueAsString();
    internal virtual void SyncAUOption() { }
    internal virtual void TryLoad(bool forceLoad = false) { }
    internal virtual void SetToDefault() { }

    protected void SetupAUOption(OptionBehaviour optionBehaviour)
    {
        Option?.SetClickMask(Tab.AUTab.ButtonClickMask);

        SpriteRenderer[] componentsInChildren = optionBehaviour.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            componentsInChildren[i].material.SetInt(PlayerMaterial.MaskLayer, MaskLayer);
        }
        foreach (TextMeshPro textMeshPro in optionBehaviour.GetComponentsInChildren<TextMeshPro>(true))
        {
            textMeshPro.fontMaterial.SetFloat("_StencilComp", 3f);
            textMeshPro.fontMaterial.SetFloat("_Stencil", MaskLayer);
        }
    }

    internal void PopNotification(string custom = "")
    {
        if (_id == null) return;
        string value = custom == string.Empty ? ValueAsString() : custom;
        string msg = $"<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">{GetParentPath()} " +
        $"<color=#868686><size=85%>{Translator.GetString("Setting.SetTo")}</size></color> {value}";
        Utils.SettingsChangeNotifier(Id, msg);
        SettingsHudDisplay.Instance?.BuildPages();
    }

    internal abstract void SyncValue(object value, bool popNoti = true);

    internal string GetParentPath()
    {
        List<string> names = [Name ?? "???"];
        OptionItem tempOption = this;

        while (tempOption.Parent != null)
        {
            names.Add(tempOption.Parent.Name);
            tempOption = tempOption.Parent;
        }
        return Utils.RemoveSizeHtmlText(string.Join("<b><color=#868686>/</color></b>", names.AsEnumerable().Reverse()));
    }

    internal OptionItem? GetLastParent() => GetParents().LastOrDefault();

    internal IEnumerable<OptionItem> GetParents()
    {
        if (Parent == null) yield break;

        var target = Parent;
        while (target != null)
        {
            yield return target;
            target = target.Parent;
        }
    }

    /// <summary>
    /// Get child index of option.
    /// </summary>
    /// <returns></returns>
    internal int GetChildIndex()
    {
        int index = 0;
        var target = this;
        while (target.Parent != null)
        {
            index++;
            target = target.Parent;
        }
        return index;
    }

    /// <summary>
    /// Generates a automatic tree text display for option.
    /// </summary>
    /// <param name="size">Size of text</param>
    /// <returns>string</returns>
    internal string FormatOptionsToTextTree(float size = 50f, bool showForPercentOption = true)
    {
        StringBuilder sb = new();
        sb.Append($"<size={size}%>");

        string arrow = "▶";
        string branch = "━";
        string midBranch = "┣";
        string closeBranch = "┗";
        string vertical = "┃";

        List<TreeNode> treeNodes = [];

        void CollectTreeData(OptionItem option, int depth, bool isLastChild, TreeNode parent)
        {
            var node = new TreeNode
            {
                ParentNode = parent,
                Text = $"{Utils.RemoveSizeHtmlText(option.Name)}: {option.ValueAsString()}",
                Depth = depth,
                IsLastChild = isLastChild
            };
            treeNodes.Add(node);

            if (option.IsParent && option.ShowChildren || (option.TryCast<OptionPercentItem>() && showForPercentOption))
            {
                for (int i = 0; i < option.Children.Count; i++)
                {
                    CollectTreeData(option.Children[i], depth + option.GetChildIndex(), i == option.Children.Count - 1, node);
                }
            }
        }

        CollectTreeData(this, 0, true, null);

        bool isSingleOption = treeNodes.Count == 1;

        for (int i = 0; i < treeNodes.Count; i++)
        {
            TreeNode node = treeNodes[i];

            StringBuilder indent = new();

            if (node.Depth > 0)
            {
                bool parentHasSibling = node.ParentNode?.IsLastChild == false;
                indent.Append(parentHasSibling ? $"{vertical} " : "     ");
            }

            string prefix;
            if (isSingleOption)
            {
                prefix = branch;
            }
            else
            {
                prefix = i == 0 ? "┏" : (node.IsLastChild ? closeBranch : midBranch);
            }

            sb.AppendLine($"{indent}{prefix}{branch}{arrow} {node.Text}");
        }

        sb.Append("</size>");
        return sb.ToString();
    }

    internal static string FormatOptionsToTextTrees(OptionItem?[] optionItems, float size = 50f, bool showForPercentOption = true)
    {
        StringBuilder sb = new();
        sb.Append($"<size={size}%>");

        string arrow = "▶";
        string branch = "━";
        string midBranch = "┣";
        string closeBranch = "┗";
        string vertical = "┃";
        string rootPrefix = "┏";

        List<TreeNode> treeNodes = [];

        void CollectTreeData(OptionItem option, int depth, bool isLastChild, TreeNode parent)
        {
            var node = new TreeNode
            {
                ParentNode = parent,
                Text = $"{Utils.RemoveSizeHtmlText(option.Name)}: {option.ValueAsString()}",
                Depth = depth,
                IsLastChild = isLastChild
            };
            treeNodes.Add(node);

            if (option.IsParent && option.ShowChildren || (option.TryCast<OptionPercentItem>() && showForPercentOption))
            {
                for (int i = 0; i < option.Children.Count; i++)
                {
                    CollectTreeData(option.Children[i], depth + option.GetChildIndex(), i == option.Children.Count - 1, node);
                }
            }
        }

        for (int i = 0; i < optionItems.Length; i++)
        {
            if (optionItems[i] == null) continue;
            CollectTreeData(optionItems[i], 0, i == optionItems.Length - 1, null);
        }

        bool isSingleOption = optionItems.Length == 1 && treeNodes.Count == 1;

        for (int i = 0; i < treeNodes.Count; i++)
        {
            TreeNode node = treeNodes[i];

            StringBuilder indent = new();

            if (node.Depth > 0)
            {
                bool parentHasSibling = node.ParentNode?.IsLastChild == false;
                indent.Append(parentHasSibling ? $"{vertical} " : "  ");
            }

            string prefix;
            if (isSingleOption)
            {
                prefix = branch;
            }
            else if (node.Depth == 0)
            {
                prefix = i == 0 ? rootPrefix : (node.IsLastChild ? closeBranch : midBranch);
            }
            else
            {
                prefix = node.IsLastChild ? closeBranch : midBranch;
            }

            sb.AppendLine($"{indent}{prefix}{branch}{arrow} {node.Text}");
        }

        sb.Append("</size>");
        return sb.ToString();
    }

    internal void CreateDescriptionButton(string text)
    {
        if (Option == null) return;

        NumberOption optionBehaviourNum = UnityEngine.Object.Instantiate(Tab.AUTab.numberOptionOrigin, Vector3.zero, Quaternion.identity, Tab.AUTab.settingsContainer);
        SetupAUOption(optionBehaviourNum);
        var button = UnityEngine.Object.Instantiate(optionBehaviourNum.PlusBtn, Option.transform);
        optionBehaviourNum.DestroyObj();
        button.transform.position = button.transform.position - new Vector3(4.75f, 0f, 0f);
        button.transform.GetComponentInChildren<TextMeshPro>(true).gameObject.DestroyObj();
        button.ReceiveMouseOut();
        button.interactableHoveredColor = Color.gray;
        button.interactableClickColor = Color.white;
        button.buttonSprite.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Icons.QuestionMark.png", 50);
        button.OnClick = new();
        button.OnClick.AddListener((Action)(() =>
        {
            var menu = GameSettingMenu.Instance;
            if (menu != null)
            {
                menu.MenuDescriptionText.text = text;
            }
        }));
    }

    /// <summary>
    /// Get bool for CheckboxOption, could throw exception if option is not a CheckboxOption.
    /// </summary>
    /// <returns>bool</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal virtual bool GetBool()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get float for FloatOption, could throw exception if option is not a FloatOption.
    /// </summary>
    /// <returns>float</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal virtual float GetFloat()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get float for IntOption, could throw exception if option is not a IntOption.
    /// </summary>
    /// <returns>int</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal virtual int GetInt()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get int for StringOption and PlayerOption, could throw exception if option is not a StringOption.
    /// </summary>
    /// <returns>int</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal virtual int GetStringValue()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks if the value is a boolean.
    /// </summary>
    /// <param name="@bool">The boolean value to check.</param>
    /// <returns>True if the value matches, otherwise false.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not implemented.</exception>
    internal virtual bool Is(bool @bool)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks if the value is a float.
    /// </summary>
    /// <param name="@float">The float value to check.</param>
    /// <returns>True if the value matches, otherwise false.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not implemented.</exception>
    internal virtual bool Is(float @float)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks if the value is an integer.
    /// </summary>
    /// <param name="@int">The integer value to check.</param>
    /// <returns>True if the value matches, otherwise false.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not implemented.</exception>
    internal virtual bool Is(int @int)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks if the value is a string.
    /// </summary>
    /// <param name="@string">The string value to check.</param>
    /// <returns>True if the value matches, otherwise false.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not implemented.</exception>
    internal virtual bool Is(string @string)
    {
        throw new NotImplementedException();
    }

    class TreeNode
    {
        internal TreeNode? ParentNode { get; set; }
        internal string Text { get; set; }
        internal int Depth { get; set; }
        internal bool IsLastChild { get; set; }
    }
}

internal abstract class OptionItem<T> : OptionItem
{
    protected TextMeshPro? TitleTMP { get; set; }
    protected TextMeshPro? ValueTMP { get; set; }
    private bool HasLoadValue { get; set; }
    protected T? MainValue
    {
        get => GameState.IsHost || !GameState.IsInGame ? Value : SyncedValue;
        set
        {
            if (GameState.IsHost || !GameState.IsInGame)
                Value = value;
            else
                SyncedValue = value;
        }
    }
    protected T? Value { get; set; } = default;
    protected T? SyncedValue { get; set; } = default;
    protected T? DefaultValue { get; set; } = default;
    internal virtual T? GetValue() => MainValue;
    internal virtual T? GetDefaultValue() => DefaultValue;
    internal override string ValueAsString() => Value?.ToString() ?? string.Empty;
    internal override void SetToDefault()
    {
        MainValue = DefaultValue;
    }
    protected abstract void CreateBehavior();
    internal virtual void OnValueChange(T oldValue, T newValue) { }
    internal Action<OptionItem>? OnValueChangeAction = (opt) => { };

    protected void SetupText(TextMeshPro textPro)
    {
        textPro.transform.SetLocalX(-2.5f);
        textPro.transform.SetLocalY(-0.05f);
        textPro.alignment = TextAlignmentOptions.Right;
        textPro.enableWordWrapping = false;
        textPro.enableAutoSizing = true;
        textPro.fontSize = 3.3f;
        textPro.fontSizeMax = 3.3f;
        textPro.fontSizeMin = 1f;
        textPro.rectTransform.sizeDelta = new(4.5f, 1f);
        textPro.outlineColor = Color.black;
        textPro.outlineWidth = 0.25f;
    }

    protected virtual void SetupOptionBehavior() { }

    protected void SetOptionVisuals()
    {
        if (Option != null)
        {
            float colorNum = 1f - (0.25f * GetChildIndex());
            Option.LabelBackground.color = new Color(colorNum, colorNum, colorNum, 1f);
            Option.LabelBackground.transform.SetLocalZ(1f);
            Option.LabelBackground.transform.localScale = new Vector3(1.6f, 0.8f, 1f);
            Option.LabelBackground.transform.SetLocalX(-2.4f);
            float resize = 0f + (0.1f * GetChildIndex());
            if (resize > 0f)
            {
                if (TitleTMP != null)
                {
                    TitleTMP.rectTransform.sizeDelta -= new Vector2(resize * 2.5f, 0f);
                    TitleTMP.transform.SetLocalX(TitleTMP.transform.localPosition.x + resize * 2.5f);
                }
                var pos = Option.LabelBackground.transform.localPosition;
                var size = Option.LabelBackground.transform.localScale;
                Option.LabelBackground.transform.localPosition = new Vector3(pos.x + (resize * 1.8f), pos.y, pos.z);
                Option.LabelBackground.transform.localScale = new Vector3(size.x - resize, size.y, size.z);
            }
        }

        UpdateVisuals();
    }

    internal virtual void SetValue(T newValue)
    {
        T oldValue = Value;
        Value = newValue;
        UpdateVisuals();
        PopNotification();
        SyncAUOption();
        Save();
        OnValueChange(oldValue, newValue);
        OnValueChangeAction.Invoke(this);
        Rpc<RpcSyncOption>.Instance.Send(new(Id, newValue));
    }

    internal override void TryLoad(bool forceLoad = false)
    {
        if (!CanLoad) return;

        if (!HasLoadValue || forceLoad)
        {
            HasLoadValue = true;
            Load();
        }
    }

    private void Load()
    {
        if (!CanLoad) return;

        if (_id == null) return;
        Value = TBRDataManager.LoadSetting(Id, DefaultValue);
        SyncAUOption();
    }

    internal void Save()
    {
        if (!CanLoad) return;

        if (_id == null) return;
        TBRDataManager.SaveSetting(Id, Value);
    }
}