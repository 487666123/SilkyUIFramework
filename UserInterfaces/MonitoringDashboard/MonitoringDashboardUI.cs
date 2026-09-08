using SilkyUIFramework.Layout;

namespace SilkyUIFramework.UserInterfaces.MonitoringDashboard;

[RegisterUI]
public partial class MonitoringDashboardUI : BaseBody
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);
    private readonly List<TargetRow> _rows = [];
    private TimeSpan _elapsed = RefreshInterval;

    protected override void OnInitialize()
    {
        InitializeComponent();

        Metrics.SetTemplateColumns([GridTrack.Fr(1f), GridTrack.Fr(1f), GridTrack.Fr(1f)]);
        Metrics.SetTemplateRows([GridTrack.Pixels(24f), GridTrack.Fr(1f)]);
        SetTableColumns(TableHeader);

        DragPanel.ControlTarget = this;
        TargetTable.ScrollBar.Width = new Dimension(8f);
        TargetTable.ScrollBar.BackgroundColor = new Color(65, 73, 80);
        TargetTable.ScrollBar.Thumb.BarColor = (new Color(154, 184, 199), new Color(114, 216, 171));

#if DEBUG
        Enabled = true;
#endif
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        _elapsed += gameTime.ElapsedGameTime;
        if (_elapsed < RefreshInterval) return;
        _elapsed = TimeSpan.Zero;

        var pool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();
        var sizes = pool.Available.Keys.Concat(pool.Occupied.Keys)
            .Distinct().OrderBy(size => size.Width).ThenBy(size => size.Height);
        var rowIndex = 0;
        var totalAvailable = 0;
        var totalOccupied = 0;
        long totalBytes = 0;

        foreach (var size in sizes)
        {
            var available = pool.Available.TryGetValue(size, out var free) ? free.Count : 0;
            var occupied = pool.Occupied.TryGetValue(size, out var used) ? used.Count : 0;
            if (available + occupied == 0) continue;

            // 保留原有每像素 4 字节的估算口径，不将其当作实际 GPU 显存占用。
            var bytes = (long)size.Width * (long)size.Height * 4L * (available + occupied);
            if (rowIndex == _rows.Count)
                _rows.Add(new TargetRow(TargetTable.Container, rowIndex));

            var row = _rows[rowIndex++];
            row.Container.Invalid = false;
            row.Dimensions.Text = $"{size.Width:0} x {size.Height:0}";
            row.Available.Text = available.ToString();
            row.Occupied.Text = occupied.ToString();
            row.Memory.Text = $"{bytes / 1048576d:0.00}";
            totalAvailable += available;
            totalOccupied += occupied;
            totalBytes += bytes;
        }

        // 复用已有行；尺寸分组减少时隐藏多余行，避免持续重建 UI 树。
        for (var i = rowIndex; i < _rows.Count; i++)
            _rows[i].Container.Invalid = true;

        EmptyState.Invalid = rowIndex != 0;
        AvailableValue.Text = totalAvailable.ToString();
        OccupiedValue.Text = totalOccupied.ToString();
        MemoryValue.Text = $"{totalBytes / 1048576d:0.00} MiB";
        TargetCount.Text = $"{rowIndex} 种尺寸";
        PoolSummary.Text = $"合计 {totalAvailable + totalOccupied} 个渲染目标";
    }

    private static void SetTableColumns(UIElementGroup table)
    {
        table.SetTemplateColumns([
            GridTrack.Fr(2f), GridTrack.Fr(1f), GridTrack.Fr(1f), GridTrack.Fr(2f)
        ]);
        table.SetTemplateRows([GridTrack.Fr(1f)]);
    }

    private sealed class TargetRow
    {
        public UIElementGroup Container { get; }
        public UITextView Dimensions { get; }
        public UITextView Available { get; }
        public UITextView Occupied { get; }
        public UITextView Memory { get; }

        public TargetRow(UIElementGroup parent, int index)
        {
            Container = new UIElementGroup
            {
                LayoutType = LayoutType.Grid,
                Width = new Dimension(0f, 1f),
                Height = new Dimension(36f),
                Gap = new Size(12f),
                BackgroundColor = index % 2 == 0 ? new Color(43, 49, 56) : new Color(38, 44, 50)
            }.Join(parent);
            Container.SetPadding(12f, 4f);
            SetTableColumns(Container);
            Dimensions = CreateCell(Container, Color.White, 0f);
            Available = CreateCell(Container, new Color(114, 216, 171), 1f);
            Occupied = CreateCell(Container, new Color(207, 168, 98), 1f);
            Memory = CreateCell(Container, new Color(154, 184, 199), 1f);
        }

        private static UITextView CreateCell(UIElementGroup parent, Color color, float alignment)
        {
            return new UITextView
            {
                FitWidth = false,
                FitHeight = false,
                TextScale = 1f,
                TextBorder = 1f,
                TextColor = color,
                TextAlign = new Vector2(alignment, 0.5f)
            }.Join(parent);
        }
    }
}
