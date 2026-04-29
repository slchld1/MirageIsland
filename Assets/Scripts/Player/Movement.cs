using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{

    Vector2 moveInput;
    public Vector2 LastMoveInput { get; private set; } = Vector2.down;
    public bool IsMoving { get; private set; }

    Rigidbody2D rb;

    public SpriteRenderer spriteRenderer;

    public Animator animator;

    private bool playingFootsteps = false;


    [SerializeField]
    public float speed = 5;
    public float footstepSpeed = 0.5f;

    public PlayerToolDispatcher toolDispatcher;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponentInChildren<Animator>();    

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    private void Update()
    {
        if (toolDispatcher != null && toolDispatcher.IsChopping)
        {
            rb.linearVelocity = Vector2.zero;
            StopFootsteps();
            return;
        }
        if (PauseController.IsGamePaused || FishingController.IsFishing)
        {
            rb.linearVelocity = Vector2.zero;
            animator.enabled = false;
            StopFootsteps();
            return;
        }    
        rb.linearVelocity = moveInput * speed;
        animator.enabled= true;

        //StartFootSteps
        if (rb.linearVelocity.magnitude > 0 && !playingFootsteps)
        {
            StartFootsteps();
        }
        else if(rb.linearVelocity.magnitude == 0 && playingFootsteps)
        {
            StopFootsteps();
        }

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            spriteRenderer.flipX = moveInput.x < 0;
        }

        // Always keep animator pointing in last known direction so idle doesn't snap to default
        animator.SetFloat("horizontal", LastMoveInput.x);
        animator.SetFloat("vertical", LastMoveInput.y);

    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        IsMoving = moveInput.sqrMagnitude > 0.01f;
        if (IsMoving)
            LastMoveInput = moveInput;



        animator.SetBool("isMoving", moveInput.sqrMagnitude > 0.01f);

    }

    void StartFootsteps()
    {
        playingFootsteps = true;
        InvokeRepeating(nameof(PlayFootstep), 0f, footstepSpeed);
    }

    void StopFootsteps()
    {
        playingFootsteps = false;
        CancelInvoke(nameof(PlayFootstep));
    }
    
    void PlayFootstep()
    {
        if (toolDispatcher != null && toolDispatcher.IsChopping) return;
        SoundEffectManager.Play("Footstep", true);
    }

}
