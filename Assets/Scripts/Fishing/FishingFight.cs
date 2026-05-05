using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fight loop runner. Phase 6 fills in the force/tension model and full Tick.
/// Phase 5 stub: holds references and provides PunchTension + a bite-flee Tick that drags the lure.
/// </summary>
public class FishingFight : MonoBehaviour
{
    private FightArena arena;
    private FishingTuning tuning;
    private FishingLine line;
    private Fish fish;
    private FishingController owner;

    private float tension;
    private float tensionOverTime;
    private float catchProgress;
    private Vector2 lureVelocity;

    public void Init(FightArena a, FishingTuning t, FishingLine l, Fish f, FishingController o)
    {
        arena = a; tuning = t; line = l; fish = f; owner = o;
        tension = 0f;
        tensionOverTime = 0f;
        catchProgress = 0f;
        lureVelocity = Vector2.zero;
    }

    public float Tension => tension;
    public float CatchProgress => catchProgress;
    public FightArena Arena => arena;

    public Vector2 GetLureWorld() => line.BobPosition;

    public void PunchTension(float amount) 
    {
        tension = Mathf.Min(tuning.tensionMax, tension + amount);
    }

    public void Tick(float dt)
    {
        if (fish != null && fish.IsBiteFleeing)
        {
            line.SetBobPosition(fish.PositionOnLure());
            lureVelocity = Vector2.zero;
            tension = Mathf.Max(0f, tension - tuning.tensionDecayPerSecond * dt);
            return;
        }

        var kb = Keyboard.current;
        Key keyNeg = arena.lateralKeysAreQE ? Key.Q : Key.W;
        Key keyPos = arena.lateralKeysAreQE ? Key.E : Key.S;
        bool holdNeg = kb != null && kb[keyNeg].isPressed;
        bool holdPos = kb != null && kb[keyPos].isPressed;
        bool tapNeg = kb != null && kb[keyNeg].wasPressedThisFrame;
        bool tapPos = kb != null && kb[keyPos].wasPressedThisFrame;
        bool lmbHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool shiftHeld = kb != null && (kb[Key.LeftShift].isPressed ||  kb[Key.RightShift].isPressed);

        Vector2 liveOutward = arena.LiveOutward(line.BobPosition);
        Vector2 liveTangent = arena.LiveTangent(line.BobPosition);

        Vector2 playerForce = Vector2.zero;
        if (holdNeg) playerForce += -liveTangent * tuning.lateralHoldForce;
        if (holdPos) playerForce += liveTangent * tuning.lateralHoldForce;
        if (tapNeg) playerForce += -liveTangent * tuning.lateralTapImpulse;
        if (tapPos) playerForce += liveTangent * tuning.lateralTapImpulse;
        if (lmbHeld)
        {
            float speed = shiftHeld ? tuning.fastReelSpeed : tuning.slowReelSpeed;
            playerForce += -liveOutward * speed;
        }

        Vector2 fishForce = Vector2.zero;
        if (fish != null)
        {
            switch (fish.State)
            {
                case FishState.DartingLeft: fishForce = -liveTangent * fish.CurrentStrength; break;
                case FishState.DartingRight: fishForce = liveTangent * fish.CurrentStrength; break;
                case FishState.DartingOut: fishForce = liveOutward * fish.CurrentStrength; break;
                case FishState.Following:
                    float wobble = Mathf.Sin(Time.time * 3f) * fish.CurrentStrength * 0.1f;
                    fishForce = liveTangent * wobble;
                    break;
            }
            Vector2 targetVelocity = (playerForce + fishForce) * (1f / 16f);
            lureVelocity = Vector2.Lerp(targetVelocity, lureVelocity, tuning.lureDampingPerFrame);

            Vector2 currentPos = line.BobPosition;
            Vector2 desiredPos = currentPos + lureVelocity * dt;
            Vector2 finalPos;

            if (Physics2D.OverlapPoint(desiredPos, tuning.waterLayerMask) != null)
            {
                finalPos = desiredPos;
            }
            else
            {
                Vector2 slideX = new Vector2(desiredPos.x, currentPos.y);
                if (Physics2D.OverlapPoint(slideX, tuning.waterLayerMask) != null)
                {
                    finalPos = slideX;
                    lureVelocity.y = 0f;
                }
                else
                {
                    Vector2 slideY = new Vector2(currentPos.x, desiredPos.y);
                    if (Physics2D.OverlapPoint(slideY, tuning.waterLayerMask) != null)
                    {
                        finalPos = slideY;
                        lureVelocity.x = 0f;
                    }
                    else
                    {
                        finalPos = currentPos;
                        lureVelocity = Vector2.zero;
                    }
                }
            }
            line.SetBobPosition(finalPos);

            Vector2 polar = arena.WorldToPolar(finalPos);
            float radius = polar.x;
            float angleAbs = Mathf.Abs(polar.y);

            float angleSeverity = Mathf.Max(0f, angleAbs - arena.greenAngleDeg) / Mathf.Max(1f, arena.greenAngleDeg);
            float radiusSeverity = Mathf.Max(0f, radius - arena.greenMaxRadius) / Mathf.Max(0.5f, arena.greenMaxRadius);
            float severity = Mathf.Clamp01(Mathf.Max(angleSeverity, radiusSeverity));

            if (arena.IsInGreenZone(finalPos))
            {
                tension = Mathf.Max(0f, tension - tuning.tensionDecayPerSecond * dt);
            }
            else
            {
                tension += tuning.outOfZoneTensionRate * (0.25f + 0.75f * severity) * dt;
            }
            tension = Mathf.Min(tension, tuning.tensionMax + 1f);

            if (tension >= tuning.tensionMax) tensionOverTime += dt;
            else tensionOverTime = 0f;

            if (tensionOverTime >= tuning.tensionSnapWindow)
            {
                owner.NotifyEscape();
                return;
            }

            float alignment = 1f - Mathf.Clamp01(angleAbs / Mathf.Max(1f, arena.greenAngleDeg));
            float proximityRange = Mathf.Max(0.5f, arena.castLandRadius - arena.minRadius);
            float proximity = 1f - Mathf.Clamp01((radius - arena.minRadius) / proximityRange);
            float centeringScore = alignment * proximity;

            if (fish != null)
            {
                bool catchable = fish.State == FishState.Following || fish.State == FishState.Resting;
                bool darting = fish.State == FishState.DartingLeft
                            || fish.State == FishState.DartingRight
                            || fish.State == FishState.DartingOut;
                if (catchable)
                    catchProgress += centeringScore * tuning.catchFillRate * dt;
                else if (darting)
                    catchProgress -= tuning.catchDecayRate * dt;
            }
            catchProgress = Mathf.Clamp01(catchProgress);

            if (catchProgress >= 1f)
            {
                owner.NotifyCatch();
                return;
            }

            if (Vector2.Distance(owner.transform.position, arena.playerAnchor) > tuning.playerMoveCancelDistance)
            {
                owner.NotifyEscape();
                return;
            }
        }
    }

}
