﻿namespace SilkyUIFramework.Animation;

#region Enums

/// <summary>
/// 动画当前的状态
/// </summary>
public enum AnimationTimerStatus
{
    Updating,
    ReverseUpdating,
    Completed,
    ReverseCompleted
}

#endregion

/// <summary>
/// 动画计时器 <br/>
/// </summary>
public class AnimationTimer(float speed = 5f, float timerMax = 100f)
{
    /// <summary>
    /// 速度
    /// </summary>
    public float Speed = speed;

    /// <summary>
    /// 当前位置
    /// </summary>
    public float Timer;

    /// <summary>
    /// 最大位置
    /// </summary>
    public float TimerMax = timerMax;

    /// <summary>
    /// 进度
    /// </summary>
    public float Schedule { get; private set; }

    public AnimationTimerStatus Status = AnimationTimerStatus.ReverseCompleted;

    /// <summary>
    /// 在正向更新完成时回调
    /// </summary>
    public event Action OnUpdateCompleted;

    /// <summary>
    /// 在反向更新完成时回调
    /// </summary>
    public event Action OnReverseUpdateCompleted;

    #region IsComplete Updating

    public bool IsCompleted => Status is AnimationTimerStatus.Completed or AnimationTimerStatus.ReverseCompleted;
    public bool IsUpdating => Status is AnimationTimerStatus.Updating or AnimationTimerStatus.ReverseUpdating;

    public bool IsForward =>
        Status is AnimationTimerStatus.Updating or AnimationTimerStatus.Completed;

    public bool IsForwardUpdating => Status is AnimationTimerStatus.Updating;
    public bool IsForwardCompleted => Status is AnimationTimerStatus.Completed;

    public bool IsReverse =>
        Status is AnimationTimerStatus.ReverseUpdating or AnimationTimerStatus.ReverseCompleted;

    public bool IsReverseUpdating => Status is AnimationTimerStatus.ReverseUpdating;
    public bool IsReverseCompleted => Status is AnimationTimerStatus.ReverseCompleted;

    #endregion

    #region Member Methods

    /// <summary>
    /// 开始正向更新
    /// </summary>
    public virtual void StartUpdate(bool reset = false)
    {
        if (reset)
        {
            Timer = 0f;
            Status = AnimationTimerStatus.Updating;
            return;
        }

        if (Status is AnimationTimerStatus.Completed) return;
        Status = AnimationTimerStatus.Updating;
    }

    /// <summary>
    /// 开始反向更新
    /// </summary>
    public virtual void StartReverseUpdate(bool reset = false)
    {
        if (reset)
        {
            Timer = 0f;
            Status = AnimationTimerStatus.ReverseUpdating;
            return;
        }

        if (Status is AnimationTimerStatus.ReverseCompleted) return;
        Status = AnimationTimerStatus.ReverseUpdating;
    }

    /// <summary>
    /// 直接跳到完全关闭状态
    /// </summary>
    public void ImmediateReverseCompleted()
    {
        Timer = 0;
        Status = AnimationTimerStatus.ReverseCompleted;
        OnReverseUpdateCompleted?.Invoke();
    }

    /// <summary>
    /// 直接跳到完全开启状态
    /// </summary>
    public void ImmediateCompleted()
    {
        Timer = TimerMax;
        Status = AnimationTimerStatus.Completed;
        OnUpdateCompleted?.Invoke();
    }

    #endregion

    public void Update(GameTime gameTime)
    {
        var speedFactor = Main.FrameSkipMode == Terraria.Enums.FrameSkipMode.Subtle ? 1f :
            (float)gameTime.ElapsedGameTime.TotalSeconds * 60f;
        switch (Status)
        {
            case AnimationTimerStatus.Updating:
            {
                Timer += (TimerMax - Timer) / Speed * speedFactor;

                if (TimerMax - Timer < TimerMax * 0.0001f)
                {
                    Timer = TimerMax;
                    Status = AnimationTimerStatus.Completed;
                    OnUpdateCompleted?.Invoke();
                }

                break;
            }
            case AnimationTimerStatus.ReverseUpdating:
            {
                Timer -= Timer / Speed * speedFactor;

                if (Timer < TimerMax * 0.0001f)
                {
                    Timer = 0;
                    Status = AnimationTimerStatus.ReverseCompleted;
                    OnReverseUpdateCompleted?.Invoke();
                }

                break;
            }
        }
        Schedule = Timer / TimerMax;
    }

    #region Lerp Methods

    public Color Lerp(Color color1, Color color2)
    {
        return Color.Lerp(color1, color2, Schedule);
    }

    public Vector2 Lerp(Vector2 vector21, Vector2 vector22)
    {
        return Vector2.Lerp(vector21, vector22, Schedule);
    }

    public Vector3 Lerp(Vector3 vector31, Vector3 vector32)
    {
        return Vector3.Lerp(vector31, vector32, Schedule);
    }

    public Vector4 Lerp(Vector4 vector41, Vector4 vector42)
    {
        return Vector4.Lerp(vector41, vector42, Schedule);
    }

    public float Lerp(float value1, float value2)
    {
        return value1 + (value2 - value1) * Schedule;
    }

    #endregion

    #region operator

    public static Color operator *(AnimationTimer timer, Color color)
    {
        return color * timer.Schedule;
    }

    public static Color operator *(Color color, AnimationTimer timer)
    {
        return color * timer.Schedule;
    }

    public static Vector2 operator *(AnimationTimer timer, Vector2 vector2)
    {
        return vector2 * timer.Schedule;
    }

    public static Vector2 operator *(Vector2 vector2, AnimationTimer timer)
    {
        return vector2 * timer.Schedule;
    }

    public static float operator *(float number, AnimationTimer timer)
    {
        return number * timer.Schedule;
    }

    public static float operator *(AnimationTimer timer, float number)
    {
        return number * timer.Schedule;
    }

    #endregion
}