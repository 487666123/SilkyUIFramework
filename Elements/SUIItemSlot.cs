using SilkyUIFramework.Components;

namespace SilkyUIFramework.Elements;

/// <summary>
/// 物品槽 UI 组件，支持原版物品栏交互（左键交换/堆叠、右键拆分、显示物品信息）。
/// 映射到 XML 元素 "ItemSlot"。
/// </summary>
[XmlElementMapping("ItemSlot")]
public class SUIItemSlot : UIView
{
    /// <summary>玩家当前是否在使用物品（动画播放中），用于阻止交互。</summary>
    public static bool PlayerInUseItem => Main.LocalPlayer?.ItemAnimationActive ?? false;

    /// <summary>物品变化时触发，old == new 时不会触发（参见 <see cref="ItemsEffectivelyEqual"/>）。</summary>
    public event EventHandler<ValueChangedEventArgs<Item>> ItemChanged;

    protected Item ItemInside = new();

    /// <summary>当前槽位中的物品。设置时会自动跳过逻辑等价（两个 Air 视为相等）以避免不必要的事件。</summary>
    public virtual Item Item
    {
        get => ItemInside;
        set
        {
            if (ItemsEffectivelyEqual(ItemInside, value)) return;
            var oldItem = ItemInside;
            ItemInside = value;
            OnItemChanged(oldItem, value);
        }
    }

    /// <summary>触发 <see cref="ItemChanged"/> 事件。</summary>
    protected virtual void OnItemChanged(Item oldItem, Item newItem) => ItemChanged?.Invoke(this, new(oldItem, newItem));

    /// <summary>
    /// 两个不同实例的空物品在逻辑上等价，避免 setter 不必要地触发 OnItemChanged。
    /// </summary>
    public static bool ItemsEffectivelyEqual(Item a, Item b) => a == b || (a.IsAir && b.IsAir);

    /// <summary>悬停时是否在游戏内显示物品信息面板。</summary>
    public bool DisplayItemInfo { get; set; } = true;

    /// <summary>是否绘制物品堆叠数量文字。</summary>
    public bool DisplayItemStack { get; set; } = true;

    /// <summary>即使堆叠数量为 1 也始终显示数量文字。</summary>
    public bool AlwaysDisplayItemStack { get; set; } = false;

    /// <summary>是否允许玩家与物品交互（点击/交换/拆分）。</summary>
    public bool ItemInteractive { get; set; } = true;

    /// <summary>物品图标的最大像素尺寸，超出会等比缩放。</summary>
    public float ItemIconSizeLimit { get; set; } = 32f;

    /// <summary>物品图标的额外缩放倍数。</summary>
    public float ItemScale { get; set; } = 1f;

    /// <summary>物品图标的多重颜色叠加。</summary>
    public Color ItemColor { get; set; } = Color.White;

    /// <summary>物品图标相对于 <see cref="InnerBounds"/> 的偏移量。</summary>
    public Vector2 ItemOffset { get; set; } = Vector2.Zero;

    /// <summary>物品图标在 <see cref="InnerBounds"/> 内的对齐锚点（0~1 归一化坐标）。</summary>
    public Vector2 ItemAlign { get; set; } = new(0.5f);

    public SUIItemSlot()
    {
        // 默认边框与背景色，模拟原版物品槽的半透明暗色风格
        Border = 2;
        BorderColor = Color.Black * 0.75f;
        BackgroundColor = Color.Black * 0.5f;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        HandleItemSlotLeftClick();
    }

    /// <summary>子类重写此方法以限制可放入的物品类型。</summary>
    public virtual bool CanPutInItemSlot(Item item) => true;

