using ReLogic.Content;

namespace SilkyUIFramework.Components
{
    public class Slice_9 : UIElement
    {
        public struct Slice_9_Info(int top, int left, int right, int bottom, int space,
            Rectangle center, Rectangle bound, Point? centerModify = null)
        {
            public int Top = top;
            public int Left = left;
            public int Right = right;
            public int Bottom = bottom;
            public int Space = space;
            public Rectangle Center = center;
            public Rectangle Bound = bound;
            public Point CenterModify = centerModify ?? Point.Zero;
        }

        public Rectangle[] Source, Destination;
        public Asset<Texture2D> Tex;
        public Slice_9_Info Info;

        public Slice_9(Slice_9_Info info)
        {
            Info = info;
            Transfer(info);
        }

        private void Transfer(Slice_9_Info info)
        {
            // 一次性提取所有参数
            var (top, left, right, bottom, space) = (info.Top, info.Left, info.Right, info.Bottom, info.Space);
            var bound = info.Bound;
            var (width, height) = (bound.Width, bound.Height);

            // 计算关键值
            int rightX = width - right;
            int bottomY = height - bottom;
            int middleW = width - left - right - space * 2;
            int middleH = height - top - bottom - space * 2;

            Source =
            [
                new(0, 0, left, top),                    // 0: 左上
                new(rightX, 0, right, top),              // 1: 右上
                new(0, bottomY, left, bottom),           // 2: 左下
                new(rightX, bottomY, right, bottom),     // 3: 右下
        
                new(left + space, 0, middleW, top),              // 4: 上中
                new(0, top + space, left, middleH),              // 5: 左中
                new(rightX , top + space, right, middleH),        // 6: 右中
                new(left + space, bottomY, middleW, bottom),     // 7: 下中
        
                info.Center                               // 8: 中心
            ];

            // 应用偏移
            var (offsetX, offsetY) = (bound.X, bound.Y);
            for (int i = 0; i < 8; i++)
            {
                Source[i].Offset(offsetX, offsetY);
            }
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Destination == null || Destination.Length == 0)
                return;

            DrawAllSlices(spriteBatch, Point.Zero, Color.White, false, false);
        }

        public override void Recalculate()
        {
            base.Recalculate();
            CalculateDrawRect();
        }

        public virtual void CalculateDrawRect()
        {
            Rectangle ui = GetDimensions().ToRectangle();

            int stretchWidth = Math.Max(0, ui.Width - Info.Left - Info.Right);
            int stretchHeight = Math.Max(0, ui.Height - Info.Top - Info.Bottom);

            int left = ui.Left;
            int top = ui.Top;
            int right = ui.Right;
            int bottom = ui.Bottom;

            int leftEnd = left + Info.Left;
            int rightStart = right - Info.Right;
            int topEnd = top + Info.Top;
            int bottomStart = bottom - Info.Bottom;

            int modifyX = Info.CenterModify.X;
            int modifyY = Info.CenterModify.Y;
            Destination =
            [
                new(left, top, Info.Left, Info.Top),
                new(right - Info.Right, top, Info.Right, Info.Top),
                new(left, bottom - Info.Bottom, Info.Left, Info.Bottom),
                new(right - Info.Right, bottom - Info.Bottom, Info.Right, Info.Bottom),
                new(leftEnd, top, stretchWidth, Info.Top),
                new(left, topEnd, Info.Left, stretchHeight),
                new(rightStart, topEnd, Info.Right, stretchHeight),
                new(leftEnd, bottomStart, stretchWidth, Info.Bottom),
                ui.Modified(modifyX, modifyY, -modifyX * 2, -modifyY * 2)
            ];
        }

        protected virtual void DrawAllSlices(SpriteBatch spb, Point offset, Color color, bool ignoreCenter, bool applyModify)
        {
            var tex = Tex.Value;

            for (int i = 8; i >= 0; i--)
            {
                if (ignoreCenter && i == 8)
                    continue;

                var source = Source[i];
                var dest = Destination[i];
                Color c = color;
                if (applyModify)
                    ModifyDrawSlice(i, ref offset, ref dest, ref source, ref c);
                if (dest.Width == 0 || dest.Height == 0)
                    continue;
                source.Offset(offset);
                spb.Draw(tex, dest, source, c);
                //DrawHelper.DrawRectangle(spb, dest, 1, color);
            }
        }
        protected virtual void ModifyDrawSlice(int index, ref Point offset, ref Rectangle destination, ref Rectangle source, ref Color color)
        {

        }

        // 平铺绘制中心区域
        protected virtual void DrawTiledCenter(SpriteBatch spb, Rectangle dest, Rectangle source, Color color, Point offset = default)
        {
            var tex = Tex.Value;

            // 应用偏移
            if (offset != Point.Zero)
            {
                dest = new Rectangle(dest.X + offset.X, dest.Y + offset.Y, dest.Width, dest.Height);
            }

            // 如果目标区域小于源区域，直接绘制
            if (dest.Width <= source.Width && dest.Height <= source.Height)
            {
                spb.Draw(tex, dest, source, color);
                return;
            }

            // 计算需要平铺的次数
            int sourceWidth = source.Width;
            int sourceHeight = source.Height;

            int tilesX = (int)Math.Ceiling((float)dest.Width / sourceWidth);
            int tilesY = (int)Math.Ceiling((float)dest.Height / sourceHeight);

            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                int currentY = dest.Y + tileY * sourceHeight;
                int drawHeight = Math.Min(sourceHeight, dest.Bottom - currentY);

                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int currentX = dest.X + tileX * sourceWidth;
                    int drawWidth = Math.Min(sourceWidth, dest.Right - currentX);

                    // 创建目标矩形
                    Rectangle tileDest = new(currentX, currentY, drawWidth, drawHeight);

                    // 如果边缘不完整，需要裁剪源矩形
                    Rectangle tileSource = new(
                        source.X,
                        source.Y,
                        drawWidth,
                        drawHeight
                    );

                    spb.Draw(tex, tileDest, tileSource, color);
                }
            }
        }
    }
}