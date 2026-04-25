using UnityEngine;

[CreateAssetMenu(fileName = "New TreeData", menuName = "Trees/Tree Data")]
public class TreeData : ScriptableObject
{
    [Header("Identity")]
    public int treeDataID;
    public string displayName;

    [Header("Chopping")]
    public bool isChoppable = true;
    public int chopCount = 3;
    public int woodDropMin = 2;
    public int woodDropMax = 4;
    public Item woodItem;

    [Header("Fruit (leave fruitItem null for wood-only trees)")]
    public Item fruitItem;
    public int ripenHours = 48;
    public int fruitPerHarvest = 1;

    [Header("Regrowth")]
    public int regrowHours = 72;
    public bool regrows = true;

    [Header("Planting")]
    public bool isPlantable = false;
    public int growHours = 24;

    [Header("Sprites")]
    public Sprite saplingSprite;
    public Sprite matureSprite;
    public Sprite fruitOverlay;

    [Header("Prefab")]
    public GameObject treePrefab;

}
