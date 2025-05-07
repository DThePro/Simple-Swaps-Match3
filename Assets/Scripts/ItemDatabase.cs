using UnityEngine;

public static class ItemDatabase
{
    public static Item[] Items { get; private set; }
    public static Item[] SpecialItems { get; private set; }
    public static Item[] EnemyItems { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Items = Resources.LoadAll<Item>("Items/");
        SpecialItems = Resources.LoadAll<Item>("Special/");
        EnemyItems = Resources.LoadAll<Item>("Enemies/");
    }
}
