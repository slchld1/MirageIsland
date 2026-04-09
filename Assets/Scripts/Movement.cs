using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{

    Vector2 moveInput;

    Rigidbody2D rb;

    public SpriteRenderer spriteRenderer;

    public Animator animator;

    [SerializeField]
    public float speed = 5;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponentInChildren<Animator>();    

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    private void Update()
    {
        if (PauseController.IsGamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            animator.enabled = false;
            return;
        }    
        rb.linearVelocity = moveInput * speed;
        animator.enabled= true;
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            spriteRenderer.flipX = moveInput.x < 0;
        }

        //if(moveInput.x != 0)
        //{
        //    if (moveInput.x > 0)
        //    {
        //        spriteRenderer.flipX = false;
        //    }else if (moveInput.x < 0)
        //    { 
        //        spriteRenderer.flipX = true; 
        //    }
        //}

        Debug.Log(moveInput);

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

}
