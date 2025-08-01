﻿namespace SilkyUIFramework;

public partial class SilkyUIManager
{
    public SilkyUIGroup GlobalSilkyUIGroup { get; private set; }

    public List<Type> GlobalBodyTypes { get; } = [];

    /// <summary> 注册全局 UI </summary>
    public void RegisterGlobalUI(Type bodyType)
    {
        Logger.Info($"Register Global UI: \"{bodyType.Name}\"");

        GlobalBodyTypes.Add(bodyType);
    }

    public void InitializeGlobalUI()
    {
        GlobalSilkyUIGroup = ServiceProvider.GetService<SilkyUIGroup>();

        foreach (var type in GlobalBodyTypes)
        {
            var silkyUI = ServiceProvider.GetRequiredService<SilkyUI>();
            var body = ServiceProvider.GetRequiredService(type) as BasicBody;

            silkyUI.Priority = type.GetCustomAttribute<RegisterGlobalUIAttribute>().Priority;
            silkyUI.SetBody(body);

            GlobalSilkyUIGroup.Add(silkyUI);
        }
    }

    public void UpdateGlobalUI(GameTime gameTime)
    {
        MouseHoverGroup = null;
        MouseFocusGroup = null;

        CurrentSilkyUIGroup = GlobalSilkyUIGroup;
        CurrentSilkyUIGroup?.Order();
        CurrentSilkyUIGroup?.UpdateUI(gameTime);

        CurrentSilkyUIGroup = null;
    }

    public void DrawGlobalUI(GameTime gameTime)
    {
        GlobalSilkyUIGroup?.Order();
        GlobalSilkyUIGroup?.Draw(gameTime);
    }
}
