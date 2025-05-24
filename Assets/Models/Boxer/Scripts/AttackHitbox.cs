using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public float lifetime = 0.001f;
    public int damage;
   public BoxingAgent ownerAgent;

  public AnimScript animScript;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        var otherAgent = other.GetComponentInParent<BoxingAgent>();

        // Только если это другой агент, и он в другой команде
        if (otherAgent != null && otherAgent != ownerAgent)
        {

            if (otherAgent.GetComponent<Health>() != null)
            {
                otherAgent.GetComponent<Health>().TakeDamage(damage);
            }

            if (otherAgent.GetComponent<Animator>() != null)
            {
                otherAgent.GetComponent<Animator>().SetTrigger("Damage");
            }

            if (ownerAgent != null)
            {
                ownerAgent.RewardHit();
            }
        }
    }


}
