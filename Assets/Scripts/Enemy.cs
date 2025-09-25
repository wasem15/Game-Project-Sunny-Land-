using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] protected float maxHealth = 3f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float invincibilityDuration = 0.5f;
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected GameObject hitEffect;

    protected Animator anim;
    protected Rigidbody2D rb;
    protected AudioSource explosion;
    protected bool isInvincible = false;
    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;

    protected virtual void Start() //allows children to have access, virtual allows override
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        explosion = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        
        // Play hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Play hit sound
        if (hitSound != null && explosion != null)
        {
            explosion.PlayOneShot(hitSound);
        }

        // Visual feedback
        StartCoroutine(HitFlash());

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start invincibility
            StartCoroutine(InvincibilityFrames());
        }
    }

    protected virtual void Die()
    {
        rb.velocity = Vector2.zero;
        anim.SetTrigger("Death");
        if (explosion != null)
        {
            explosion.Play();
        }
        // Disable colliders and other components
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        // Destroy after animation
        Destroy(gameObject, 1f); // Adjust time based on death animation length
    }

    protected IEnumerator HitFlash()
    {
        // Flash white
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    protected IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        // Make sprite semi-transparent
        Color transparent = originalColor;
        transparent.a = 0.5f;
        spriteRenderer.color = transparent;
        
        yield return new WaitForSeconds(invincibilityDuration);
        
        // Reset
        isInvincible = false;
        spriteRenderer.color = originalColor;
    }

    public void JumpedOn()
    {
        TakeDamage(1f); // Jumping on enemy deals 1 damage
    }
}
