using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-space mesh overlay that visualizes the polar arena's three zones:
/// green (in greenzone), yellow (severity 0–0.5), red (severity > 0.5).
/// Built once on Show; static during the fight.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FightArenaOverlay : MonoBehaviour
{
    [Header("Band Sizing")]
    [Tooltip("Yellow extends out to this multiple of green's angle and radius (severity 0.5 boundary).")]
    public float yellowScale = 1.5f;
    [Tooltip("Red extends out to this multiple of green's angle and radius (visual cap).")]
    public float redScale = 2.5f;

    [Header("Resolution")]
    [Tooltip("Triangle-fan segments per pie slice. Higher = smoother arc.")]
    public int segments = 32;

    [Header("Colors")]
    public Color greenColor = new Color(0.30f, 0.85f, 0.35f, 0.35f);
    public Color yellowColor = new Color(0.95f, 0.85f, 0.20f, 0.30f);
    public Color redColor = new Color(0.90f, 0.30f, 0.25f, 0.25f);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh { name = "FightArenaOverlay" };
        meshFilter.sharedMesh = mesh;
        gameObject.SetActive(false);
    }

    public void Show(FightArena arena)
    {
        transform.position = arena.playerAnchor;
        float ang = Mathf.Atan2(arena.castDir.y, arena.castDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);

        Build(arena);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Build(FightArena arena)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var colors = new List<Color>();

        // Layered: red (biggest) is drawn first, yellow on top, green on top of yellow.
        // With Sprites/Default-style material (ZWrite off, alpha blend), submission order = draw order.
        AppendPieSlice(verts, tris, colors, arena.greenAngleDeg * redScale, arena.greenMaxRadius * redScale,
redColor);
        AppendPieSlice(verts, tris, colors, arena.greenAngleDeg * yellowScale, arena.greenMaxRadius * yellowScale,
yellowColor);
        AppendPieSlice(verts, tris, colors, arena.greenAngleDeg, arena.greenMaxRadius,
greenColor);

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();
    }

    private void AppendPieSlice(
        List<Vector3> verts, List<int> tris, List<Color> colors,
        float halfAngleDeg, float radius, Color c)
    {
        int center = verts.Count;
        verts.Add(Vector3.zero);
        colors.Add(c);

        // Local space: castDir = +X. Sweep angle around +X from -halfAngleDeg to +halfAngleDeg.
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, t) * Mathf.Deg2Rad;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            colors.Add(c);
        }

        for (int i = 0; i < segments; i++)
        {
            tris.Add(center);
            tris.Add(center + 1 + i);
            tris.Add(center + 2 + i);
        }
    }
}
