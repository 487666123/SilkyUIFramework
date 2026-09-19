namespace SilkyUIFramework.Interfaces;

public interface IContainer<in T>
{
    void Add(T item);
    bool Remove(T item);
}
