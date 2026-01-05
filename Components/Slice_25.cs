using ReLogic.Content;

namespace SilkyUIFramework.Components
{
    public class Slice_25 : UIElement
    {
        public struct Slice_25_Info(int top, int left, int right, int bottom,
            int edgeCenterWidth, int edgeCenterHeight, int edgeStripWidth, int edgeStripHeight,
            Rectangle center, Rectangle bound, int space = 2, Point? centerModify = null)
        {
            public int Top = top;
            public int Left = left;
            public int Right = right;
            public int Bottom = bottom;

            /// <summary>上下中间条的宽度</summary>
            public int EdgeCenterWidth = edgeCenterWidth;
            /// <summary>左右中间条的高度</summary>
            public int EdgeCenterHeight = edgeCenterHeight;
            /// <summary>边条的宽度</summary>
            public int EdgeStripWidth = edgeStripWidth;
            /// <summary>边条的高度</summary>
            public int EdgeStripHeight = edgeStripHeight;
            /// <summary>中心块定界矩形</summary>
            public Rectangle Center = center;
            /// <summary>单帧定界框</summary>
            public Rectangle Bound = bound;
            /// <summary>切片纹理间隔</summary>
            public int Space = space;
            public Point CenterModify = centerModify ?? Point.Zero;
        }

        public Rectangle[] Source, Destination;
        public Asset<Texture2D> Tex;
        public Slice_25_Info Info;

        public Slice_25(Slice_25_Info info)
        {
            Info = info;
            Transfer(info);
        }

