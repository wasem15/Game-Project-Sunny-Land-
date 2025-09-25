using UnityEngine;

public class Fireball : MonoBehaviour
{
    private float speed;
    private float damage;
    private bool isFacingRight;
    private Rigidbody2D rb;
    private ParticleSystem trailEffect;
    private AudioSource hitSound;
    
    // Homing behavior variables
    private float rotationSpeed = 200f; // How fast the projectile rotates to face target
    private float maxHomingDistance = 20f; // Maximum distance to look for enemies
    private LayerMask enemyLayer;
    private Transform target;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        trailEffect = GetComponentInChildren<ParticleSystem>();
        hitSound = GetComponent<AudioSource>();
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        
        // Find initial target
        FindNearestEnemy();
    }

    private void Update()
    {
        if (target == null)
        {
            FindNearestEnemy();
        }
        else
        {
            // Calculate direction to target
            Vector2 direction = (target.position - transform.position).normalized;
            
            // Rotate towards target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move in the direction we're facing
            rb.velocity = transform.right * speed;
        }
    }

    private void FindNearestEnemy()
    {
        // Find all enemies within range
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, maxHomingDistance, enemyLayer);
        
        if (enemies.Length > 0)
        {
            // Find closest enemy
            float closestDistance = float.MaxValue;
            Transform closestEnemy = null;
            
            foreach (Collider2D enemy in enemies)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy.transform;
                }
            }
            
            target = closestEnemy;
        }
    }

    public void Initialize(float projectileSpeed, float projectileDamage, bool facingRight)
    {
        speed = projectileSpeed;
        damage = projectileDamage;
        isFacingRight = facingRight;
        
        // Set initial velocity
        float direction = isFacingRight ? 1f : -1f;
        rb.velocity = new Vector2(speed * direction, 0f);
        
        // Flip sprite if needed
        if (!isFacingRight)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Deal damage to enemy
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            
            // Play hit effects
            if (hitSound != null)
            {
                hitSound.Play();
            }
            
            // Create hit effect
            if (trailEffect != null)
            {
                trailEffect.Stop();
                trailEffect.transform.parent = null;
                Destroy(trailEffect.gameObject, trailEffect.main.duration);
            }
            
            // Destroy fireball
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            // Create impact effect
            if (trailEffect != null)
            {
                trailEffect.Stop();
                trailEffect.transform.parent = null;
                Destroy(trailEffect.gameObject, trailEffect.main.duration);
            }
            
            // Destroy fireball
            Destroy(gameObject);
        }
    }
} 