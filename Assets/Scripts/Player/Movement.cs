using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{

    Vector2 moveInput;

    Rigidbody2D rb;

    public SpriteRenderer spriteRenderer;

    public Animator animator;

    private bool playingFootsteps = false;


    [SerializeField]
    public float speed = 5;
    public float footstepSpeed = 0.5f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponentInChildren<Animator>();    

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    private void Update()
    {
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

    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if(moveInput.sqrMagnitude > 0.01f)
        {
        animator.SetFloat("horizontal", moveInput.x);

        animator.SetFloat("vertical", moveInput.y);
        }



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
        SoundEffectManager.Play("Footstep", true);
    }

}
