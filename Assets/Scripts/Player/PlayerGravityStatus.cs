using UnityEngine;

// Added to the player at runtime by Boss when it first casts gravity
// manipulation. Launches the player in a fixed direction (on top of, not
// instead of, their normal control), tints them blue while active, and
// stops the launch when it actually slams into something (not just any
// incidental contact with the ground layer they may already be standing on).
public class PlayerGravityStatus : MonoBehaviour
{
    [SerializeField] Color tintColor = new Color(0.2f, 0.5f, 1f, 1f);

    // Added at runtime (never present in the scene file), so there is no
    // Inspector to configure this in - resolve the ground/wall layer by name
    // instead of relying on a serialized mask nobody can ever set.
    LayerMask wallMask;

    Rigidbody2D rigid;
    SpriteRenderer sprite;
    Color originalColor;

    public bool IsActive { get; private set; }

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        originalColor = sprite.color;
        wallMask = LayerMask.GetMask("ground");
    }

    public void ApplyGravityLaunch(Vector2 direction, float speed)
    {
        IsActive = true;
        sprite.color = tintColor;
        rigid.linearVelocity = direction.normalized * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsActive)
            return;

        if (((1 << collision.gameObject.layer) & wallMask.value) == 0)
            return;

        // The player is often already touching the ground layer when the
        // launch starts (a sideways launch slides right along the floor
        // they're standing on), so any contact with that layer isn't
        // necessarily a block. Only end the launch when the pre-collision
        // relative velocity actually drives into the surface (opposes its
        // normal) rather than sliding tangentially along it.
        Vector2 velocity = collision.relativeVelocity;
        if (velocity.sqrMagnitude < 0.0001f)
            return;

        Vector2 velocityDir = velocity.normalized;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Vector2.Dot(velocityDir, contact.normal) < -0.3f)
            {
                EndGravityLaunch();
                return;
            }
        }
    }

    void EndGravityLaunch()
    {
        IsActive = false;
        sprite.color = originalColor;
        rigid.linearVelocity = Vector2.zero;
    }
}
