namespace Games.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string ItemName { get; set; }
    public int SlotIndex { get; set; }  // 在物品栏中的位置索引
    public bool IsCollected { get; set; } // 是否已被收集
    public bool IsUsed { get; set; } // 是否已被使用

    public static implicit operator InventoryItem(int v)
    {
        throw new NotImplementedException();
    }
} 