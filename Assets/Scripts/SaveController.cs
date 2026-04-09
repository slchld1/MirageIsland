using Cinemachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private Inventory inventoryController;
    private HotbarController hotbarController;
    private Chest[] chests;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeComponents();
        LoadGame();
    }

    private void InitializeComponents()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "savedData.json");
        inventoryController = FindAnyObjectByType<Inventory>();
        hotbarController = FindAnyObjectByType<HotbarController>();
        chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);

    }

    public void SaveGame()
    {
        SavedData saveData = new SavedData
        {
            playerPosition = GameObject.FindWithTag("Player").transform.position,
            mapBoundary = FindAnyObjectByType<CinemachineConfiner>().m_BoundingShape2D.gameObject.name,
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotbarSaveData = hotbarController.GetHotbarItems(),
            chestSaveData = GetChestState(),

        };
        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        Debug.Log("Game Saved." + saveLocation);
    }
    private List<ChestSaveData> GetChestState()
    {
        List<ChestSaveData> chestStates = new List<ChestSaveData>(); 

        foreach(Chest chest in chests)
        {
            ChestSaveData chestSaveData = new ChestSaveData
            {
                chestID = chest.ChestID,
                isOpened = chest.IsOpened
            };
            chestStates.Add(chestSaveData);
        }

        return chestStates;
    }
    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {

            SavedData saveData = JsonUtility.FromJson<SavedData>(File.ReadAllText(saveLocation));

            GameObject.FindWithTag("Player").transform.position = saveData.playerPosition;

            PolygonCollider2D savedMapBoundary = GameObject.Find(saveData.mapBoundary).GetComponent<PolygonCollider2D>();
            FindAnyObjectByType<CinemachineConfiner>().m_BoundingShape2D = savedMapBoundary;

            MapController_Manual.Instance?.HighLightArea(saveData.mapBoundary);

            MapController_Dynamic.Instance?.GenerateMap(savedMapBoundary);

            inventoryController.SetInventoryItems(saveData.inventorySaveData);

            hotbarController.SetHotbarItems(saveData.hotbarSaveData);

            LoadChestState(saveData.chestSaveData);

        }
        else
        {
            SaveGame();

            inventoryController.SetInventoryItems(new List<InventorySaveData>());

            hotbarController.SetHotbarItems(new List<InventorySaveData>());


            MapController_Dynamic.Instance?.GenerateMap();
        }
    }

    private void LoadChestState(List<ChestSaveData> chestStates)
    {
        foreach(Chest chest in chests)
        {
            ChestSaveData chestSaveData = chestStates.FirstOrDefault(c => c.chestID == chest.ChestID);

            if (chestSaveData != null)
            {
                chest.SetOpened(chestSaveData.isOpened);
            }
        }
    }
}
