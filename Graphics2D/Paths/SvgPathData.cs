using System.Globalization;

namespace SilkyUIFramework.Graphics2D.Paths;

internal sealed class SvgPathData(string data, PathFillRule fillRule)
{
    private readonly string _data = data;

    private readonly VectorPathBuilder _path = new() { FillRule = fillRule };

    private int _position;

    private char _previous;

    private Vector2 _control;

    public VectorPath Parse()
    {
        SkipWhitespace();
        if (_position < _data.Length && _data[_position] is not ('M' or 'm'))
            throw Invalid("路径数据必须以 M 或 m 开始");

        char command = '\0';
        while (_position < _data.Length)
        {
            var explicitCommand = char.IsAsciiLetter(_data[_position]);
            if (explicitCommand)
                command = _data[_position++];
            var kind = char.ToUpperInvariant(command);

            var origin = char.IsAsciiLetterLower(command) ? _path.CurrentPoint : Vector2.Zero;

            var allowComma = !explicitCommand;

            var current = _path.CurrentPoint;
            switch (kind)
            {
                case 'M':
                    _path.MoveTo(ReadPoint(origin, allowComma));
                    command = command == 'M' ? 'L' : 'l';
                    break;
                case 'L':
                    _path.LineTo(ReadPoint(origin, allowComma));
                    break;
                case 'H':
                    _path.LineTo(new Vector2(origin.X + ReadNumber(allowComma), current.Y));
                    break;
                case 'V':
                    _path.LineTo(new Vector2(current.X, origin.Y + ReadNumber(allowComma)));
                    break;
                case 'C':
                {
                    var first = ReadPoint(origin, allowComma);
                    _control = ReadPoint(origin);
                    _path.CubicTo(first, _control, ReadPoint(origin));
                    break;
                }
                case 'S':
                {
                    var first = _previous is 'C' or 'S' ? Reflect(current, _control) : current;
                    _control = ReadPoint(origin, allowComma);
                    _path.CubicTo(first, _control, ReadPoint(origin));
                    break;
                }
                case 'Q':
                    _control = ReadPoint(origin, allowComma);
                    _path.QuadTo(_control, ReadPoint(origin));
                    break;
                case 'T':
                    _control = _previous is 'Q' or 'T' ? Reflect(current, _control) : current;
                    _path.QuadTo(_control, ReadPoint(origin, allowComma));
                    break;
                case 'A':
                {
                    var radii = ReadPoint(Vector2.Zero, allowComma);

                    var rotation = ReadNumber() % 360f * (MathF.PI / 180f);

                    var large = ReadFlag();

                    var clockwise = ReadFlag();
                    _path.ArcTo(radii, rotation, large, clockwise, ReadPoint(origin));
                    break;
                }
                case 'Z':
                    _path.Close();
                    command = '\0';
                    break;
                default:
                    throw Invalid("缺少有效的路径命令");
            }
            _previous = kind;
            SkipWhitespace();
        }
        return _path.Build();
    }

    private static Vector2 Reflect(Vector2 point, Vector2 control) => new(
        (float)(2d * point.X - control.X), (float)(2d * point.Y - control.Y));

    private Vector2 ReadPoint(Vector2 origin, bool allowComma = true) =>
        new(origin.X + ReadNumber(allowComma), origin.Y + ReadNumber());

    private bool ReadFlag()
    {
        SkipSeparator(true);
        if (_position == _data.Length || _data[_position] is not ('0' or '1'))
            throw Invalid("弧标志必须为 0 或 1");
        return _data[_position++] == '1';
    }

    private float ReadNumber(bool allowComma = true)
    {
        SkipSeparator(allowComma);
        var start = _position;
        ReadSign();
        var hasDigits = ReadDigits();
        if (_position < _data.Length && _data[_position] == '.')
        {
            _position++;
            hasDigits |= ReadDigits();
        }
        if (!hasDigits) throw Invalid("缺少数字");
        if (_position < _data.Length && _data[_position] is 'e' or 'E')
        {
            _position++;
            ReadSign();
            if (!ReadDigits()) throw Invalid("指数缺少数字");
        }
        if (!float.TryParse(_data.AsSpan(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture,
                out var value) || !float.IsFinite(value))
            throw Invalid("数字超出路径坐标范围");
        return value;
    }

    private void ReadSign()
    {
        if (_position < _data.Length && _data[_position] is '+' or '-') _position++;
    }

    private bool ReadDigits()
    {
        var start = _position;
        while (_position < _data.Length && char.IsAsciiDigit(_data[_position])) _position++;
        return _position != start;
    }

    private void SkipSeparator(bool allowComma)
    {
        SkipWhitespace();
        if (allowComma && _position < _data.Length && _data[_position] == ',')
        {
            _position++;
            SkipWhitespace();
        }
    }

    private void SkipWhitespace()
    {
        while (_position < _data.Length && _data[_position] is ' ' or '\t' or '\n' or '\r') _position++;
    }

    private FormatException Invalid(string message) => new($"SVG 路径数据第 {_position + 1} 个字符：{message}。");
}