        private void Transfer(Slice_25_Info info)
        {
            var (top, left, right, bottom, space) = (info.Top, info.Left, info.Right, info.Bottom, info.Space);
            var (edgeStripW, edgeStripH, edgeCenterW, edgeCenterH) =
                (info.EdgeStripWidth, info.EdgeStripHeight, info.EdgeCenterWidth, info.EdgeCenterHeight);
            var bound = info.Bound;
            var (width, height) = (bound.Width, bound.Height);

            int rightX = width - right;
            int bottomY = height - bottom;

            // 计算关键位置
            int leftStripStart = left + space;                           // 上左条开始（+间隔）
            int topCenterStart = leftStripStart + edgeStripW + space;    // 上中点开始（+间隔）
            int rightStripStart = topCenterStart + edgeCenterW + space;  // 上右条开始（+间隔）

            int topStripStart = top + space;                             // 左上条开始（+间隔）
            int sideCenterStart = topStripStart + edgeStripH + space;    // 左中点开始（+间隔）
            int bottomStripStart = sideCenterStart + edgeCenterH + space;// 左中下条开始（+间隔）

            Source =
            [
                // 0-3: 4个角（固定）- 没有间隔
                new(0, 0, left, top),                              // 0: 左上角
                new(rightX, 0, right, top),                        // 1: 右上角
                new(0, bottomY, left, bottom),                     // 2: 左下角
                new(rightX, bottomY, right, bottom),               // 3: 右下角
    
                // 4-7: 4个边中点（固定）- 起点考虑间隔
                new(topCenterStart, 0, edgeCenterW, top),          // 4: 上中点
                new(topCenterStart, bottomY, edgeCenterW, bottom), // 5: 下中点
                new(0, sideCenterStart, left, edgeCenterH),        // 6: 左中点
                new(rightX, sideCenterStart, right, edgeCenterH),  // 7: 右中点
    
                // 8-11: 水平边条（拉伸）- 起点考虑间隔
                new(leftStripStart, 0, edgeStripW, top),           // 8: 上左条
                new(rightStripStart, 0, edgeStripW, top),          // 9: 上右条
                new(leftStripStart, bottomY, edgeStripW, bottom),  // 10: 下左条
                new(rightStripStart, bottomY, edgeStripW, bottom), // 11: 下右条
    
                // 12-15: 垂直边条（拉伸）- 起点考虑间隔
                new(0, topStripStart, left, edgeStripH),           // 12: 左中上条
                new(0, bottomStripStart, left, edgeStripH),        // 13: 左中下条
                new(rightX, topStripStart, right, edgeStripH),     // 14: 右中上条
                new(rightX, bottomStripStart, right, edgeStripH),  // 15: 右中下条
    
                info.Center                                        // 16: 中心块（已手动指定位置）
            ];

            // 应用偏移
            var (offsetX, offsetY) = (bound.X, bound.Y);
            for (int i = 0; i < 16; i++)
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

            // 计算拉伸尺寸
            int stretchWidth = Math.Max(0, ui.Width - Info.Left - Info.Right - Info.EdgeCenterWidth);
            int stretchHeight = Math.Max(0, ui.Height - Info.Top - Info.Bottom - Info.EdgeCenterHeight);

            int stretchedEdgeStripWidth = stretchWidth / 2;    // 每个横向边条的拉伸宽度
            int stretchedSideStripHeight = stretchHeight / 2;  // 每个纵向边条的拉伸高度

            // 关键坐标
            int left = ui.Left;
            int top = ui.Top;
            int right = ui.Right;
            int bottom = ui.Bottom;
            int edgeMidX = left + Info.Left + stretchedEdgeStripWidth;      // 上中点X
            int sideMidY = top + Info.Top + stretchedSideStripHeight;       // 左中点Y

            Destination =
            [
                // 0-3: 4个角（固定）
                new(left, top, Info.Left, Info.Top),                          // 左上角
                new(right - Info.Right, top, Info.Right, Info.Top),          // 右上角
                new(left, bottom - Info.Bottom, Info.Left, Info.Bottom),     // 左下角
                new(right - Info.Right, bottom - Info.Bottom, Info.Right, Info.Bottom), // 右下角
    
                // 4-7: 4个边中点（固定）
                new(edgeMidX, top, Info.EdgeCenterWidth, Info.Top),           // 上中点
                new(edgeMidX, bottom - Info.Bottom, Info.EdgeCenterWidth, Info.Bottom), // 下中点
                new(left, sideMidY, Info.Left, Info.EdgeCenterHeight),        // 左中点
                new(right - Info.Right, sideMidY, Info.Right, Info.EdgeCenterHeight), // 右中点
    
                // 8-11: 水平边条（拉伸）
                new(left + Info.Left, top, stretchedEdgeStripWidth, Info.Top), // 上左条
                new(edgeMidX + Info.EdgeCenterWidth, top, stretchedEdgeStripWidth, Info.Top), // 上右条
                new(left + Info.Left, bottom - Info.Bottom, stretchedEdgeStripWidth, Info.Bottom), // 下左条
                new(edgeMidX + Info.EdgeCenterWidth, bottom - Info.Bottom, stretchedEdgeStripWidth, Info.Bottom), // 下右条
    
                // 12-15: 垂直边条（拉伸）
                new(left, top + Info.Top, Info.Left, stretchedSideStripHeight), // 左中上条
                new(left, sideMidY + Info.EdgeCenterHeight, Info.Left, stretchedSideStripHeight), // 左中下条
                new(right - Info.Right, top + Info.Top, Info.Right, stretchedSideStripHeight), // 右中上条
                new(right - Info.Right, sideMidY + Info.EdgeCenterHeight, Info.Right, stretchedSideStripHeight), // 右中下条
    
                // 16: 中心块（平铺）
                ui.Modified(Info.CenterModify.X, Info.CenterModify.Y, -Info.CenterModify.X * 2, -Info.CenterModify.Y * 2)
            ];
            for (int i = 8; i < 12; i++)
                Destination[i] = Destination[i].Modified(-1, 0, 2, 0);
            for (int i = 12; i < 16; i++)
                Destination[i] = Destination[i].Modified(0, -1, 0, 2);
        }

        protected virtual void DrawAllSlices(SpriteBatch spb, Point offset, Color color, bool ignoreCenter, bool applyModify)
        {
            var tex = Tex.Value;

            for (int i = 16; i >= 0; i--)
            {
                if (ignoreCenter && i == 16)
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
                /*if (i != 16 && i > 3)
                    DrawHelper.DrawRectangle(spb, dest, 1, Color.White);*/
            }
        }
        protected virtual void ModifyDrawSlice(int index, ref Point offset, ref Rectangle destination, ref Rectangle source, ref Color color)
        {

        }

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