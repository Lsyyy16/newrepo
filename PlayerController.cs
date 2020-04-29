using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Player Movement variables.
    public float moveSpeed = 2.5f;
    public float sprintSpeed = 5f;
    public float jumpSpeed = 12f;
    public bool doubleJumpCheck;
    public int jumpCount;
    public bool canMove;
    public Rigidbody2D rb;

    // Variables for level manager.
    public Vector3 respawnPosition;
    public LevelManager theLevelManager;

    // Grounded variables.
    public Transform vibeCheck;
    public float vibeCheckRadius;
    public LayerMask realGrounded;
    public bool isGrounded;

    // Knockback variables.
    public float knockbackForce;
    public float knockbackDuration;     // Duration in seconds of every knockback.
    public float currentKnockbackDuration;     // Remaining duration of existing knockback.

    // Cooldown bar variables.
    public int maxCooldown = 2;
    public int minCooldown = 0;
    public int currentCooldown;
    public CooldownBar cooldownBar;

    // Audio variables.
    public AudioSource jumpSound;
    public AudioSource hurtSound;
    public AudioSource skillSound;

    // Attack variables.
    private bool attack;
    public GameObject attackCollider;
    public float timeForAnimation;

    private bool m_PlatformMoving = false;  // Check for moving platform.
    private Animator myAnim;    // Reference the animator.
    private DialogueManager theDialogueManager; // Reference to the dialogue.

    // Start is called before the first frame update
    void Start()
    {
        respawnPosition = transform.position;   // Sets respawn position to where the player spawns.

        // Gets and store reference to the respective components so that it can be accessed.
        rb = GetComponent<Rigidbody2D>();
        myAnim = GetComponent<Animator>();

        // Searches for the respective GameObjects that the LevelManager & DialogueManager scripts are attached to.
        theLevelManager = FindObjectOfType<LevelManager>();
        theDialogueManager = FindObjectOfType<DialogueManager>();      
        
        // Ensures that the cooldown bar is full at the start of the game.
        currentCooldown = maxCooldown;  
        cooldownBar.SetMaxCooldown(maxCooldown);

        // Makes sure the attack collider is not active at the start of the game.
        attackCollider.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(!canMove)    // Makes sure player is not in knockback at Level End.
        {
            isGrounded = Physics2D.OverlapCircle(vibeCheck.position, vibeCheckRadius, realGrounded);
            myAnim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            myAnim.SetBool("Ground", isGrounded);
            myAnim.SetBool("Attack", attack);
            return;
        }

        // Ground check formula.
        isGrounded = Physics2D.OverlapCircle(vibeCheck.position, vibeCheckRadius, realGrounded);

        // To make sure the player does not move on the z axis.
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        if (currentKnockbackDuration <= 0)       // If we are not currently in knockback.
        {
            if (Input.GetAxisRaw("Horizontal") > 0)     // Move right.
            {
                rb.velocity = new Vector2(moveSpeed, rb.velocity.y);

                if (m_PlatformMoving)   // Makes sure the change in player scale is offset.
                {
                    transform.localScale = new Vector3(.5f, .5f, .5f);
                }
                else
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }             
            }
            else if (Input.GetAxisRaw("Horizontal") < 0)    // Move left.
            {
                rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);

                if (m_PlatformMoving)   // Makes sure the change in player scale is offset.
                {
                    transform.localScale = new Vector3(-.5f, .5f, .5f);
                }
                else
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);    // Player does not move if no input.
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveSpeed = sprintSpeed;    // Code for player sprint.
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                moveSpeed = 2.5f;   // Sets speed of player back to walk speed.
            }

            if (Input.GetButtonDown("Jump") && isGrounded && (jumpCount <= 0))
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
                jumpCount += 1;     // Adds one to the jumpCount.
                doubleJumpCheck = false;    // Resets doubleJump.
                jumpSound.Play();   // Plays jump sound.
            }
            else if (Input.GetButtonDown("Jump") && !isGrounded && (jumpCount <= 1) && !doubleJumpCheck)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);        // Makes player jump a second time.
                jumpCount = 0;  // Resets jumpCount to 0.
                doubleJumpCheck = true;     // Disables double jump until the next time.
                jumpSound.Play();   // Plays jump sound.
            }
            else
            {
                jumpCount = 0;  // Resets jumpCount to 0.
            }

            // For Attacking.
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (currentCooldown > minCooldown)
                {
                    StartCoroutine("AttackColliderDuration");
                }
                else return;
            }
            else
            {
                attack = false;
            }              
            
        }
        else    // When in knockback.
        {
            currentKnockbackDuration -= Time.deltaTime;     // Force the knockback of the Player.
            if (transform.localScale.x > 0)
            {
                rb.velocity = new Vector3(-knockbackForce, knockbackForce, 0.0f);       // Knockbacks the player to the left.
            }
            else
            {
                rb.velocity = new Vector3(knockbackForce, knockbackForce, 0.0f);        // Knockbacks the player to the right.
            }
        }
        // Animations.
        myAnim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        myAnim.SetBool("Ground", isGrounded);
        myAnim.SetBool("Attack", attack);
    }

    void FixedUpdate()
    {
        transform.Translate(moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime, 0f, moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime);
    }

    public void ReduceCooldown(int usedAttack)
    {
        if (currentCooldown > minCooldown)      // Decreases cooldown count.
        {
            currentCooldown -= usedAttack;
            cooldownBar.SetCooldown(currentCooldown);
            skillSound.Play();
        }
        else if (currentCooldown <= minCooldown)
        {
            currentCooldown = minCooldown;  // Makes sure the cooldown count does not go below 0.
        }
    }

    public void RestoreCooldown(int restoreAttack)
    {
        if (currentCooldown < maxCooldown)  // Increases cooldown count.
        {
            currentCooldown += restoreAttack;
            cooldownBar.SetCooldown(currentCooldown);
        }
        else if (currentCooldown >= maxCooldown)
        {
            currentCooldown = maxCooldown;  // Makes sure the cooldown count does not go above 2.
        }
    }

    

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "KillPlane")
        {
            theLevelManager.Respawn();  // If player falls off the map.
        }
        if (other.tag == "Checkpoint")
        {
            respawnPosition = other.transform.position;     // Set the player respawn position to checkpoint position.
        }
        if (other.tag == "Throne")
        {
            theDialogueManager.inLvlTwo = true;     // Makes sure only victory screen appears in Lvl2.
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Makes the player a child of the moving platform so as to move along with it.
        if (other.gameObject.tag == "MovingPlatform")
        {
            gameObject.transform.parent = other.transform;
            m_PlatformMoving = true;
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        // Releases player from being a child of the moving platform when player leaves.
        if (other.gameObject.tag == "MovingPlatform")
        {
            gameObject.transform.parent = null;
            m_PlatformMoving = false;
        }
    }

    // Check if player is falling.
    public bool IsFalling()
    {
        if (rb.velocity.y < 0) return true;
        else return false;
    }

    public void Knockback()
    {
        currentKnockbackDuration = knockbackDuration;   // Sets the duration of the current knockback to the knockback duration.
    }

    public IEnumerator AttackColliderDuration()
    {
        attackCollider.SetActive(true);     // Activates attack collider.
        attack = true;
        ReduceCooldown(1);  // Decreses cooldown count by 1.
        yield return new WaitForSeconds(timeForAnimation);  // Wait till animation plays finish and disable attack collider.
        attack = false;
        attackCollider.SetActive(false);
    }
}

