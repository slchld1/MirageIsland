using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapController_Dynamic : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform mapParent;
    public GameObject areaPrefab;
    public RectTransform playerIcon;

    [Header("Colours")]
    public Color defaultColour = Color.grey;
    public Color currentAreaColour = Color.green;

    [Header("Map Settings")]
    public GameObject mapBounds; //Parent of area Colliders
    public PolygonCollider2D initialArea; //Initial starting Area
    public float mapScale = 10f; //Adjust map size on UI

    private PolygonCollider2D[] mapAreas; //Children of Mapbounds
    private Dictionary<string, RectTransform> uiAreas = new Dictionary<string, RectTransform>(); //Map each PolygonCollider2d to corrisponding RectTransform

    public static MapController_Dynamic Instance { get; set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        mapAreas = mapBounds.GetComponentsInChildren<PolygonCollider2D>();
    }

    //Generate Maps
    public void GenerateMap(PolygonCollider2D newCurrentArea = null)
    {
        PolygonCollider2D currentArea = newCurrentArea != null ? newCurrentArea : initialArea;

        ClearMap();

        foreach(PolygonCollider2D area in mapAreas)
        {
            CreateAreaUI(area, area == currentArea);
        }

        MovePlayerIcon(currentArea.name);
    }

    //ClearMap
    private void ClearMap()
    {
        foreach(Transform child in mapParent)
        {
            Destroy(child.gameObject);
        }

        uiAreas.Clear();
    }

    private void CreateAreaUI(PolygonCollider2D area, bool isCurrent)
    {
        //Instantiate prefab for image
        GameObject areaImage = Instantiate(areaPrefab, mapParent);
        RectTransform rectTransform = areaImage.GetComponent<RectTransform>();
        
        //Get bounds
        Bounds bounds = area.bounds;

        //Scale UI image to fit map and bounds
        rectTransform.sizeDelta = new Vector2(bounds.size.x * mapScale, bounds.size.y * mapScale);
        rectTransform. anchoredPosition = bounds.center * mapScale;

        //Set colour based on current or not
        areaImage.GetComponent<Image>().color = isCurrent ? currentAreaColour : defaultColour;

        //Add to Dictionary
        uiAreas[area.name] = rectTransform;
    }

    //UpdatedCurrentArea
    public void UpdateCurrentArea(string newCurrentArea)
    {
       //Update Colour
       foreach(KeyValuePair<string, RectTransform> area in uiAreas)
        {
            area.Value.GetComponent<Image>().color = area.Key == newCurrentArea ? currentAreaColour : defaultColour;
        }

       MovePlayerIcon(newCurrentArea);
    }

    //MovePlayerIcon
    private void MovePlayerIcon(string newCurrentArea)
    {
        if(uiAreas.TryGetValue(newCurrentArea, out RectTransform areaUI))
        {
            //If current area was found set the icon position to center of the area
            playerIcon.anchoredPosition = areaUI.anchoredPosition;
        }
    }
}
