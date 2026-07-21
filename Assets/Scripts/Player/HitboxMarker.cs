using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HitboxMarker : MonoBehaviour
{
    public string id;
    public Collider2D hb;
    private PlayerCombat pcs;
    private bool wasEnabled;
    public Vector2 localpos;

    private List<Collider2D> results = new();
    private void Reset()
    {
        hb = GetComponent<Collider2D>();
        if (id == null) Debug.Log("Unnamed Hitbox Detected!");
    }

    private void Start()
    {
    }

    void Update()
    {
        if (!wasEnabled && hb.enabled)
        {
            CheckOverlaps();
        }

        wasEnabled = hb.enabled;
    }

    private void Awake()
    {
        localpos = transform.localPosition;

        pcs = GetComponentInParent<PlayerCombat>();
        hb = GetComponent<Collider2D>();
        Debug.Log($"HitboxMarker: Hitbox of id {id} collider disabled");
        hb.enabled = false; // Disable collider immediately.
        if (id == null)
        {
            float randNum = Random.Range(0, 10);
            id = $"Unnamed hitbox {randNum}";
        }
        wasEnabled = false;

    }
    public void Apply(Vector2 localPos, Vector2 size, bool enabled)
    {
        if (hb == null)
            return;

        transform.localPosition = localpos;

        if (hb is BoxCollider2D box)
        {
            box.size = box.size;
            box.enabled = enabled; // Disable/Enable colliders instead of object
        }
        else if (hb is CapsuleCollider2D capsule)
        {
            capsule.size = capsule.size;
            capsule.enabled = enabled;
        }
    }

    public void HandleTriggers(Collider2D other)
    {
        if (gameObject.CompareTag("Hitbox") && other.CompareTag("Hurtbox")) // Includes parry Hitbox
        {
            pcs.OnHitboxEnter(other, id);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggers(other);
    }

    private void CheckOverlaps()
    {
        int count = hb.Overlap(results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] == null) return;
            HandleTriggers(results[i]);
        }
    }
}

