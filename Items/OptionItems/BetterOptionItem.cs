using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public enum SettingType
{
    Byte,
    TempByte,
    Int,
    Float,
    Bool,
}

public class BetterOptionItem
{
    public static string InfiniteIcon => "<b><size=150%>∞</size></b>";
    protected float topDistance = 0.15f;
    protected float bottomDistance = 0.50f;
    public static int IdNum = 0;
    public float StaticSpacingNum => 1f;
    public Action<BetterOptionItem> OnValueChange { get; set; } = (BetterOptionItem opt) => { };
    public float StaticSpacingNumPlus => 0.45f;
    public static Dictionary<int, int> TempPlayerOptionData = [];
    public static int TempPlayerOptionDataNum = 0;
    public static List<BetterOptionItem> BetterOptionItems = [];

    public int maskLayer = 20;
    public int Id { get; protected set; } = 0;
    public BetterOptionTab? Tab { get; protected set; }
    public OptionBehaviour? Option { get; protected set; }
    public string? Name { get; protected set; } = "None";
    public TextMeshPro? TitleText;
    public GameObject? obj;

    public BetterOptionItem? ThisParent;
    public bool IsChild = false;
    public bool IsParent => ChildrenList.Count > 0;
    public List<BetterOptionItem> ChildrenList = [];
    public virtual bool ShowChildrenCondition() => false;
    public virtual bool SelfShowCondition() => true;

    public static void UpdatePositions()
    {
        float SpacingNum = 0;

        foreach (var item in BetterOptionItems)
        {
            if (item == null | item.Tab.Id != GameSettingMenuPatch.ActiveTab) continue;

            item.obj.transform.SetLocalY(2f);

            if (item.ThisParent != null)
            {
                item.obj.SetActive(item.ThisParent.ShowChildrenCondition() && item.SelfShowCondition() && item.ThisParent.Option.gameObject.active);
                if (!(item.ThisParent.ShowChildrenCondition() && item.SelfShowCondition() && item.ThisParent.Option.gameObject.active))
                    continue;
            }

            if (item is BetterOptionPlayerItem player)
            {
                player.Load();
            }

            SpacingNum += item switch
            {
                BetterOptionHeaderItem => item.topDistance,
                BetterOptionDividerItem => item.topDistance,
                _ => 0f,
            };

            item.obj.transform.SetLocalY(2f - 1f * SpacingNum);
            _ = new LateTask(() =>
            {
                if (item?.TitleText?.text != null && item.Name != null)
                {
                    item.TitleText.DestroyTextTranslator();
                    item.TitleText.text = item.Name;
                }
            }, 0.025f, shouldLog: false);

            SpacingNum += item switch
            {
                BetterOptionHeaderItem => item.bottomDistance,
                BetterOptionTitleItem => item.bottomDistance,
                _ => 0.45f,
            };
        }

        _ = new LateTask(() =>
        {
            BetterOptionTab.allTabs?.FirstOrDefault(tab => tab.Id == GameSettingMenuPatch.ActiveTab)?.Tab?.scrollBar?.SetYBoundsMax(1.65f * SpacingNum / 1.8f);
            BetterOptionTab.allTabs?.FirstOrDefault(tab => tab.Id == GameSettingMenuPatch.ActiveTab)?.Tab?.scrollBar?.ScrollRelative(new(0f, 0f));
        }, 0.005f, shouldLog: false);
    }

    public void SetUp(OptionBehaviour optionBehaviour)
    {
        SpriteRenderer[] componentsInChildren = optionBehaviour.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            componentsInChildren[i].material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }
        foreach (TextMeshPro textMeshPro in optionBehaviour.GetComponentsInChildren<TextMeshPro>(true))
        {
            textMeshPro.fontMaterial.SetFloat("_StencilComp", 3f);
            textMeshPro.fontMaterial.SetFloat("_Stencil", maskLayer);
        }
    }

    public int GetGeneratedId()
    {
        var num = 100 * IdNum;
        IdNum++;
        return num;
    }

    class TreeNode
    {
        public TreeNode? ParentNode { get; set; }
        public string Text { get; set; }
        public int Depth { get; set; }
        public bool IsLastChild { get; set; }
    }

    public string FormatOptionsToText(float size = 50f)
    {
        StringBuilder sb = new();
        sb.Append($"<size={size}%>");

        string arrow = "▶";
        string branch = "━";
        string midBranch = "┣";
        string closeBranch = "┗";
        string vertical = "┃";

        List<TreeNode> treeNodes = [];

        void CollectTreeData(BetterOptionItem option, int depth, bool isLastChild, TreeNode parent)
        {
            var node = new TreeNode
            {
                ParentNode = parent,
                Text = $"{Utils.RemoveSizeHtmlText(option.Name)}: {option.FormatValueAsText()}",
                Depth = depth,
                IsLastChild = isLastChild
            };
            treeNodes.Add(node);

            if (option.IsParent && option.ShowChildrenCondition() || option.TryCast<BetterOptionPercentItem>())
            {
                for (int i = 0; i < option.ChildrenList.Count; i++)
                {
                    CollectTreeData(option.ChildrenList[i], depth + option.GetChildIndex(), i == option.ChildrenList.Count - 1, node);
                }
            }
        }

        CollectTreeData(this, 0, true, null);

        for (int i = 0; i < treeNodes.Count; i++)
        {
            TreeNode node = treeNodes[i];

            StringBuilder indent = new();

            if (node.Depth > 0)
            {
                bool parentHasSibling = node.ParentNode?.IsLastChild == false;
                indent.Append(parentHasSibling ? $"{vertical} " : "     ");
            }

            string prefix = i == 0 ? "┏" : (node.IsLastChild ? closeBranch : midBranch);
            sb.AppendLine($"{indent}{prefix}{branch}{arrow} {node.Text}");
        }

        sb.Append("</size>");
        return sb.ToString();
    }

    public int GetChildIndex()
    {
        int index = 0;
        var target = this;
        while (target.ThisParent != null && target.IsParent)
        {
            index++;
            target = target.ThisParent;
        }
        return index;
    }

    public virtual bool GetBool()
    {
        throw new NotImplementedException();
    }

    public virtual float GetFloat()
    {
        throw new NotImplementedException();
    }

    public virtual int GetInt()
    {
        throw new NotImplementedException();
    }

    public virtual int GetValue()
    {
        throw new NotImplementedException();
    }

    public virtual void SetData(OptionBehaviour optionBehaviour)
    {
        throw new NotImplementedException();
    }

    public virtual void ValueChanged(int id, OptionBehaviour optionBehaviour)
    {
        throw new NotImplementedException();
    }

    public virtual void SyncValue(string value) { }

    public virtual string FormatValueAsText() => string.Empty;
}