    /// <summary>处理左键点击交互：同物品堆叠 → 合并数量；不同物品 → 交换（受 <see cref="CanPutInItemSlot"/> 限制）。</summary>
    protected virtual void HandleItemSlotLeftClick()
    {
        if (PlayerInUseItem) return;

        // 开启物品栏 &&（物品框 || 鼠标）至少有一个 NonAir
        if (ItemInteractive && Main.playerInventory && (Main.mouseItem.NotAir() || Item.NotAir()))
        {
            // 物品相同且未堆叠满：尝试将鼠标上的物品堆叠到槽位中
            if (Main.mouseItem.type == Item.type && Item.NotAir() && Item.NotFull())
            {
                TryStackItem(Item, Main.mouseItem, out var numTransferred);
                if (numTransferred > 0)
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
            // 物品不同：交换手中物品（受 CanPutInItemSlot 限制）
            else
            {
                if (CanPutInItemSlot(Main.mouseItem))
                {
                    (Main.mouseItem, Item) = (Item, Main.mouseItem);
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }
    }

    /// <summary>右键长按计时器（帧计数），用于实现渐进加速的拆分逻辑。</summary>
    protected TimeSpan LastRightClickActionTime;

    /// <summary>记录右键按下的起始帧，为长按加速拆分提供基准。</summary>
    public override void OnRightMouseDown(UIMouseEvent evt)
    {
        base.OnRightMouseDown(evt);
        LastRightClickActionTime = Main.gameTimeCache.TotalGameTime;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        HandleItemSlotRightLongPress();
    }

    /// <summary>
    /// 右键长按拆分逻辑，带渐进加速曲线：
    /// <list type="bullet">
    ///   <item>0~30 tick：每 15 tick 拆分 1 个（精确控制）</item>
    ///   <item>30~60 tick：每 5 tick 拆分 1 个</item>
    ///   <item>60~90 tick：每 tick 拆分 1 个</item>
    ///   <item>90~150 tick：每 tick 拆分 11 个</item>
    ///   <item>150+ tick：每 tick 拆分 33 个</item>
    /// </list>
    /// </summary>
    protected virtual void HandleItemSlotRightLongPress()
    {
        if (PlayerInUseItem) return;

        // 物品不可交互 || 右键没有按下 || 鼠标没有悬浮 || 物品为空
        if (!ItemInteractive || !Main.playerInventory || LeftMousePressed || !RightMousePressed || !IsMouseHovering || Item.IsAir) return;

        if (Main.mouseItem.IsAir)
        {
            // 鼠标上没有物品, 所以是首次拿起, 只有可以堆叠的物品可以用右键拿起
            if (Item.maxStack > 1)
            {
                Main.mouseItem = new Item(Item.type);
                Item.stack -= 1;
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }
        else
        {
            var tick = (int)(Main.gameTimeCache.TotalGameTime - LastRightClickActionTime).TotalMilliseconds;
            // 鼠标上有物品
            if (Item.type == Main.mouseItem.type)
            {
                // 右键长按加速曲线：先慢（逐 tick 可控），后快（批量转移）
                switch (tick)
                {
                    case < 500:
                    {
                        if (tick % 250 is 0)
                        {
                            TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        }
                        break;
                    }
                    case < 1000:
                    {
                        if (tick % 100 is 0)
                        {
                            TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        }
                        break;
                    }
                    case < 2000:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        break;
                    }
                    case < 3000:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 9);
                        break;
                    }
                    default:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 36);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 尝试将 source 中的物品堆叠到 destination 中。
    /// </summary>
    /// <param name="destination">目标物品（接收堆叠）。</param>
    /// <param name="source">源物品（被抽取）。</param>
    /// <param name="numTransferred">实际转移的数量。</param>
    /// <param name="infiniteSource">源物品是否为无限（不会减少 stack）。</param>
    /// <param name="numToTransfer">限制最大转移数量；为 null 时转移全部。</param>
    public static void TryStackItem(Item destination, Item source, out int numTransferred, bool infiniteSource = false, int? numToTransfer = null)
    {
        if (!destination.IsAir && !source.IsAir && destination.type == source.type && ItemLoader.CanStack(destination, source))
        {
            if (numToTransfer.HasValue)
                numToTransfer = Math.Min(numToTransfer.Value, source.stack);
            ItemLoader.StackItems(destination, source, out numTransferred, infiniteSource, numToTransfer);
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }

        numTransferred = 0;
    }

    #region Draw

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        if (Item.IsAir) return;

        // 悬停时向游戏注册物品信息，触发原版物品信息提示框
        if (DisplayItemInfo && IsMouseHovering)
        {
            Main.hoverItemName = Item.Name;
            Main.HoverItem = Item.Clone();
        }

        DrawItemIcon(spriteBatch, Item, Color.White, InnerBounds.Position + ItemOffset + (Vector2)InnerBounds.Size * ItemAlign, ItemIconSizeLimit, ItemScale, ItemAlign);

        if (AlwaysDisplayItemStack || (DisplayItemStack && Item.stack > 1))
        {
            DrawItemStack(spriteBatch);
        }
    }

    /// <summary>堆叠数字的格式化字符串，如 "{0}" 或 "x{0}"。</summary>
    public string StackFormat { get; set; } = "{0}";

    /// <summary>堆叠数字在 <see cref="InnerBounds"/> 内的对齐锚点（0~1 归一化坐标）。</summary>
    public Vector2 StackAlign { get; set; } = new(0.18f, 0.9f);

    /// <summary>绘制物品堆叠数量，带描边阴影效果。</summary>
    public void DrawItemStack(SpriteBatch spriteBatch)
    {
        var font = FontAssets.ItemStack.Value;
        var stack = string.Format(StackFormat, Item.stack);
        var textSize = font.MeasureString(stack) * 0.75f * ItemScale;
        var position = InnerBounds.Position + ((Vector2)InnerBounds.Size - textSize) * StackAlign;

        // 黑色描边（多个方向的偏移叠加）
        foreach (var offset in SnippetModule.ShadowOffsets)
        {
            spriteBatch.DrawString(font, stack, position + offset * 2f, Color.Black, 0f, Vector2.Zero, 0.75f, 0f, 1f);
        }
        // 白色前景
        spriteBatch.DrawString(font, stack, position, Color.White, 0f, Vector2.Zero, 0.75f, 0f, 1f);
    }

    /// <summary>
    /// 绘制物品图标，自动处理缩放、颜色叠加、动画帧以及特殊指示器（陷阱、不安全、碎石放置等）。
    /// 逻辑与原版 <c>Main.DrawItemInternal</c> 保持一致。
    /// </summary>
    public static void DrawItemIcon(SpriteBatch spriteBatch, Item item, Color color, Vector2 center,
        float sizeLimit = 32f, float sizeScale = 1f, Vector2? iconAlign = null)
    {
        Main.instance.LoadItem(item.type);
        var texture2D = TextureAssets.Item[item.type].Value;
        var frame = Main.itemAnimations[item.type]?.GetFrame(texture2D) ?? texture2D.Frame();

        sizeScale *= (frame.Width > sizeLimit || frame.Height > sizeLimit)
            ? frame.Width > frame.Height ? sizeLimit / frame.Width : sizeLimit / frame.Height
            : 1f;
        var origin = frame.Size() * (iconAlign ?? new Vector2(0.5f));

        if (ItemLoader.PreDrawInInventory(item, spriteBatch, center, frame, item.GetAlpha(color),
                item.GetColor(color), origin, sizeScale))
        {
            spriteBatch.Draw(texture2D, center, frame, item.GetAlpha(color), 0f, origin, sizeScale,
                SpriteEffects.None, 0f);
            if (item.color != Color.Transparent)
                spriteBatch.Draw(texture2D, center, frame, item.GetColor(color), 0f, origin, sizeScale,
                    SpriteEffects.None, 0f);
        }

        ItemLoader.PostDrawInInventory(item, spriteBatch, center, frame, item.GetAlpha(color),
            item.GetColor(color), origin, sizeScale);

        // 陷阱类物品的红色 wire 指示器
        if (ItemID.Sets.TrapSigned[item.type])
            Main.spriteBatch.Draw(TextureAssets.Wire.Value, center + new Vector2(14f) * sizeScale,
                new Rectangle(4, 58, 8, 8), color, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);

        // 不安全物品的警告指示器
        if (ItemID.Sets.DrawUnsafeIndicator[item.type])
        {
            var vector2 = new Vector2(-4f, -4f) * sizeScale;
            var value7 = TextureAssets.Extra[ExtrasID.UnsafeIndicator].Value;
            var rectangle2 = value7.Frame();
            Main.spriteBatch.Draw(value7, center + vector2 + new Vector2(14f) * sizeScale, rectangle2, color, 0f,
                rectangle2.Size() / 2f, 1f, SpriteEffects.None, 0f);
        }

        // 碎石放置物品的放置模式指示器
        if (item.type is ItemID.RubblemakerSmall or ItemID.RubblemakerMedium or ItemID.RubblemakerLarge)
        {
            var vector3 = new Vector2(2f, -6f) * sizeScale;
            switch (item.type)
            {
                case ItemID.RubblemakerSmall:
                {
                    var value10 = TextureAssets.Extra[ExtrasID.RubbleMakerIndicator].Value;
                    var rectangle5 = value10.Frame(3, 1, 2);
                    Main.spriteBatch.Draw(value10, center + vector3 + new Vector2(16f) * sizeScale, rectangle5,
                        color, 0f, rectangle5.Size() / 2f, 1f, SpriteEffects.None, 0f);
                    break;
                }
                case ItemID.RubblemakerMedium:
                {
                    var value9 = TextureAssets.Extra[ExtrasID.RubbleMakerIndicator].Value;
                    var rectangle4 = value9.Frame(3, 1, 1);
                    Main.spriteBatch.Draw(value9, center + vector3 + new Vector2(16f) * sizeScale, rectangle4,
                        color, 0f, rectangle4.Size() / 2f, 1f, SpriteEffects.None, 0f);
                    break;
                }
                case ItemID.RubblemakerLarge:
                {
                    var value8 = TextureAssets.Extra[ExtrasID.RubbleMakerIndicator].Value;
                    var rectangle3 = value8.Frame(3);
                    Main.spriteBatch.Draw(value8, center + vector3 + new Vector2(16f) * sizeScale, rectangle3,
                        color, 0f, rectangle3.Size() / 2f, 1f, SpriteEffects.None, 0f);
                    break;
                }
            }
        }
    }

    #endregion
}