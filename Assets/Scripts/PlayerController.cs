using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D coll;
    private LayerMask ground;
    private LayerMask enemyLayer;
    [SerializeField] private AudioSource footstep;
    [SerializeField] private AudioSource gemAudio;
    [SerializeField] private AudioSource jumpAudio;

    // Speed boost variables
    private float normalSpeed = 9f;
    private float boostedSpeed = 15f;
    private int gemsCollectedInLevel = 0;
    private bool isSpeedBoosted = false;
    private Coroutine speedBoostCoroutine;

    // Double jump variables
    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;
    private bool isInLevel2 = false;

    private enum State {idle, running, jumping, falling, hurt}; //animation states, decides interactions
    private State state = State.idle;
    private int speed = 9;
    //[SerializeField] private float airControl = 3f;
    private float jumpForce = 25f;
    private float airControl = 0.8f;
    private float hurtForce = 10f;
     
    [Header("Fire Attack Ability")]
    [Tooltip("The prefab for the fireball projectile that will be instantiated when shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [Tooltip("The transform position where fireballs will spawn from")]
    [SerializeField] private Transform firePoint;
    [Tooltip("How fast the fireball will travel")]
    [SerializeField] private float bulletSpeed = 15f;
    [Tooltip("How long the fire ability remains active after activation")]
    [SerializeField] private float abilityDuration = 10f;
    [Tooltip("Number of gems required to activate the fire ability")]
    [SerializeField] private int gemsRequired = 10;
    [Tooltip("Time between auto-fire shots")]
    [SerializeField] private float fireRate = 0.5f;
    [Tooltip("Audio source for the fire shooting sound")]
    [SerializeField] private AudioSource fireSound;
    [Tooltip("Visual effect shown when the fire ability is active")]
    [SerializeField] private GameObject abilityActiveEffect;
    [Tooltip("UI element showing the ability cooldown")]
    [SerializeField] private Image abilityCooldownUI;
    [Tooltip("Text displaying the current state of the fire ability")]
    [SerializeField] private TextMeshProUGUI abilityText;
    [Tooltip("Particle effect to show when ability is ready")]
    [SerializeField] private ParticleSystem readyEffect;
    [Tooltip("Particle effect to show when ability is active")]
    [SerializeField] private ParticleSystem activeEffect;
    
    private bool isAutoFiring = false;
    private int gemsCollectedForAbility = 0;
    private Coroutine abilityTimerCoroutine;
    private Coroutine autoFireCoroutine;
    private float abilityCooldownTimer = 0f;
    private bool isAbilityOnCooldown = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        coll = GetComponent<CircleCollider2D>();

        ground = LayerMask.GetMask("Ground");
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        
        // Reset gems collected in level when starting
        gemsCollectedInLevel = 0;
        
        // Check if we're in Level2 and unlock double jump
        isInLevel2 = SceneManager.GetActiveScene().name == "Level2";
        if (isInLevel2)
        {
            UnlockDoubleJump();
        }
        else
        {
            canDoubleJump = false;
        }

        // Initialize ability UI
        if (abilityCooldownUI != null)
        {
            abilityCooldownUI.fillAmount = 0f;
        }
        UpdateAbilityText();
        
        // Validate required components
        ValidateFireAbilityComponents();
    }

    private void ValidateFireAbilityComponents()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Fireball prefab is not assigned in the PlayerController. Auto-fire ability will not work!");
        }
        if (firePoint == null)
        {
            Debug.LogError("Fire point transform is not assigned in the PlayerController. Auto-fire ability will not work!");
        }
    }

    private void UnlockDoubleJump()
    {
        canDoubleJump = true;
        hasDoubleJumped = false;
    }

    private void Update()
    {
       if (state != State.hurt)
        {
            Movement();
        } 
        
        animator.SetInteger("state", (int)state); //set animation based on enumerator state
        AnimationState();
        
        // Reset double jump when touching ground
        if (coll.IsTouchingLayers(ground))
        {
            hasDoubleJumped = false;
        }

        // Handle fire ability
        HandleFireAbility();
        
        // Update ability cooldown UI
        UpdateAbilityUI();
    }

    private void HandleFireAbility()
    {
        // Check for ability activation
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!isAutoFiring && !isAbilityOnCooldown && gemsCollectedForAbility >= gemsRequired)
            {
                Debug.Log("Activating fire ability!"); // Debug log
                ActivateAutoFire();
            }
            else if (isAutoFiring)
            {
                Debug.Log("Shooting!"); // Debug log
                Shoot();
            }
        }
    }

    private void ActivateAutoFire()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Cannot activate auto-fire: Missing required components!");
            return;
        }

        isAutoFiring = true;
        gemsCollectedForAbility = 0;
        
        // Start ability timer
        if (abilityTimerCoroutine != null)
        {
            StopCoroutine(abilityTimerCoroutine);
        }
        abilityTimerCoroutine = StartCoroutine(AutoFireTimer());
        
        // Start auto-fire coroutine
        if (autoFireCoroutine != null)
        {
            StopCoroutine(autoFireCoroutine);
        }
        autoFireCoroutine = StartCoroutine(AutoFireRoutine());
        
        // Activate visual effect
        if (abilityActiveEffect != null)
        {
            abilityActiveEffect.SetActive(true);
        }
        
        // Show ready effect
        if (readyEffect != null)
        {
            readyEffect.Play();
        }
        
        Debug.Log("Auto-fire ability activated! Duration: " + abilityDuration + " seconds");
        UpdateAbilityText();
    }

    private IEnumerator AutoFireTimer()
    {
        float remainingTime = abilityDuration;
        
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            abilityCooldownTimer = remainingTime;
            UpdateAbilityUI();
            yield return null;
        }
        
        // Deactivate ability
        isAutoFiring = false;
        isAbilityOnCooldown = true;
        abilityCooldownTimer = abilityDuration;
        
        // Stop auto-fire
        if (autoFireCoroutine != null)
        {
            StopCoroutine(autoFireCoroutine);
        }
        
        // Deactivate visual effect
        if (abilityActiveEffect != null)
        {
            abilityActiveEffect.SetActive(false);
        }
        
        UpdateAbilityText();
    }

    private IEnumerator AutoFireRoutine()
    {
        while (isAutoFiring)
        {
            Shoot();
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Cannot shoot: Bullet prefab is missing!");
            return;
        }
        
        if (firePoint == null)
        {
            Debug.LogError("Cannot shoot: Fire point transform is missing!");
            return;
        }

        // Create bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Fireball fireballScript = bullet.GetComponent<Fireball>();
        
        if (fireballScript != null)
        {
            // Set bullet properties - direction doesn't matter for homing projectiles
            fireballScript.Initialize(bulletSpeed, 2f, true);
        }
        else
        {
            Debug.LogError("Fireball script not found on bullet prefab!");
        }
        
        // Play sound
        if (fireSound != null)
        {
            fireSound.Play();
        }
    }

    private void UpdateAbilityUI()
    {
        if (abilityCooldownUI != null)
        {
            if (isAbilityOnCooldown)
            {
                abilityCooldownTimer -= Time.deltaTime;
                abilityCooldownUI.fillAmount = abilityCooldownTimer / abilityDuration;
                
                if (abilityCooldownTimer <= 0f)
                {
                    isAbilityOnCooldown = false;
                    abilityCooldownUI.fillAmount = 0f;
                }
            }
            else if (isAutoFiring)
            {
                // Show remaining time while ability is active
                abilityCooldownUI.fillAmount = abilityCooldownTimer / abilityDuration;
            }
            else
            {
                // Show gem progress
                abilityCooldownUI.fillAmount = (float)gemsCollectedForAbility / gemsRequired;
            }
        }
    }

    private void UpdateAbilityText()
    {
        if (abilityText != null)
        {
            if (isAutoFiring)
            {
                abilityText.text = $"Auto-Fire: Active ({Mathf.Ceil(abilityCooldownTimer)}s)";
            }
            else if (isAbilityOnCooldown)
            {
                abilityText.text = $"Auto-Fire: Cooldown ({Mathf.Ceil(abilityCooldownTimer)}s)";
            }
            else
            {
                abilityText.text = $"Auto-Fire: {gemsCollectedForAbility}/{gemsRequired} Gems";
            }
        }
    }

    //trigger collision
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Collectible")
        {
            gemAudio.Play();
            Destroy(collision.gameObject);
            PermanentUI.perm.gems++;
            PermanentUI.perm.gemText.text = PermanentUI.perm.gems.ToString();
            
            // Track gems for ability
            if (!isAutoFiring && !isAbilityOnCooldown)
            {
                gemsCollectedForAbility++;
                UpdateAbilityText();
                
                // Activate auto-fire when enough gems are collected
                if (gemsCollectedForAbility >= gemsRequired)
                {
                    ActivateAutoFire();
                }
            }
            
            // Check for Level2 and gem collection
            if (SceneManager.GetActiveScene().name == "Level2")
            {
                gemsCollectedInLevel++;
                if (gemsCollectedInLevel >= 5 && !isSpeedBoosted)
                {
                    ActivateSpeedBoost();
                }
            }
        }
        if (collision.tag == "Enemy") //enemy projectiles
        {
                state = State.hurt;
                if (collision.gameObject.transform.position.x > transform.position.x) 
                {
                    rb.velocity = new Vector2(-hurtForce, hurtForce);
                }
                else
                {
                    rb.velocity = new Vector2(hurtForce, hurtForce);
                }
        }
    }

    //enemy collision
    private void OnCollisionEnter2D(Collision2D other)
    {
        
        if(other.gameObject.tag == "Enemy") 
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            
            RaycastHit2D hit = Physics2D.Raycast(coll.bounds.center, Vector2.down, 1.3f, enemyLayer);
            if (hit.collider != null || state == State.falling)
            {
                state = State.falling;
                enemy.JumpedOn();
                Jump();
            }
            else
            {
                state = State.hurt;
                if (other.gameObject.transform.position.x > transform.position.x) 
                {
                    rb.velocity = new Vector2(-hurtForce, hurtForce);
                }
                else
                {
                    rb.velocity = new Vector2(hurtForce, hurtForce);
                }
            }
        }
    }
    /**
     * Function that controls movement based on input values. 
     **/
    private void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal");
        bool jumping = Input.GetButtonDown("Jump");
        bool isTouchingGround = coll.IsTouchingLayers(ground);

        //midair movement
        if(state != State.hurt) {
            if (hDirection < 0 && !isTouchingGround)
            {
                rb.velocity = new Vector2(-speed*airControl, rb.velocity.y);
            }
            else if (hDirection > 0 && !isTouchingGround)
            {
                rb.velocity = new Vector2(speed*airControl, rb.velocity.y);
            }

            //moving left
            else if (hDirection < 0 && isTouchingGround)
            {
                rb.velocity = new Vector2(-speed, rb.velocity.y);

            }

            //moving right
            else if (hDirection > 0 && isTouchingGround)
            {
                rb.velocity = new Vector2(speed, rb.velocity.y);
                transform.localScale = new Vector2(1, 1);
            }
            //staying still
            else if (hDirection == 0 && isTouchingGround)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        
        //jumping
        if (jumping)
        {
            if (isTouchingGround)
            {
                Jump();
            }
            // Double jump check
            else if (canDoubleJump && !hasDoubleJumped)
            {
                DoubleJump();
            }
        }

        if (hDirection <0)
        {
            transform.localScale = new Vector2(-1, 1); //sets sprite horizontal flip
        }
        else
        {
            transform.localScale = new Vector2(1, 1);
        }
        
        
    }

    
    private void Jump()
    {
        jumpAudio.Play();
        rb.velocity = new Vector2(rb.velocity.x/2, jumpForce);
        state = State.jumping;
    }

    private void DoubleJump()
    {
        jumpAudio.Play();
        rb.velocity = new Vector2(rb.velocity.x/2, jumpForce * 0.8f); // Slightly reduced jump force for double jump
        state = State.jumping;
        hasDoubleJumped = true;
    }

    /**
     * Function that transitions between animation states as defined in the enumerator
     * animation is set in Update
     **/
    private void AnimationState()
    {
        if (state == State.jumping)
        {
            if (rb.velocity.y < 0.1f)
            {
                state = State.falling;
            }
        }
        else if (state == State.falling)
        {
            if (coll.IsTouchingLayers(ground))
            {
                state = State.idle;
            }
        }
        else if (state == State.hurt)
        {
            if (Mathf.Abs(rb.velocity.x) < 2f)
            {
                state = State.idle;
            }
        }
        else if (Mathf.Abs(rb.velocity.x) > 2f)
        {
            state = State.running;
        }
        else
        {
            state = State.idle;
        }
    }

    private void Footstep()
    {
        footstep.Play();
    }

    private void ActivateSpeedBoost()
    {
        isSpeedBoosted = true;
        speed = (int)boostedSpeed;
        
        // Stop any existing speed boost coroutine
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }
        
        // Start new speed boost coroutine
        speedBoostCoroutine = StartCoroutine(SpeedBoostTimer());
    }

    private IEnumerator SpeedBoostTimer()
    {
        yield return new WaitForSeconds(3f);
        
        // Reset speed and state
        speed = (int)normalSpeed;
        isSpeedBoosted = false;
        gemsCollectedInLevel = 0;
    }

    public void TakeDamage(float damage, Vector2 knockbackForce)
    {
        state = State.hurt;
        rb.velocity = knockbackForce;
        // Add any additional damage effects here (e.g., health reduction, screen shake, etc.)
    }
}
