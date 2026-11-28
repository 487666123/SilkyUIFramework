using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.UserInterfaces;

public sealed partial class VectorPathDemoUI
{
    private sealed class IconPart
    {
        private readonly SUISvgIcon _view;
        private readonly VectorPathDemoIcons.Part _part;
        private readonly Vector2 _size;
        private readonly Matrix _transform;
        private readonly double _phase;
        private readonly PathMesh _restMesh;
        private PathMesh _endMesh;
        private float _progress;

        public Point Origin { get; }
        public PathMesh Mesh { get; private set; }

        public IconPart(SUISvgIcon view, VectorPathDemoIcons.Part part, Vector2 size, Matrix transform, Point origin, double phase)
        {
            _view = view;
            _part = part;
            _size = size;
            _transform = transform;
            _phase = phase;
            Origin = origin;
            Mesh = _restMesh = PrepareMesh(0f);
        }

        public bool Update(double seconds)
        {
            var motion = _part.Motion;
            if (!motion.IsMoving) return false;
            var progress = MotionProgress(seconds + _phase - motion.Delay, motion.Repeat);
            if (motion.Repeat && progress == 1f) progress = 0f;
            if (progress == _progress) return false;

            // 姿势先作用于路径，再按当前设备像素重新计算覆盖率。
            Mesh = progress switch
            {
                0f => _restMesh,
                1f => _endMesh ??= PrepareMesh(progress),
                _ => PrepareMesh(progress),
            };
            _progress = progress;
            return true;
        }

        private PathMesh PrepareMesh(float progress)
        {
            var motion = _part.Motion;
            var deformation = Matrix.CreateScale(1f + motion.ScaleDelta.X * progress, 1f + motion.ScaleDelta.Y * progress, 1f);
            if (motion.PageFlip)
            {
                // 书脊固定。纸页转到侧面时投影收窄，翻过书脊后宽度变为负数。
                var angle = MathHelper.Pi * progress;
                deformation.M11 = MathF.Cos(angle);
                deformation.M12 = -0.28f * MathF.Sin(angle);
                if (MathF.Abs(deformation.M11) < 0.01f) return new PathMesh([], []);
            }
            var pose = progress == 0f ? Matrix.Identity :
                Matrix.CreateTranslation(-motion.Pivot.X, -motion.Pivot.Y, 0f) * deformation *
                Matrix.CreateRotationZ(motion.Angle * progress) *
                Matrix.CreateTranslation(motion.Pivot.X + motion.Shift.X * progress, motion.Pivot.Y + motion.Shift.Y * progress, 0f);
            return _view.PrepareLayerRange(_part.FirstLayer, _part.LayerCount, _size, _transform, pose);
        }
    }

    private static float MotionProgress(double seconds, bool repeat)
    {
        const double duration = 0.46d;
        const double returnStart = 1.06d;
        const double period = 2.45d;
        var time = seconds % period;
        if (time < 0d) time += period;
        if (time < duration) return EaseMotion((float)(time / duration));
        if (repeat || time < returnStart) return 1f;
        return time < returnStart + duration ? 1f - EaseMotion((float)((time - returnStart) / duration)) : 0f;
    }

    private static float EaseMotion(float progress)
    {
        if (progress is <= 0f or >= 1f) return progress;
        var left = 0f;
        var right = 1f;
        for (var i = 0; i < 12; i++)
        {
            var t = (left + right) * 0.5f;
            var remaining = 1f - t;
            var x = 3f * remaining * t * (0.22f * remaining + 0.36f * t) + t * t * t;
            if (x < progress) left = t;
            else right = t;
        }
        var u = 1f - (left + right) * 0.5f;
        return 1f - u * u * u;
    }

    private sealed class GalleryIconBatch(List<IconPart> parts)
    {
        private VertexPositionColor[] _vertices = [];
        private int[] _indices = [];
        private PathMesh _mesh;

        public PathMesh Update(double seconds)
        {
            var changed = _mesh is null;
            // 所有图层都要推进动画，即使前面的图层已经触发网格更新。
            foreach (var part in parts)
                changed |= part.Update(seconds);

            if (changed) _mesh = AssembleMesh();
            return _mesh;
        }

        private PathMesh AssembleMesh()
        {
            // 动画网格的顶点数会变化。保留缓冲容量，并按原始图层顺序合批。
            var vertexCount = 0;
            var indexCount = 0;
            foreach (var part in parts)
            {
                vertexCount += part.Mesh.Vertices.Length;
                indexCount += part.Mesh.Indices.Length;
            }
            if (_vertices.Length < vertexCount) Array.Resize(ref _vertices, Math.Max(vertexCount, _vertices.Length * 2));
            if (_indices.Length < indexCount) Array.Resize(ref _indices, Math.Max(indexCount, _indices.Length * 2));

            var vertexStart = 0;
            var indexStart = 0;
            foreach (var part in parts)
            {
                var offset = new Vector3(part.Origin.X, part.Origin.Y, 0f);
                foreach (var index in part.Mesh.Indices) _indices[indexStart++] = vertexStart + index;
                foreach (var vertex in part.Mesh.Vertices)
                    _vertices[vertexStart++] = new VertexPositionColor(vertex.Position + offset, vertex.Color);
            }
            return new PathMesh(_vertices, vertexCount, _indices, indexCount, Matrix.Identity);
        }
    }
}
