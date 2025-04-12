﻿namespace SilkyUIFramework.BasicElements;

public partial class UIView
{
    public bool LeftMousePressed { get; set; }
    public bool RightMousePressed { get; set; }
    public bool MiddleMousePressed { get; set; }
    public bool IsFocus { get; protected set; }

    public delegate void MouseEventHandler(UIMouseEvent evt, UIView listeningElement);
    public delegate void MouseWheelEventHandler(UIScrollWheelEvent evt, UIView listeningElement);
    public delegate void SelectionEventHandler(UIView listeningElement);

    // 鼠标事件
    public event MouseEventHandler LeftMouseDown;
    public event MouseEventHandler LeftMouseUp;
    public event MouseEventHandler LeftMouseClick;

    public event MouseEventHandler RightMouseDown;
    public event MouseEventHandler RightMouseUp;
    public event MouseEventHandler RightMouseClick;

    public event MouseEventHandler MiddleMouseDown;
    public event MouseEventHandler MiddleMouseUp;
    public event MouseEventHandler MiddleMouseClick;

    // 焦点事件
    public event MouseEventHandler GotFocus;
    public event MouseEventHandler LostFocus;

    // 悬浮事件
    public event MouseEventHandler MouseEnter;
    public event MouseEventHandler MouseLeave;
    public event MouseEventHandler MouseMove;

    public event MouseWheelEventHandler MouseWheel;

    // 选中事件
    public event SelectionEventHandler Selected;
    public event SelectionEventHandler Deselected;

    public bool IsSelected { get; protected set; }

    public bool Selectable { get; set; } = true;

    public void HandleSelected()
    {
        IsSelected = true;
        Selected?.Invoke(this);
        OnSelected();
    }

    public void HandleDeselected()
    {
        IsSelected = false;
        OnDeselected();
        Deselected?.Invoke(this);
    }

    public virtual void OnSelected() { }

    public virtual void OnDeselected() { }

    public virtual void OnLeftMouseDown(UIMouseEvent evt)
    {
        LeftMousePressed = true;
        LeftMouseDown?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnLeftMouseDown(evt);
    }

    public virtual void OnLeftMouseUp(UIMouseEvent evt)
    {
        LeftMousePressed = false;
        LeftMouseUp?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnLeftMouseUp(evt);
    }

    public virtual void OnLeftMouseClick(UIMouseEvent evt)
    {
        LeftMouseClick?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnLeftMouseClick(evt);
    }

    public virtual void OnRightMouseDown(UIMouseEvent evt)
    {
        RightMousePressed = true;
        RightMouseDown?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnRightMouseDown(evt);
    }

    public virtual void OnRightMouseUp(UIMouseEvent evt)
    {
        RightMousePressed = false;
        RightMouseUp?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnRightMouseUp(evt);
    }

    public virtual void OnRightMouseClick(UIMouseEvent evt)
    {
        RightMouseClick?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnRightMouseClick(evt);
    }

    public virtual void OnMiddleMouseDown(UIMouseEvent evt)
    {
        MiddleMousePressed = true;
        MiddleMouseDown?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMiddleMouseDown(evt);
    }

    public virtual void OnMiddleMouseUp(UIMouseEvent evt)
    {
        MiddleMousePressed = false;
        MiddleMouseUp?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMiddleMouseUp(evt);
    }

    public virtual void OnMiddleMouseClick(UIMouseEvent evt)
    {
        MiddleMouseClick?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMiddleMouseClick(evt);
    }

    // 焦点事件方法
    public virtual void OnGotFocus(UIMouseEvent evt)
    {
        IsFocus = true;
        GotFocus?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnGotFocus(evt);
    }

    public virtual void OnLostFocus(UIMouseEvent evt)
    {
        IsFocus = false;
        LostFocus?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnLostFocus(evt);
    }

    // 悬浮事件方法
    public virtual void OnMouseEnter(UIMouseEvent evt)
    {
        IsMouseHovering = true;
        MouseEnter?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMouseEnter(evt);
    }

    public virtual void OnMouseLeave(UIMouseEvent evt)
    {
        IsMouseHovering = false;
        MouseLeave?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMouseLeave(evt);
    }

    public virtual void OnMouseMove(UIMouseEvent evt)
    {
        MouseMove?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMouseMove(evt);
    }

    public virtual void OnMouseWheel(UIScrollWheelEvent evt)
    {
        MouseWheel?.Invoke(evt, this);
        evt.Previous = this;
        Parent?.OnMouseWheel(evt);
    }

    public virtual Vector2 InputMethodPosition => InnerBounds.LeftBottom + new Vector2(0f, 32f);

    public virtual bool OccupyPlayerInput { get; set; }

    public virtual void HandlePlayerInput(bool inputMethodStatus) { }
}