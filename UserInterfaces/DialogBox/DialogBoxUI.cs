#if DEBUG && false

namespace SilkyUIFramework.UserInterfaces.DialogBox;

/// <summary>
/// 没啥鸟用
/// </summary>
[RegisterGlobalUI(priority: 2000)]
public class DialogBoxUI : BaseBody
{
    private UITextView TextView { get; set; }
    private SUIEditText EditText { get; set; }

    protected override void OnInitialize()
    {
        Enabled = true;
        EnableBlur = true;
        OverflowHidden = true;
        BorderRadius = new Vector4(12f);

        Gap = 0f;
        SetSize(300f, 300f);

        TextView = new UITextView
        {
            Padding = 12f,
            FitWidth = false,
            Width = new Dimension(0f, 1f),
            WordWrap = true,
            Text = "Hello World!Hello World!Hello World!Hello World!Hello World!",
        }.Join(this);
        TextView.BackgroundColor = Color.Red * 0.25f;

        new HorizontalRule().Join(this);

        EditText = new SUIEditText
        {
            Padding = 12f,
            FitWidth = false,
            Width = new Dimension(0f, 1f),
            WordWrap = true,
            Text = "Line 2",
        }.Join(this);
        EditText.BackgroundColor = Color.Green * 0.25f;

        new HorizontalRule().Join(this);

        EditText = new SUIEditText
        {
            Padding = 12f,
            FitWidth = false,
            Width = new Dimension(0f, 1f),
            WordWrap = true,
            Text = "Line 3",
        }.Join(this);
        EditText.BackgroundColor = Color.Blue * 0.25f;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        OverflowHidden = true;

        SetLeft(0f, 0f, 0.5f);
        SetTop(0f, 0f, 0.25f);
        SetSize(550f, 500f);

        //MarkLayoutDirty();

        TextView.WordWrap = true;
        TextView.Text = $"[c/ff0000:[调试][c/ff0000:] 不会出现在发布版]\n" +
            $"[c/ff0000:{GetType().FullName!.Replace('.', '\\')}]";

        if (Children.Count >= 3 && Children[2] is SUIEditText editText)
        {
            editText.Placeholder = "被你发现了 =)";
        }

        EditText.Padding = 12f;
        EditText.Placeholder = "请输入";
        EditText.WordWrap = true;
        EditText.FitWidth = true;
        EditText.MaxWidth = new Dimension(0f, 1f);
        EditText.UseDeathText();
    }
}

#endif