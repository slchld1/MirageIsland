using System.Collections;
using UnityEngine;

public class PickupDelay : MonoBehaviour
{
    [Header("Delay Tuning")]
    public float delaySeconds = 0.5f;

    [Header("Refs (assign in prefab)")]
    public Collider2D pickupCollider;

    private void Start()
    {
        if (pickupCollider != null) pickupCollider.enabled = false;
        StartCoroutine(EnableAfterDelay());
    }

    private IEnumerator EnableAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        if (pickupCollider != null) pickupCollider.enabled = true;
    }
}
