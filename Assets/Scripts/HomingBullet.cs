using UnityEngine;

public class HomingBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float rotationSpeed = 200f;
    
    private Rigidbody2D rb;
    private Transform target;
    private ParticleSystem trailEffect;
    private AudioSource hitSound;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        trailEffect = GetComponentInChildren<ParticleSystem>();
        hitSound = GetComponent<AudioSource>();
        
        // Set initial velocity
        rb.velocity = transform.right * speed;
        
        // Find initial target
        FindNearestEnemy();
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
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
        // Find all enemies in the scene
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        
        if (enemies.Length > 0)
        {
            // Find closest enemy
            float closestDistance = float.MaxValue;
            Transform closestEnemy = null;
            
            foreach (Enemy enemy in enemies)
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Deal damage to enemy
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Hit enemy! Dealt {damage} damage");
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
            
            // Destroy bullet
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
            
            // Destroy bullet
            Destroy(gameObject);
        }
    }
} 