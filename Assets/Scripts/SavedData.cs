using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SavedData
{
    public Vector3 playerPosition;
    public string mapBoundary;
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public List<ChestSaveData> chestSaveData;
}

[System.Serializable]

public class ChestSaveData
{
    public string chestID;
    public bool isOpened;
}