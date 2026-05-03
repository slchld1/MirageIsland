using System.Collections;
using UnityEngine;

public enum FishState
{
    BiteFlee,       // 0.45s scripted opening
    Following,
    Resting,
    DartingLeft,
    DartingRight,
    DartingOut
}
/// <summary>
/// Hooked fish. Owns its own state machine, stamina, and sprite facing.
/// Phase 5: only BiteFlee state is implemented; others are stubbed for Phase 6.
/// </summary>
public class Fish : MonoBehaviour
{
    public FishData data;
    public SpriteRenderer body;

    private FishingTuning tuning;
    private FightArena arena;
    private FishingFight fight;
    private FishState state = FishState.BiteFlee;
    private float stamina;
    private float stateTimer;
    private Vector2 biteFleeDir;

    public FishState State => state;
    public bool IsBiteFleeing => state == FishState.BiteFlee;

    public void Init(FishData d, FightArena a, FishingTuning t, FishingFight f, Vector2 lurePos)
    {
        data = d;
        arena = a;
        tuning = t;
        fight = f;
        transform.position = lurePos;

        int rarity = (d != null) ? Mathf.Max(1, d.rarity) : 1;
        stamina = tuning.fishStaminaMaxBase + tuning.fishStaminaMaxPerRarity * (rarity - 1);

        // Pick a random bite-flee direction: lateral OR angled-out, no screen-edge bias.
        bool lateralPick = Random.value < 0.6f;
        if (lateralPick)
            biteFleeDir = arena.lateral * (Random.value < 0.5f ? -1f : 1f);
        else
        {
            float lateralSign = Random.value < 0.5f ? -1f : 1f;
            biteFleeDir = (arena.outward + arena.lateral * lateralSign * 0.5f).normalized;
        }
        stateTimer = tuning.biteFleeDuration;

        FaceAwayFromPlayer();
    }

    public void Tick(float dt)
    {
        switch (state)
        {
            case FishState.BiteFlee: TickBiteFlee(dt); break;
                // Phase 6 fills in Following/Resting/Darting* branches
        }
        UpdateSpriteFacing();
    }

    private void TickBiteFlee(float dt)
    {
        Vector2 step = biteFleeDir * tuning.biteFleeFollowSpeed * dt;
        transform.position += (Vector3)step;

        stateTimer -= dt;
        if (stateTimer <= 0f) EnterFollowing();
    }

    private void EnterFollowing()
    {
        state = FishState.Following;
        stateTimer = 0f;
    }

    private void FaceAwayFromPlayer()
    {
        Vector2 toPlayer = arena.playerAnchor - (Vector2)transform.position;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
    }

    private void UpdateSpriteFacing()
    {
        // Phase 8 fills in dart tilt + near-shore flip.
        FaceAwayFromPlayer();
    }

    public Vector2 PositionOnLure() => transform.position;
    public Vector2 BiteFleeDirection => biteFleeDir;
}

