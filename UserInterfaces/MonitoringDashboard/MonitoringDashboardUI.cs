using SilkyUIFramework.Layout;

namespace SilkyUIFramework.UserInterfaces.MonitoringDashboard;

[RegisterUI]
public partial class MonitoringDashboardUI : BaseBody
{
    protected override void OnInitialize()
    {
        InitializeComponent();

        GridItem1.ColumnSpan = new GridSpan(null, 3);

        GridItem3.ColumnSpan = new GridSpan(null, 3);

        GridContainer.SetTemplateColumns([
            GridTrack.Auto,
            GridTrack.Fr(1f),
            GridTrack.Fr(1f),
        ]);

        GridContainer.SetTemplateRows([
            GridTrack.Auto,
            GridTrack.Pixels(20f),
            GridTrack.Pixels(20f),
            GridTrack.Pixels(20f),
            GridTrack.Pixels(20f),
        ]);

#if DEBUG
        Enabled = true;
#endif

        DragPanel.ControlTarget = this;
        Title.UseDeathText();

        BorderColor = SUIColor.Border;
        BackgroundColor = SUIColor.Background * 0.75f;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>() is { } pool)
        {
            TargetTable.Container.RemoveAllChildren();
            TargetTable.Container.SetPadding(8);

            var totalBit = 0;
            foreach (var (key, value) in pool.Available)
            {
                var bit = value.Sum(rt => rt.Width * rt.Height);
                totalBit += bit;
                var keyTextView = new UITextView()
                {
                    Text = $"Image Size: {key} Count: {value.Count} Occupied Size: {bit * 4 / 1024 / 1024f:0.00}MB",
                    TextScale = 0.8f,
                }.Join(TargetTable.Container);
            }

            var countText = $"RenderTargetPool {pool.TotalCount} {totalBit * 4 / 1024 / 1024f:0.00}MB";
            TargetCount.Text = countText;
        }
    }
}
