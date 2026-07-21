using UnityEngine;

public class SpriteConfig : MonoBehaviour
{
    [SerializeField] PlayerMovement pms;
    private PlayerCombat pcs;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        pcs = GetComponentInParent<PlayerCombat>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void ChangeAnim(string anim)
    {
        pms.ChangeAnimation(anim);
    }

    public void ResetAction()
    {
        pcs.ResetActionState();
    }
}
