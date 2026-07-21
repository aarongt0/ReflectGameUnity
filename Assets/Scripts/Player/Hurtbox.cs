using NUnit.Framework.Internal;
using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    public string id;
    private Health health;
    private Logger logger;
    private PlayerCombat pcs;

    void Awake()
    {
        health = GetComponentInParent<Health>(); 
        logger = GetComponentInParent<Logger>();
        pcs = GetComponentInParent<PlayerCombat>();
    }
    void OnGUI()
    {
        if (id == null) return;

        // Display player name/id
    }
}

