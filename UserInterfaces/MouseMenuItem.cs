﻿namespace SilkyUIFramework.UserInterfaces;

public class MouseMenuItem : UIElementGroup
{
    public UITextView TextView { get; }

    public string Content { get; }
    public int MenuIndex { get; }

    public MouseMenuCallback MouseMenuCallback { get; set; }

    public MouseMenuItem(string content, int menuIndex)
    {
        Content = content; MenuIndex = menuIndex;

        FitWidth = true; FitHeight = true;

        Padding = new Margin(8f, 4f, 25f, 4f);
        BorderRadius = new Vector4(4f);

        TextView = new UITextView()
        {
            MinWidth = new Dimension(125f),
            Text = content,
            TextScale = 0.8f,
        }.Join(this);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if (MouseMenuCallback?.Invoke(Content, MenuIndex) ?? true)
        {
            //if (GetAncestor() is MouseMenuUI menuUI)
            //{
            //    menuUI.Enabled = false;
            //}
        }

        base.OnLeftMouseDown(evt);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        BackgroundColor = Color.Black * HoverTimer.Lerp(0.25f, 0.4f);
        base.UpdateStatus(gameTime);
    }
}