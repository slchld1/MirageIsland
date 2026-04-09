using UnityEngine;
using Cinemachine;
using UnityEditor.Experimental.GraphView;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundary;

    [SerializeField] Direction direction;

    [SerializeField] float addPos = 1.8f;

    enum Direction { Up, Down, Left, Right }

    CinemachineConfiner confiner;




    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            confiner.m_BoundingShape2D = mapBoundary;
            UpdatePlayerPosition(collision.gameObject);

            MapController_Manual.Instance?.HighLightArea(mapBoundary.name);
            MapController_Dynamic.Instance?.UpdateCurrentArea(mapBoundary.name);
        }
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;

        switch (direction)
        {
            case Direction.Up:
                newPos.y += addPos;
                break;
            case Direction.Down:
                newPos.y -= addPos;
                break;
            case Direction.Left:
                newPos.x += addPos;
                break;
            case Direction.Right:
                newPos.x -= addPos;
                break;
        }

        player.transform.position = newPos;
    }
}
