namespace TheBetterRoles.Items.OptionItems;

/// <summary>
/// Percent option.
/// Type: Float
/// </summary>
internal class OptionPercentItem : OptionFloatItem
{

    /// <summary>
    /// Creates a new percent item for the options menu. If an item with the specified ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <param name="id">The unique identifier for the percent item.</param>
    /// <param name="tab">The tab to which the percent item belongs.</param>
    /// <param name="tranStr">The translation string for the percent item label.</param>
    /// <param name="defaultValue">The default value for the percent item (in the range of 0 to 100).</param>
    /// <param name="parent">An optional parent option item that this percent item belongs to.</param>
    /// <returns>The created or reused <see cref="OptionPercentItem"/> instance.</returns>
    internal static OptionPercentItem Create(int id, OptionTab tab, string tranStr, float defaultValue, OptionItem parent = null)
    {
        if (GetOptionById(id) is OptionPercentItem floatItem)
        {
            floatItem.CreateBehavior();
            return floatItem;
        }

        OptionPercentItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.Increment = 5;
        Item.Range = new FloatRange(0f, 100f);
        Item.DefaultValue = defaultValue;
        Item.Fixs = ("", "");

        if (parent != null)
        {
            Item.Parent = parent;
            parent.Children.Add(Item);
        }

        Item.CreateBehavior();
        return Item;
    }

    internal override string ValueAsString() => $"<color={GetColor(MainValue)}>{MainValue}%</color>";

    internal string GetColor(float num)
    {
        switch (num)
        {
            case float n when n <= 0f:
                return "#ff0600";
            case float n when n <= 25f:
                return "#ff9d00";
            case float n when n <= 50f:
                return "#fff900";
            case float n when n <= 75f:
                return "#80ff00";
            default:
                return "#80ff00";
        }
    }
}
