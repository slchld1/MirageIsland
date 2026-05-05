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
    private float currentStrength;

    public FishState State => state;
    public float CurrentStrength => currentStrength;
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
        currentStrength = tuning.baseFishStrength * (1f + 0.25f * (rarity - 1));

        Vector2 outwardAtCast = arena.castDir;
        Vector2 lateralAtCast = new Vector2(-outwardAtCast.y, outwardAtCast.x);
        // Pick a random bite-flee direction: lateral OR angled-out, no screen-edge bias.
        bool lateralPick = Random.value < 0.6f;
        if (lateralPick)
            biteFleeDir = lateralAtCast * (Random.value < 0.5f ? -1f : 1f);
        else
        {
            float lateralSign = Random.value < 0.5f ? -1f : 1f;
            biteFleeDir = (outwardAtCast + lateralAtCast * lateralSign * 0.5f).normalized;
        }
        stateTimer = tuning.biteFleeDuration;

        FaceAwayFromPlayer();
    }

    public void Tick(float dt)
    {
        switch (state)
        {
            case FishState.BiteFlee: TickBiteFlee(dt); break;
            case FishState.Following: TickFollowing(dt); break;
            case FishState.Resting: TickResting(dt); break;
            case FishState.DartingLeft:
            case FishState.DartingRight:
            case FishState.DartingOut: TickDart(dt); break;
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
    
    private void TickFollowing(float dt)
    {
        Vector2 lurePos = fight.GetLureWorld();
        Vector2 toLure = lurePos - (Vector2)transform.position;
        float step = tuning.followSpeed * dt;
        if (toLure.magnitude > step)
        {
            transform.position += (Vector3)(toLure.normalized * step);
        }
        else
        {
            transform.position = lurePos;
        }
        stamina = Mathf.Min(MaxStamina(), stamina + tuning.staminaRegenWhileFollowing * dt);

        int rarity = (data != null) ? Mathf.Max(1, data.rarity) : 1;
        float dartChance = tuning.baseDartChance * (1f + 0.20f * (rarity - 1) * dt);
        if (Random.value < dartChance && stamina >= tuning.dartStaminaCost)
        {
            EnterDart();
        }
    }

    private void TickResting(float dt)
    {
        stamina = Mathf.Min(MaxStamina(), stamina + tuning.staminaRegenWhileResting * dt);
        stateTimer -= dt;
        if (stateTimer <= 0f) EnterFollowing();
    }

    private void TickDart(float dt)
    {
        Vector2 lurePos = fight.GetLureWorld();
        Vector2 toLure = lurePos - (Vector2)transform.position;
        float step = tuning.followSpeed * 1.5f * dt;
        if (toLure.magnitude > step)
        {
            transform.position += (Vector3)(toLure.normalized * step);
        }
        else
        {
            transform.position = lurePos;
        }
        stateTimer -= dt;
        if (stateTimer <= 0f)
        { 
            stamina -= tuning.dartStaminaCost;
            EnterResting();
        }
    }

    private void EnterFollowing()
    {
        state = FishState.Following;
        stateTimer = 0f;
    }

    private void EnterResting()
    {
        state = FishState.Resting;
        int rarity = (data != null) ? Mathf.Max(1, data.rarity) : 1;
        stateTimer = tuning.restDurationBase + tuning.restDurationPerRarity * (rarity - 1);
        stateTimer = Mathf.Max(0.2f, stateTimer);
    }

    private void EnterDart()
    {
        int rarity = (data != null) ? Mathf.Max(1, data.rarity) : 1;
        float t = (rarity - 1) / 4f;
        Vector3 w1 = tuning.dartWeightsRarity1;
        Vector3 w5 = tuning.dartWeightsRarity5;
        Vector3 w = Vector3.Lerp(w1, w5, t);
        float total = w.x + w.y + w.z;
        float r = Random.value * total;

        if (r < w.x) state = FishState.DartingLeft;
        else if (r < w.x + w.y) state = FishState.DartingRight;
        else state = FishState.DartingOut;

        stateTimer = tuning.dartDurationBase + tuning.dartDurationPerRarity * (rarity - 1);
        stateTimer = Mathf.Max(0.2f, stateTimer);
    }

    private float MaxStamina()
    {
        int rarity = (data != null) ? Mathf.Max(1, data.rarity) : 1;
        return tuning.fishStaminaMaxBase + tuning.fishStaminaMaxPerRarity * (rarity - 1);
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
}

