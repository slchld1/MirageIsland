using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
public enum TreeState
{
    Seedling,
    Mature,
    Ripe,
    Stump,
}

public class TreeMain : MonoBehaviour
{
    [Header("Config")]
    public TreeData treeData;

    [Header("Runtime")]
    public TreeState state = TreeState.Mature;
    public bool wasPlanted = false;
    public string treeID;
    public float stateEnteredAtTotalHours;
    public int hpRemaining;
    public bool permanentlyGone;

    [Header("Renderers (assign in prefab)")]
    public SpriteRenderer topRenderer;
    public SpriteRenderer stumpRenderer;
    public SpriteRenderer[] fruitRenderers;

    [Header("Animator (assign in prefab")]
    public TreeAnimator animator;

    [Header("Colliders (assign in prefab)")]
    public Collider2D trunkCollider;

    [Header("Fall")]
    public float fallAlignThreshold = 0.5f;
    public float woodDropMinDistance = 1.2f;
    public float woodDropMaxDistance = 2.0f;

    [Header("Fruit Runtime")]
    public int currentFruitCount = 0;
    private float nextFruitGrowsAtTotalHours = 0f;

    [Header("Guard Flag")]
    private bool isFelling;


    private void Awake()
    {
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = GlobalHelper.GenerateUniqueId(gameObject);
        }
        hpRemaining = treeData != null ? treeData.chopCount : 1;
        UpdateSprite();
    }
    private void Update()
    {
        if (treeData == null || DayCycleManager.Instance == null) return;

        if (stateEnteredAtTotalHours == 0f)
        {
            stateEnteredAtTotalHours = DayCycleManager.Instance.TotalHours;
            ScheduleNextFruit();
            return;
        }

        float elapsed = DayCycleManager.Instance.TotalHours - stateEnteredAtTotalHours;

        switch (state)
        {
            case TreeState.Seedling:
                if (elapsed >= treeData.growHours) Enter(TreeState.Mature);
                break;
            case TreeState.Mature:
            case TreeState.Ripe:
                if (treeData.fruitItem == null) break;
                if (currentFruitCount >= treeData.maxFruitCount) break;
                if (DayCycleManager.Instance.TotalHours >= nextFruitGrowsAtTotalHours)
                {
                    currentFruitCount++;
                    if (state == TreeState.Mature)
                    {
                        Enter(TreeState.Ripe); // Enter() reschedules via ScheduleNextFruit
                    }
                    else
                    {
                        UpdateSprite();
                        if (currentFruitCount < treeData.maxFruitCount) ScheduleNextFruit(); 
                        // at cap -> freeze timer and queue up the next fruit when pick fruit
                    }
                }
                break;

            case TreeState.Stump:
                if (treeData.regrows && elapsed >= treeData.regrowHours) Enter(TreeState.Mature);
                break;
        }
    }
    private void ScheduleNextFruit()
    {
        if (treeData == null || treeData.fruitItem == null) return;
        if (DayCycleManager.Instance == null) return;

        float now = DayCycleManager.Instance.TotalHours;
        if (state == TreeState.Mature)
            nextFruitGrowsAtTotalHours = now + treeData.matureCooldownHours;
        else if (state == TreeState.Ripe)
            nextFruitGrowsAtTotalHours = now + Random.Range(treeData.fruitGrowMinHours, treeData.fruitGrowMaxHours);
    }

    private void Enter(TreeState next)
    {
        state = next;
        if (DayCycleManager.Instance != null)
        {
            stateEnteredAtTotalHours = DayCycleManager.Instance.TotalHours;
        }
        UpdateSprite();
        if (trunkCollider != null) trunkCollider.enabled = (next != TreeState.Stump);
        ScheduleNextFruit();
    }

    public void TakeDamage(int damage, Vector3 chopperWorldPos)
    {
        if (isFelling) return;
        if (treeData == null || !treeData.isChoppable) return;
        if (state == TreeState.Seedling || state == TreeState.Stump) return;

        int fallDir = ComputeFallDirection(chopperWorldPos);

        if (animator != null) animator.PlayShake();

        if (state == TreeState.Ripe)
        {
            DropFruits(currentFruitCount);
            currentFruitCount = 0;
            Enter(TreeState.Mature);
            return;
        }

        hpRemaining -= damage;
        if (hpRemaining <= 0) Fell(fallDir);
    }
    public void PickFruit()
    {
        if (state != TreeState.Ripe) return;
        if (currentFruitCount <= 0) return;

        bool wasAtMax = (currentFruitCount >= treeData.maxFruitCount);

        DropFruits(1);
        currentFruitCount--;

        if (currentFruitCount <= 0)
        {
        Enter(TreeState.Mature);
        }
        else
        {
            UpdateSprite();
            if (wasAtMax) ScheduleNextFruit();
        }
    }

    private int ComputeFallDirection(Vector3 chopperWorldPos)
    {
        float dx = chopperWorldPos.x - transform.position.x;
        if (Mathf.Abs(dx) < fallAlignThreshold)
        {
            return UnityEngine.Random.value < 0.5f ? -1 : 1;
        }
        return dx > 0f ? -1 : 1;
    }

    private int pendingFallDir; // stashed for onFellComplete
    private void Fell(int fallDir)
    {
        isFelling = true;
        pendingFallDir = fallDir;

        if (trunkCollider != null) trunkCollider.enabled = false;

        if (animator != null)
        {
            animator.PlayFell(
                fallDir,
                onImpact: () => DropWood(pendingFallDir),
                onComplete: OnFellComplete
                );
        }
        else
        {
            DropWood(fallDir);
            OnFellComplete();
        }
    }


    private void OnFellComplete()
    {
        isFelling = false;
        if(wasPlanted)
        {
            Destroy(gameObject);
            return;
        }
        if (!treeData.regrows)
        {
            permanentlyGone = true;
            Destroy(gameObject);
            return;
        }
        hpRemaining = treeData.chopCount;
        Enter(TreeState.Stump);
    }

    private void DropWood(int fallDir)
    {
        if (treeData.woodItem == null) return;
        int count = Random.Range(treeData.woodDropMin, treeData.woodDropMax + 1);
        for (int i = 0; i < count; i++)
        {
            float xPush = fallDir * UnityEngine.Random.Range(woodDropMinDistance, woodDropMaxDistance);
            float yJit = UnityEngine.Random.Range(-0.2f, 0.2f);
            Vector3 offset = new Vector3(xPush, yJit, 0f);
            GameObject drop = Instantiate(treeData.woodItem.gameObject, transform.position + offset, Quaternion.identity);
            var bounce = drop.GetComponent<BounceEffect>();
            if (bounce != null) bounce.StartBounce();
        }
    }

    private void DropFruits(int count)
    {
        if (treeData.fruitItem == null) return;
        int totalItems = count * treeData.yieldPerPick;
        if (totalItems <= 0) return;


        for (int i = 0; i < totalItems;i++)
        {
            float angle = (i / (float)totalItems) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float radius = Random.Range(0.6f, 0.9f);

            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle), 0f);

            GameObject drop = Instantiate(treeData.fruitItem.gameObject, transform.position + offset, Quaternion.identity);
            var bounce = drop.GetComponent<BounceEffect>();
            if (bounce != null) bounce.StartBounce();
        }
    }
    

    private void UpdateSprite()
    {
        if (treeData == null) return;

        switch(state)
        {
            case TreeState.Seedling:
                if (topRenderer != null)
                {
                    topRenderer.sprite = treeData.saplingSprite;
                    topRenderer.enabled = true;
                    Color c1 = topRenderer.color; c1.a = 1f; topRenderer.color = c1;
                }
                if (stumpRenderer != null) stumpRenderer.enabled = false;
                break;

            case TreeState.Mature:
            case TreeState.Ripe:
                if (topRenderer != null)
                {
                    topRenderer.sprite = treeData.topSprite;
                    topRenderer.enabled = true;
                    Color c2 = topRenderer.color; c2.a = 1f; topRenderer.color = c2;
                }
                if (stumpRenderer != null)
                {
                    stumpRenderer.sprite = treeData.stumpSprite;
                    stumpRenderer.enabled = true;
                }
                break;

            case TreeState.Stump:
                if (topRenderer != null) topRenderer.enabled = false;
                if (stumpRenderer != null)
                {
                    stumpRenderer.sprite = treeData.stumpSprite;
                    stumpRenderer.enabled = true;
                }
                break;
        }

        if(fruitRenderers != null)
        {
            for (int i = 0; i < fruitRenderers.Length; i++)
            {
                if (fruitRenderers[i] != null)
                    fruitRenderers [i].gameObject.SetActive(i < currentFruitCount);
            }
        }
    }

}
