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
    public static int IdNum = 0;
    public float StaticSpacingNum => 1f;
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
            if (item.Tab.Id != GameSettingMenuPatch.ActiveTab) continue;

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
                BetterOptionHeaderItem => 0.1f,
                BetterOptionDividerItem => 0.15f,
                _ => 0f,
            };

            item.obj.transform.SetLocalY(2f - 1f * SpacingNum);
            _ = new LateTask(() =>
            {
                if (item?.TitleText?.text != null && item.Name != null)
                {
                    item.TitleText.text = item.Name;
                }
            }, 0.005f, shoudLog: false);

            SpacingNum += item switch
            {
                BetterOptionHeaderItem => 0.75f,
                BetterOptionTitleItem => 0.50f,
                _ => 0.45f,
            };


            _ = new LateTask(() =>
            {
                item.Tab.Tab.scrollBar.SetYBoundsMax(1.65f * SpacingNum / 1.8f);
                item.Tab.Tab.scrollBar.ScrollRelative(new(0f, 0f));
            }, 0.005f, shoudLog: false);
        }
    }

    public void SetUp(OptionBehaviour optionBehaviour)
    {
        SetData(optionBehaviour);
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

    public virtual void SyncValue() { }

    public virtual string FormatValueAsText() => string.Empty;
}
