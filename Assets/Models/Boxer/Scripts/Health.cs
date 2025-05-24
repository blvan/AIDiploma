using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public BoxingAgent ownerAgent;
    public AnimScript animScript;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"Current health: {currentHealth}!");
        bool isBlocking = animScript != null && animScript.IsBlocking();

        int finalDamage = isBlocking ? amount / 2 : amount;
        currentHealth -= finalDamage;

        if (isBlocking && ownerAgent != null)
        {
            ownerAgent.RewardBlock();
        }

        if (!isBlocking && ownerAgent != null)
        {
            ownerAgent.PenalizeHit();
        }

        if (currentHealth <= 0 && ownerAgent != null)
        {
             Debug.Log($"Dead!");
            ownerAgent.PenalizeDeath();
            Die();
        }

        if (animScript != null)
        {   
            animScript.PlayDamageAnimation();
        }
    }

    public void Die()
    {
        
        if (ownerAgent != null)
        {
            ownerAgent.PenalizeDeath();

            if (ownerAgent.opponent.TryGetComponent(out BoxingAgent oppAgent))
            {
                oppAgent.AddReward(+1f);
                oppAgent.EndEpisode();
            }
        }
    }
}