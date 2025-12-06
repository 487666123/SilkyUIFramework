namespace SilkyUIFramework;

[Service(ServiceLifetime.Transient)]
public class SilkyUIGroup
{
    private readonly List<SilkyUI> _originalSilkyUIs = [];

    private readonly List<SilkyUI> _silkyUIs = [];

    public IReadOnlyList<SilkyUI> SilkyUIs => _silkyUIs;

    public void Add(SilkyUI ui) => _originalSilkyUIs.Add(ui);

    public void Clear()
    {
        foreach (var ui in _originalSilkyUIs)
        {
            ui.SetBody(null);
        }

        _originalSilkyUIs.Clear();
    }

    public void MoveToTop(SilkyUI ui)
    {
        if (_originalSilkyUIs.Remove(ui))
        {
            _originalSilkyUIs.Insert(0, ui);
        }
    }

    private void Order()
    {
        _silkyUIs.Clear();
        _silkyUIs.AddRange(_originalSilkyUIs.OrderByDescending(value => value.Priority));

        _originalSilkyUIs.Clear();
        _originalSilkyUIs.AddRange(_silkyUIs);
    }

    public void GetHoverTarget(out SilkyUI silkyUI, out UIView element)
    {
        Order();

        foreach (var ui in SilkyUIs)
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

    public void UpdateUI(GameTime gameTime)
    {
        foreach (var ui in _silkyUIs.Where(ui => ui != null))
        {
            ui.Update(gameTime);
        }
    }

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers, int index)
    {
        Order();

        foreach (var silkyUI in _silkyUIs)
        {
            if (silkyUI.RootNode.GetType().GetCustomAttribute<RegisterUIAttribute>() is not { } registerUI) continue;

            var silkyUILayer = new SilkyUILayer(silkyUI, registerUI.Name, registerUI.InterfaceScaleType);

            layers.Insert(index + 1, silkyUILayer);
        }
    }

    public void Draw(GameTime gameTime)
    {
        Order();

        var reversedList = new List<SilkyUI>(_silkyUIs);
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