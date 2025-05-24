// AnimScript.cs — УНИВЕРСАЛЬНЫЙ КОНТРОЛЛЕР
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class AnimScript : MonoBehaviour
{
    public float moveSpeed = 2f;
    public bool isPlayerControlled = true;

    private Animator animator;
    private Rigidbody rb;
    private Vector3 moveDirection;
    public GameObject hitboxPrefab;
    public Transform hitboxSpawnPoint;

    public bool isInverted = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }
    

     public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

  public void Move(float direction)
    {
        if (IsAttacking()) return; // блокируем движение во время атаки

        moveDirection = new Vector3(0f, 0f, direction);

        // Если персонаж инвертирован — играем противоположные анимации
        if (isInverted)
        {
            animator.SetBool("IsWalkingForward", direction < 0);
            animator.SetBool("IsWalkingBackWard", direction > 0);
        }
        else
        {
            animator.SetBool("IsWalkingForward", direction > 0);
            animator.SetBool("IsWalkingBackWard", direction < 0);
        }

        // Поворот персонажа не меняется (можно убрать вообще, если не нужен)
        if (direction != 0)
            transform.localScale = new Vector3(Mathf.Sign(direction), 1f, 1f);
    }



    public void PlayDamageAnimation()
    {
        animator.SetTrigger("Damage");
    }

    public int damageTake;

    public void SpawnHitbox(int damage)
    {
         GameObject hitbox = Instantiate(hitboxPrefab, hitboxSpawnPoint.position, hitboxSpawnPoint.rotation);
         var hitboxScript = hitbox.GetComponent<AttackHitbox>();

            if (hitboxScript != null)
            {
                hitboxScript.ownerAgent = GetComponent<BoxingAgent>();
                hitboxScript.damage = damage; // напрямую урон
            }
    }

    private float lastAttackTime;
    public float attackCooldown = 1f;

    public void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown || IsBlocking()) return;

        animator.SetTrigger("AttackTrigger");
        StartCoroutine(SpawnHitboxWithDelay(0.3f));
        lastAttackTime = Time.time;
    }

    private IEnumerator SpawnHitboxWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnHitbox(10); 
    }

    public bool IsAttacking()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    public bool IsBlocking()
    {
        return animator.GetBool("IsBlocking");
    }
    
    public void SetBlocking(bool isBlocking)
    {
        animator.SetBool("IsBlocking", isBlocking);
    }


    void Update()
    {  // Debug.Log($"{animator.GetBool("IsBlocking")} :Block!");

        if (!isPlayerControlled) return;

        float move = Input.GetAxisRaw("Horizontal");
        Move(move);

        bool isBlocking = Input.GetMouseButton(1);
        animator.SetBool("IsBlocking", isBlocking);
      // Debug.Log($"{animator.GetBool("IsBlocking")} :Block!");

        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    void FixedUpdate()
    {
          if (IsAttacking())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        rb.linearVelocity = moveDirection.normalized * moveSpeed;
    }
}