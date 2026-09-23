namespace SilkyUIFramework.Extensions.Tweening;

/// <summary>
/// Tween 扩展层的默认插值函数注册中心。
/// 按成员值类型保存和查询强类型插值委托。
/// </summary>
public class TweenLerpRegistry : ILoadable
{
    #region Implement ILoadable

    public void Load(Mod mod)
    {
        Register<float>(static (value1, value2, amount) => value1 + (value2 - value1) * amount);
        Register<double>(static (value1, value2, amount) => value1 + (value2 - value1) * amount);
        Register<Vector2>(Vector2.Lerp);
        Register<Vector3>(Vector3.Lerp);
        Register<Vector4>(Vector4.Lerp);
        Register<Color>(Color.Lerp);
        Register<Matrix>(Matrix.Lerp);
        Register<Anchor>(Anchor.Lerp);
        Register<Dimension>(Dimension.Lerp);
        Register<Margin>(Margin.Lerp);
    }

    public void Unload() => _lerpFuncs.Clear();

    #endregion

    // 保留原始强类型委托，创建补间时按 T 取回，不生成 object 插值包装。
    private static readonly Dictionary<Type, Delegate> _lerpFuncs = [];

    /// <summary>
    /// 注册或覆盖指定类型的插值函数。
    /// </summary>
    public static void Register<T>(Func<T, T, float, T> lerpFunc)
    {
        ArgumentNullException.ThrowIfNull(lerpFunc);
        _lerpFuncs[typeof(T)] = lerpFunc;
    }

    /// <summary>
    /// 尝试获取指定类型的插值函数。
    /// </summary>
    public static bool TryGet<T>(out Func<T, T, float, T> lerpFunc)
    {
        if (_lerpFuncs.TryGetValue(typeof(T), out var value) &&
            value is Func<T, T, float, T> typedLerpFunc)
        {
            lerpFunc = typedLerpFunc;
            return true;
        }

        lerpFunc = null;
        return false;
    }

    /// <summary>
    /// 获取指定类型的插值函数；未注册时抛出清晰异常。
    /// </summary>
    public static Func<T, T, float, T> Get<T>()
    {
        if (TryGet<T>(out var lerpFunc)) return lerpFunc;
        throw CreateMissingLerpException(typeof(T));
    }

    private static InvalidOperationException CreateMissingLerpException(Type type)
    {
        return new InvalidOperationException(
            $"No lerp function registered for type '{type.FullName}'. " +
            $"Register one with {nameof(TweenLerpRegistry)}.{nameof(Register)}<T>() " +
            "or use the MemberTo<TTarget, TValue>() overload with an explicit lerp function.");
    }
}
