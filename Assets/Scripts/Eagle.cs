using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Eagle : Enemy
{
    [Header("Basic Settings")]
    [SerializeField] private Transform firePoint;
    private Transform player;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float shootCD = 3f;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private AudioSource shootSound;

    [Header("Dash Attack Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashCooldown = 4f;
    [SerializeField] private float dashDamage = 2f;
    [SerializeField] private float dashKnockbackForce = 10f;
    [SerializeField] private float windupDuration = 0.8f;
    [SerializeField] private float recoveryDuration = 1f;
    [SerializeField] private float dashDistance = 8f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject windupEffect;
    [SerializeField] private GameObject dashTrailEffect;
    [SerializeField] private Color windupColor = new Color(1f, 0.5f, 0.5f, 1f);
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip windupSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip recoverySound;

    [Header("UI")]
    [SerializeField] private GameObject dashTutorialPrefab;
    private bool tutorialShown = false;

    // State tracking
    private bool isInLevel1 = false;
    private bool isCharging = false;
    private bool isInCooldown = false;
    private bool isInWindup = false;
    private bool isInRecovery = false;
    private Vector2 chargeDirection;
    private Coroutine currentDashCoroutine;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float originalSpeed;

    new void Start() {
        base.Start();
        player = GameObject.Find("Player").transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalSpeed = dashSpeed;
        
        // Check if we're in Level1
        isInLevel1 = SceneManager.GetActiveScene().name == "Level1";
        
        if (isInLevel1 && !tutorialShown) {
            ShowDashTutorial();
        }
        
        // Start shooting if in Level1
        if (isInLevel1) {
            StartShooting();
        }
    }

    private void StartShooting() {
        if (firePoint == null) {
            Debug.LogError("Fire Point not assigned to Eagle!");
            return;
        }
        if (bulletPrefab == null) {
            Debug.LogError("Bullet Prefab not assigned to Eagle!");
            return;
        }
        
        // Start shooting with cooldown
        InvokeRepeating("Shoot", 0f, shootCD);
    }

    private void Shoot() {
        if (player == null || firePoint == null || bulletPrefab == null) return;

        float distanceToPlayer = Vector2.Distance(player.position, firePoint.position);
        
        if (distanceToPlayer <= shootRange) {
            // Calculate direction to player
            Vector2 direction = (player.position - firePoint.position).normalized;
            
            // Create bullet
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Fireball fireballScript = bullet.GetComponent<Fireball>();
            
            if (fireballScript != null) {
                // Set bullet properties
                fireballScript.Initialize(bulletSpeed, 1f, direction.x > 0);
            }
            
            // Play shoot sound
            if (shootSound != null) {
                shootSound.Play();
            }

            // In Level1, perform dash attack after shooting
            if (isInLevel1 && !isInCooldown && !isCharging && !isInWindup && !isInRecovery) {
                StartDashAttack();
            }
        }
    }

    private void ShowDashTutorial() {
        if (dashTutorialPrefab != null) {
            GameObject tutorial = Instantiate(dashTutorialPrefab, Vector3.zero, Quaternion.identity);
            tutorialShown = true;
            Destroy(tutorial, 5f);
        }
    }

    private void StartDashAttack() {
        if (currentDashCoroutine != null) {
            StopCoroutine(currentDashCoroutine);
        }
        currentDashCoroutine = StartCoroutine(DashAttackSequence());
    }

    private IEnumerator DashAttackSequence() {
        // Windup Phase
        isInWindup = true;
        if (windupEffect != null) {
            windupEffect.SetActive(true);
        }
        if (audioSource != null && windupSound != null) {
            audioSource.PlayOneShot(windupSound);
        }
        spriteRenderer.color = windupColor;
        
        // Calculate direction to player
        chargeDirection = (player.position - transform.position).normalized;
        
        yield return new WaitForSeconds(windupDuration);
        
        // Dash Phase
        isInWindup = false;
        isCharging = true;
        if (windupEffect != null) {
            windupEffect.SetActive(false);
        }
        if (dashTrailEffect != null) {
            dashTrailEffect.SetActive(true);
        }
        if (audioSource != null && dashSound != null) {
            audioSource.PlayOneShot(dashSound);
        }
        
        float dashTimer = 0f;
        Vector2 startPosition = transform.position;
        
        while (dashTimer < dashDuration && Vector2.Distance(startPosition, transform.position) < dashDistance) {
            rb.velocity = chargeDirection * dashSpeed;
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        // Recovery Phase
        isCharging = false;
        if (dashTrailEffect != null) {
            dashTrailEffect.SetActive(false);
        }
        isInRecovery = true;
        if (audioSource != null && recoverySound != null) {
            audioSource.PlayOneShot(recoverySound);
        }
        
        yield return new WaitForSeconds(recoveryDuration);
        
        // Reset state
        isInRecovery = false;
        spriteRenderer.color = originalColor;
        
        // Start cooldown
        isInCooldown = true;
        yield return new WaitForSeconds(dashCooldown);
        isInCooldown = false;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player") && isCharging) {
            // Deal damage to player
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null) {
                // Calculate knockback direction
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                Vector2 knockbackForce = knockbackDirection * dashKnockbackForce;
                
                // Apply damage and knockback
                player.TakeDamage(dashDamage, knockbackForce);
            }
        }
    }

    private void Update() {
        // Update visual effects based on state
        if (isInWindup) {
            // Add any additional windup visual effects here
        }
        
        if (isInRecovery) {
            // Add any additional recovery visual effects here
        }
    }
}
