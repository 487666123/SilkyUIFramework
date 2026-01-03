using SilkyUIFramework.Components;

namespace SilkyUIFramework.Elements;

[XmlElementMapping("ItemSlot")]
public class SUIItemSlot : UIView
{
    public static bool PlayerInUseItem => Main.LocalPlayer?.ItemAnimationActive ?? false;

    public event EventHandler<ValueChangedEventArgs<Item>> ItemChanged;

    protected Item ItemInside = new();

    public virtual Item Item
    {
        get => ItemInside;
        set
        {
            if (ItemEqual(ItemInside, value)) return;
            var oldItem = ItemInside;
            ItemInside = value;
            OnItemChanged(oldItem, value);
        }
    }

    protected virtual void OnItemChanged(Item oldItem, Item newItem)
    {
        ItemChanged?.Invoke(this, new(oldItem, newItem));
    }

    public static bool ItemEqual(Item a, Item b) => a == b || (a.IsAir && b.IsAir);

    /// <summary>
    /// 允许显示物品信息
    /// </summary>
    public bool DisplayItemInfo { get; set; } = true;

    /// <summary>
    /// 允许显示物品堆叠数量
    /// </summary>
    public bool DisplayItemStack { get; set; } = true;

    /// <summary>
    /// 总是显示物品堆叠数量，即使堆叠数量为 1
    /// </summary>
    public bool AlwaysDisplayItemStack { get; set; } = false;

    public bool ItemInteractive { get; set; } = true;

    public float ItemIconSizeLimit { get; set; } = 32f;
    public float ItemScale { get; set; } = 1f;
    public Color ItemColor { get; set; } = Color.White;
    public Vector2 ItemOffset { get; set; } = Vector2.Zero;
    public Vector2 ItemAlign { get; set; } = new(0.5f);

    public SUIItemSlot()
    {
        Border = 2;
        BorderColor = Color.Black * 0.75f;
        BackgroundColor = Color.Black * 0.5f;
    }

    protected int RightMousePressedTimer;

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        HandleItemSlotLeftClick();
    }

    /// <summary>
    /// 能否放入
    /// </summary>
    public virtual bool CanPutInItemSlot(Item item)
    {
        return true;
    }

    protected virtual void HandleItemSlotLeftClick()
    {
        if (PlayerInUseItem) return;

        // 开启物品栏 && (物品框 || 鼠标) 至少有一个 NonAir
        if (ItemInteractive && Main.playerInventory && (Main.mouseItem.NotAir() || Item.NotAir()))
        {
            // 物品相同: 未堆叠满堆叠入物品栏
            if (Main.mouseItem.IsTheSameAs(Item) && Item.NotAir() && Item.NotFull())
            {
                TryStackItem(Item, Main.mouseItem, out var numTransferred);
                if (numTransferred > 0)
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
            // 物品不同交换手中物品
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

    public override void OnRightMouseDown(UIMouseEvent evt)
    {
        base.OnRightMouseDown(evt);
        RightMousePressedTimer = 0;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        HandleItemSlotRightLongPress();
        if (RightMousePressed)
            RightMousePressedTimer++;
    }

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
            // 鼠标上有物品
            if (Item.IsTheSameAs(Main.mouseItem))
            {
                switch (RightMousePressedTimer)
                {
                    case < 30:
                    {
                        if (RightMousePressedTimer % 15 is 0)
                        {
                            TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        }
                        break;
                    }
                    case < 60:
                    {
                        if (RightMousePressedTimer % 5 is 0)
                        {
                            TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        }
                        break;
                    }
                    case < 90:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 1);
                        break;
                    }
                    case < 150:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 11);
                        break;
                    }
                    default:
                    {
                        TryStackItem(Main.mouseItem, Item, out _, numToTransfer: 33);
                        break;
                    }
                }
            }
        }
    }

    public static void TryStackItem(Item destination, Item source, out int numTransferred, bool infiniteSource = false, int? numToTransfer = null)
    {
        if (!destination.IsAir && !source.IsAir && ItemSameAndCanStack(destination, source))
        {
            if (numToTransfer.HasValue)
                numToTransfer = Math.Min(numToTransfer.Value, source.stack);
            ItemLoader.StackItems(destination, source, out numTransferred, infiniteSource, numToTransfer);
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }

        numTransferred = 0;
    }

    public static bool ItemSameAndCanStack(Item destination, Item source)
    {
        return destination.IsTheSameAs(source) && ItemLoader.CanStack(destination, source);
    }

    #region Draw

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        if (Item.IsAir) return;

        if (DisplayItemInfo && IsMouseHovering)
        {
            Main.hoverItemName = Item.Name;
            Main.HoverItem = Item.Clone();
        }

        DrawItemIcon(spriteBatch, Item, Color.White, InnerBounds.Position + ItemOffset + (Vector2)InnerBounds.Size * ItemAlign, ItemIconSizeLimit, ItemScale, ItemAlign);

        // 绘制物品堆叠数字
        if (AlwaysDisplayItemStack || (DisplayItemStack && Item.stack > 1))
        {
            DrawItemStack(spriteBatch);
        }
    }

    public string StackFormat { get; set; } = "{0}";
    public Vector2 StackAlign { get; set; } = new(0.18f, 0.9f);

    public void DrawItemStack(SpriteBatch spriteBatch)
    {
        var font = FontAssets.ItemStack.Value;
        var stack = string.Format(StackFormat, Item.stack);
        var textSize = font.MeasureString(stack) * 0.75f * ItemScale;
        var position = InnerBounds.Position + ((Vector2)InnerBounds.Size - textSize) * StackAlign;

        foreach (var offset in SnippetModule.ShadowOffsets)
        {
            spriteBatch.DrawString(font, stack, position + offset * 2f, Color.Black, 0f, Vector2.Zero, 0.75f, 0f, 1f);
        }
        spriteBatch.DrawString(font, stack, position, Color.White, 0f, Vector2.Zero, 0.75f, 0f, 1f);
    }

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

        if (ItemID.Sets.TrapSigned[item.type])
            Main.spriteBatch.Draw(TextureAssets.Wire.Value, center + new Vector2(14f) * sizeScale,
                new Rectangle(4, 58, 8, 8), color, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);

        if (ItemID.Sets.DrawUnsafeIndicator[item.type])
        {
            var vector2 = new Vector2(-4f, -4f) * sizeScale;
            var value7 = TextureAssets.Extra[ExtrasID.UnsafeIndicator].Value;
            var rectangle2 = value7.Frame();
            Main.spriteBatch.Draw(value7, center + vector2 + new Vector2(14f) * sizeScale, rectangle2, color, 0f,
                rectangle2.Size() / 2f, 1f, SpriteEffects.None, 0f);
        }

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