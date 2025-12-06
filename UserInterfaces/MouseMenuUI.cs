using SilkyUIFramework.Animation;

namespace SilkyUIFramework.UserInterfaces;

public delegate bool MouseMenuCallback(string name, int index);

public interface IMouseMenu
{
    void OpenMenu(MouseAnchor mouseAnchor, Vector2 mousePosition, List<string> contents, MouseMenuCallback callback);
}

public enum MouseAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

#if true

[RegisterGlobalUI(priority: 1000)]
public partial class MouseMenuUI : BaseBody, IMouseMenu
{
    public static bool IsShow { get; set; }

    public override bool Enabled
    {
        get
        {
            if (IsShow) return true;
            return !_switchTimer.IsReverseCompleted;
        }
        set => IsShow = value;
    }

    public override bool IsInteractable => _switchTimer.IsForward;

    public override Bounds BlurBounds => MenuContainer.Bounds;
    public override Vector4 BlurBorderRadius => MenuContainer.BorderRadius;

    protected override void OnInitialize()
    {
        EnableBlur = true;
        InitializeComponent();

        SetLeft(0f, 0f, 0f);
        SetTop(0f, 0f, 0f);

        MenuContainer.BorderColor = SUIColor.Border * 0.75f;
        MenuContainer.BackgroundColor = SUIColor.Background * 0.75f;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if (evt.Source == this) Enabled = false;
        base.OnLeftMouseDown(evt);
    }

    private readonly AnimationTimer _switchTimer = new(3);

    protected override void UpdateStatus(GameTime gameTime)
    {
        if (IsShow) _switchTimer.StartUpdate();
        else _switchTimer.StartReverseUpdate();

        _switchTimer.Update(gameTime);

        UseRenderTarget = _switchTimer.IsUpdating;
        Opacity = _switchTimer.Lerp(0f, 1f);

        RenderTargetMatrix = Matrix.CreateTranslation(0, _switchTimer.Lerp(10f, 0), 0);

        base.UpdateStatus(gameTime);
    }

    public void OpenMenu(MouseAnchor mouseAnchor, Vector2 mousePosition, List<string> contents,
        MouseMenuCallback callback)
    {
        Enabled = true;

        switch (mouseAnchor)
        {
            case MouseAnchor.TopLeft:
            {
                MenuContainer.SetLeft(mousePosition.X, 0, 0);
                MenuContainer.SetTop(mousePosition.Y, 0, 0);
                break;
            }
            case MouseAnchor.TopRight:
            {
                MenuContainer.SetLeft(mousePosition.X, -1f, 1f);
                MenuContainer.SetTop(mousePosition.Y, 0, 0);
                break;
            }
            case MouseAnchor.BottomLeft:
            {
                MenuContainer.SetLeft(mousePosition.X, 0, 0);
                MenuContainer.SetTop(mousePosition.Y, -1f, 1f);
                break;
            }
            case MouseAnchor.BottomRight:
            {
                MenuContainer.SetLeft(mousePosition.X, -1f, 1f);
                MenuContainer.SetTop(mousePosition.Y, -1f, 1f);
                break;
            }
        }

        ScrollView.Container.RemoveAllChildren();

        for (var i = 0; i < contents.Count; i++)
        {
            var text = new MouseMenuItem(contents[i], i).Join(ScrollView.Container);
            text.MouseMenuCallback = callback;
        }
    }
}

#endif