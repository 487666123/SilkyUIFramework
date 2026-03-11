namespace SilkyUIFramework;

[Service(ServiceLifetime.Transient)]
/// <summary>
/// 管理当前模组注册的全部 <see cref="SilkyUI"/>，并负责排序、更新与绘制分发。
/// </summary>
public class SilkyUIStack
{
    /// <summary>
    /// UI 栈中的原始元素。每次排序后会回写，保证后续操作基于最新层级顺序。
    /// </summary>
    private readonly List<SilkyUI> _stackItems = [];

    /// <summary>
    /// 排序后的栈元素缓存，用于当前帧遍历。
    /// </summary>
    private readonly List<SilkyUI> _orderedStackItems = [];

    /// <summary>
    /// 当前排序结果只读视图。
    /// </summary>
    public IReadOnlyList<SilkyUI> OrderedUIs => _orderedStackItems;

    /// <summary>
    /// 将一个 UI 压入栈尾。
    /// </summary>
    public void Push(SilkyUI ui) => _stackItems.Add(ui);

    /// <summary>
    /// 清空全部 UI，并先断开其 Body 引用。
    /// </summary>
    public void Clear()
    {
        foreach (var ui in _stackItems)
        {
            ui.SetBody(null);
        }

        _stackItems.Clear();
        _orderedStackItems.Clear();
    }

    /// <summary>
    /// 将指定 UI 提升到栈顶。
    /// </summary>
    public void BringToFront(SilkyUI ui)
    {
        if (_stackItems.Remove(ui))
        {
            _stackItems.Insert(0, ui);
        }
    }

    /// <summary>
    /// 按优先级重排 UI（高优先级在前），并将结果回写到原始集合。
    /// </summary>
    private void RefreshOrder()
    {
        _orderedStackItems.Clear();
        _orderedStackItems.AddRange(_stackItems.OrderByDescending(value => value.Priority));

        _stackItems.Clear();
        _stackItems.AddRange(_orderedStackItems);
    }

    /// <summary>
    /// 从前到后查找当前鼠标悬停命中的 UI 与元素。
    /// </summary>
    public void GetHoverTarget(out SilkyUI silkyUI, out UIView element)
    {
        RefreshOrder();

        foreach (var ui in OrderedUIs)
        {
            var target = ui.GetHoverElement();
            if (target != null)
            {
                silkyUI = ui;
                element = target;
                return;
            }
        }

        silkyUI = null;
        element = null;
        return;
    }

    /// <summary>
    /// 更新全部已排序 UI。
    /// </summary>
    public void Update(GameTime gameTime)
    {
        foreach (var ui in _orderedStackItems.Where(ui => ui != null))
        {
            ui.Update(gameTime);
        }
    }

    /// <summary>
    /// 将普通 UI 图层插入 Terraria 接口层列表。
    /// </summary>
    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers, int index)
    {
        RefreshOrder();

        foreach (var silkyUI in _orderedStackItems)
        {
            if (silkyUI.RootNode.GetType().GetCustomAttribute<RegisterUIAttribute>() is not { } registerUI) continue;

            var silkyUILayer = new SilkyUILayer(silkyUI, registerUI.Name, registerUI.InterfaceScaleType);

            layers.Insert(index + 1, silkyUILayer);
        }
    }

    /// <summary>
    /// 绘制标记为全局 UI 的元素，按层级从后向前回放。
    /// </summary>
    public void Draw(GameTime gameTime)
    {
        RefreshOrder();

        var reversedList = new List<SilkyUI>(_orderedStackItems);
        reversedList.Reverse();

        foreach (var ui in reversedList.Where(ui => ui.RootNode.GetType().IsDefined(typeof(RegisterGlobalUIAttribute))))
        {
            ui.TransformMatrix = Main.UIScaleMatrix;

            Main.spriteBatch.ReBegin(SpriteSortMode.Deferred,
                null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null, ui.TransformMatrix);

            ui.Draw(gameTime, Main.spriteBatch);

            Main.spriteBatch.ReBegin(SpriteSortMode.Deferred,
                null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null, ui.TransformMatrix);
        }
    }
}
