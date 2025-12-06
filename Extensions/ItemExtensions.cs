namespace SilkyUIFramework.Extensions;

public static class ItemExtensions
{
    extension(Item item)
    {
        public bool NotAir() => !item.IsAir;
        public bool Full() => item.stack >= item.maxStack;
        public bool NotFull() => item.stack < item.maxStack;
    }
}