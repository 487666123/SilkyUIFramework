using SilkyUIFramework.Animation;
using SilkyUIFramework.Common.Tweening;

namespace SilkyUIFramework.UserInterfaces;

public delegate bool MouseMenuCallback(object obj, int index);

public record class MouseMenuItem(string Name, object Content = null);

public interface IMouseMenu
{
    void OpenMenu(MouseAnchor mouseAnchor, Vector2 mousePosition, List<MouseMenuItem> items, MouseMenuCallback callback);
}

public enum MouseAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

[RegisterGlobalUI(priority: 1000)]
public partial class MouseMenuUI : BaseBody, IMouseMenu
{
    public Tween StartTween { get; set; }

    public void Switch(bool enabled)
    {
        if (enabled) ToStatus(1f, Matrix.CreateTranslation(0, 0f, 0), enabled);
        else ToStatus(0f, Matrix.CreateTranslation(0, 10f, 0), enabled);
    }

    private void ToStatus(float opacity, Matrix matrix, bool enabled)
    {
        StartTween?.Kill();

        Enabled = true;
        UseRenderTarget = true;

        var tween = StartTween = CreateTween().Parallel().SetEase(EaseType.Out).SetTrans(TransitionType.Quint);
        StartTween.FadeTo(this, opacity, 0.2f);
        StartTween.MemberTo(this, nameof(RenderTargetMatrix), matrix, 0.2f, static (left, right, t) => Matrix.Lerp(left, right, t));
        StartTween.OnFinished += () =>
        {
            Enabled = enabled;
            UseRenderTarget = false;
            StartTween = null;
        };
    }

    public override bool IsInteractable => StartTween?.IsFinished ?? true;

    public override IEnumerable<UIView> BlurElements => [MenuContainer];

    protected override void OnInitialize()
    {
        EnableBlur = true;
        InitializeComponent();

        SetLeft(0f, 0f, 0f);
        SetTop(0f, 0f, 0f);

        MenuContainer.BorderColor = SUIColor.Border * 0.75f;
        MenuContainer.BackgroundColor = SUIColor.Background * 0.75f;

        Switch(false);
        Enabled = false;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if (evt.Source == this) Switch(false);
        base.OnLeftMouseDown(evt);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        UseRenderTarget = true;
    }

    public void OpenMenu(MouseAnchor mouseAnchor, Vector2 mousePosition, List<MouseMenuItem> items,
        MouseMenuCallback callback)
    {
        Switch(true);

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

        for (var i = 0; i < items.Count; i++)
        {
            new UIMouseMenuItem(items[i].Name, i)
            {
                MouseMenuCallback = callback
            }.Join(ScrollView.Container);
        }
    }
}