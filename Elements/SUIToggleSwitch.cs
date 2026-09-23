using SilkyUIFramework.StyleSystem;

namespace SilkyUIFramework.Elements;

public class SUIToggleSwitchThumb : UIView
{
    public bool AutoBorderRadius { get; set; } = true;

    public SUIToggleSwitchThumb()
    {
        Positioning = Positioning.Absolute;
        IgnoreMouseInteraction = true;
        SetSize(0f, 0f, 1f, 1f);
        BackgroundColor = SUIColor.Background * 0.75f;
    }

    public override void Measure(float width, float height)
    {
        var size = Math.Min(width, height);
        base.Measure(size, size);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (AutoBorderRadius)
            BorderRadius = new Vector4(Math.Min(Bounds.Width, Bounds.Height) / 2f - 0.5f);
        base.Draw(gameTime, spriteBatch);
    }
}

/// <summary>
/// 拨动开关
/// </summary>
[XmlElementMapping("ToggleSwitch")]
public class SUIToggleSwitch : UIElementGroup
{
    public bool AutoBorderRadius { get; set; } = true;

    public event Action<bool> SwitchDown;
    public event Action<bool> StatusChanged;

    public SUIToggleSwitchThumb Thumb { get; }

    public SUIToggleSwitch()
    {
        Border = 2;
        SetPadding(2f);
        SetSize(36f, 20f);

        Thumb = new SUIToggleSwitchThumb().Join(this);

        StyleSheet.SetStyle(UIElementState.Normal, new StyleDefinition()
            .Set($"{nameof(Thumb)}.{nameof(BackgroundColor)}", SUIColor.Foreground)
            .Set($"{nameof(Thumb)}.{nameof(Thumb.Left)}", new Anchor(0, 0, 0))
            .Background(SUIColor.Foreground * 0.25f)
            .BorderColor(SUIColor.Foreground));

        StyleSheet.SetStyle(UIElementState.Custom1, new StyleDefinition()
            .Set($"{nameof(Thumb)}.{nameof(BackgroundColor)}", SUIColor.Highlight)
            .Set($"{nameof(Thumb)}.{nameof(Thumb.Left)}", new Anchor(0, 0, 1))
            .Background(SUIColor.Highlight * 0.25f)
            .BorderColor(SUIColor.Highlight));
    }

    public virtual void OnSwitchDown(bool value)
    {
        Status = value;
        SwitchDown?.Invoke(value);
    }


    public virtual void OnStatusChanged(bool value)
    {
        StatusChanged?.Invoke(value);

        if (value) AddState(UIElementState.Custom1);
        else RemoveState(UIElementState.Custom1);
    }

    public virtual bool Status
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnStatusChanged(value);
        }
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        OnSwitchDown(!Status);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (AutoBorderRadius)
            BorderRadius = new Vector4(Math.Min(Bounds.Width, Bounds.Height) / 2f - 0.5f);
        base.Draw(gameTime, spriteBatch);
    }
}
