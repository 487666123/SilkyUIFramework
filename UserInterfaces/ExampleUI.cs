namespace SilkyUIFramework.UserInterfaces;

#if DEBUG && false

[RegisterGlobalUI("ExampleUI", 0)]
public partial class ExampleUI(IMouseMenu menuUI) : BaseBody
{
    private readonly IMouseMenu MenuUI = menuUI;

    public SUIDraggableView DraggableView { get; protected set; }

    //readonly FontSystem _fontSystem = new();

    protected override void OnInitialize()
    {

        InitializeComponent();

        FitWidth = true;
        FitHeight = true;
        SetGap(10f);

        SetLeft(0f, 0f, 0.5f);
        SetTop(0f, 0f, 0.5f);

        MainAlignment = MainAlignment.Center;
        CrossAlignment = CrossAlignment.Stretch;

        BorderColor = Color.Black;

        var backgroundColor = new Color(63, 65, 151);
        var borderColor = new Color(18, 18, 38);

        DraggableView = new SUIDraggableView(this)
        {
            LayoutType = LayoutType.Flexbox,
            FlexDirection = FlexDirection.Row,
            MainAlignment = MainAlignment.Start,
            CrossAlignment = CrossAlignment.Center,
            CrossContentAlignment = CrossContentAlignment.Center
        }.Join(this);
        DraggableView.SetHeight(50f);

        var textView1 = new UITextView()
        {
            Text = "Example UI 1",
            BackgroundColor = Color.White * 0.5f,
        }.Join(DraggableView);

        var textView3 = new UITextView()
        {
            Text = "Example UI 3",
            BackgroundColor = Color.White * 0.5f,
        }.Join(DraggableView);

        var editText = new SUIEditText()
        {
            BackgroundColor = backgroundColor * 0.25f,
            Border = 2f,
            BorderColor = borderColor,
        }.Join(this);
        editText.SetHeight(50f);

        // 滚动
        var blockContainer = new SUIScrollView()
        {
            Container = {
                BackgroundColor = Color.Red * 0.25f,
                Padding = new Margin(12f),
                LayoutType = LayoutType.Flexbox,
                FlexWrap = true,
                FlexDirection = FlexDirection.Row,
                MainAlignment = MainAlignment.SpaceBetween,
                CrossAlignment = CrossAlignment.Start,
                //TemplateColumns = [..TemplateUnit.Repeat(5, 0f, 1f)],
                //TemplateRows = [..TemplateUnit.Repeat(3, 0f, 1f)],
            },
        }.Join(this);
        blockContainer.SetSize(500f, 300f);
        blockContainer.SetGap(10f);

        blockContainer.ScrollBar.OnUpdateStatus += _ =>
        {
            blockContainer.Container.MainAlignment = MainAlignment.SpaceBetween;
            blockContainer.ScrollBar.SetWidth(blockContainer.ScrollBar.HoverTimer.Lerp(10f, 100f));
        };

        var textView2 = new UITextView()
        {
            Text = "人是铁，饭是钢，一顿不吃饿的慌。",
            FitWidth = false,
            WordWrap = true,
            Padding = 2,
        }.Join(blockContainer.Container);
        textView2.SetWidth(0f, 1f);

        for (int i = 0; i < 2000; i++)
        {
            // 创建并添加
            var block = new SUIItemSlot
            {
                ItemInteractive = false,
                DisplayItemInfo = false,
                BorderRadius = new Vector4(8f),
                Border = 2f,
                BorderColor = borderColor * 0.5f,
                BackgroundColor = backgroundColor * 0.25f,
            }.Join(blockContainer.Container);
            block.RightMouseDown += (sender, evt) =>
            {
                List<string> list = [$"复制", "删除"];

                MenuUI.OpenMenu(MouseAnchor.TopLeft, block.Bounds.Center, list, (content, index) =>
                {
                    switch (content)
                    {
                        case "复制":
                        {
                            Main.LocalPlayer.QuickSpawnItem(null, block.Item.type, block.Item.maxStack);
                            break;
                        }
                        case "删除":
                        {
                            block.Remove();
                            break;
                        }
                    }
                    return true;
                });
            };

            block.DisplayItemStack = true;
            block.Item = new Item(i + 1);
            block.Item.stack = block.Item.maxStack;
            block.SetSize(50f, 50f, 0f, 0f);
        }

        var container2 = new UIElementGroup
        {
            LayoutType = LayoutType.Flexbox,
            FlexDirection = FlexDirection.Row,
            MainAlignment = MainAlignment.SpaceEvenly,
            BackgroundColor = Color.Red * 0.25f,
            FitWidth = true,
            FitHeight = true,
            FlexGrow = 1f,
            OverflowHidden = true,
            Padding = 10f,
        }.Join(this);
        container2.SetWidth(500f);

        var box5 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container2);
        box5.SetSize(100f, 40f);

        var box6 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container2);
        box6.SetSize(100f, 100f);

        var box7 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container2);
        box7.SetSize(100f, 80f);

        var box8 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container2);
        box8.SetSize(100f, 100f);

        var container3 = new UIElementGroup
        {
            LayoutType = LayoutType.Flexbox,
            FlexDirection = FlexDirection.Row,
            MainAlignment = MainAlignment.SpaceBetween,
            CrossAlignment = CrossAlignment.End,
            BackgroundColor = Color.Black * 0.25f,
            FitWidth = true,
            FitHeight = true,
            FlexGrow = 1f,
            OverflowHidden = true,
            Padding = 10f,
        }.Join(this);
        container3.SetWidth(500f);

        var box9 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container3);
        box9.SetSize(100f, 100f);

        var box10 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container3);
        box10.SetSize(100f, 80f);

        var box11 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container3);
        box11.SetSize(100f, 100f);

        var box12 = new UIView
        { BackgroundColor = Color.White * 0.5f }.Join(container3);
        box12.SetSize(100f, 20f);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        UseRenderTarget = false;

        FitWidth = false;
        SetWidth(300);
    }

    protected override void UseRenderTargetDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Opacity = HoverTimer.Lerp(0.5f, 1f);
        var scale = HoverTimer.Lerp(1f, 1.1f);
        RenderTargetMatrix =
            Matrix.CreateTranslation(-Bounds.Center.X * 2f, -Bounds.Center.Y * 2f, 0f) *
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(Bounds.Center.X * 2f, Bounds.Center.Y * 2f, 0f);
        base.UseRenderTargetDraw(gameTime, spriteBatch);
    }
}

#endif