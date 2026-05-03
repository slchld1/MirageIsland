using UnityEngine;

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

    public void Init(FightArena a, FishingTuning t, FishingLine l, Fish f, FishingController o)
    {
        arena = a; tuning = t; line = l; fish = f; owner = o;
    }

    public void PunchTension(float amount) { /* Phase 6 implements */ }

    public void Tick(float dt, bool dampedInput)
    {
        // Phase 5 stub: during bite-flee, drag lure to fish position.
        if (fish != null && fish.IsBiteFleeing)
        {
            line.SetBobPosition(fish.PositionOnLure());
        }
    }
}
