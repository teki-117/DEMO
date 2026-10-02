using System;
using System.Collections.Generic;

[Serializable]
public class ItemStack
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class InventoryState
{
    public List<ItemStack> items = new List<ItemStack>();
}