using System.Collections;
using UnityEngine;

public class PlayerGravityStatus : MonoBehaviour
{
    [SerializeField] Color tintColor = new Color(0.2f, 0.5f, 1f, 1f);
    [SerializeField] float maxTintDuration = 1.2f;

    LayerMask wallMask;

    Rigidbody2D rigid;
    SpriteRenderer sprite;
    Color originalColor;
    Coroutine tintTimeout;

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

        if (tintTimeout != null)
            StopCoroutine(tintTimeout);
        tintTimeout = StartCoroutine(EndAfter(maxTintDuration));
    }

    IEnumerator EndAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        EndGravityLaunch();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsActive)
            return;

        if (((1 << collision.gameObject.layer) & wallMask.value) == 0)
            return;

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
        if (!IsActive)
            return;

        IsActive = false;
        sprite.color = originalColor;
        rigid.linearVelocity = Vector2.zero;

        if (tintTimeout != null)
        {
            StopCoroutine(tintTimeout);
            tintTimeout = null;
        }
    }
}
