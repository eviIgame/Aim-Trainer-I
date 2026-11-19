using UnityEngine;

namespace Tapshot
{
    /// <summary>
    /// A 3D ball that drifts around inside a rectangle (horizontal + vertical) on a plane in front of the player.
    /// Velocity steers toward a new random direction every so often so the path is smooth but unpredictable.
    /// </summary>
    public class TrackTarget : MonoBehaviour
    {
        [SerializeField] Vector2 halfExtents = new Vector2(6f, 2.8f);
        [SerializeField] Vector2 speedRange = new Vector2(2.5f, 6f);
        [SerializeField] Vector2 retargetInterval = new Vector2(0.4f, 1.2f);
        [SerializeField] float steering = 6f;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color baseColor = new Color(1f, 0.478f, 0.102f);
        [SerializeField] Color hitColor = Color.white;

        Vector3 center;
        Vector2 velocity, desiredVelocity;
        float retargetTimer;
        float hitFlash;
        float radius = 0.5f;
        MaterialPropertyBlock block;

        public bool Moving { get; set; } = true;

        public void Configure(Vector3 planeCenter, Vector2 extents, float diameter)
        {
            center = planeCenter;
            center = new Vector3(center.x, center.y+1f, transform.position.z);
            halfExtents = extents;
            radius = diameter * 0.5f;
            transform.localScale = Vector3.one * diameter;
            transform.position = center;
            PickDirection();
            velocity = desiredVelocity;
        }

        void Awake()
        {
            center = transform.position;
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            PickDirection();
            velocity = desiredVelocity;
            ApplyColor(baseColor);
        }

        public void OnHit() => hitFlash = 1f;

        void Update()
        {
            if (hitFlash > 0f)
            {
                hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.deltaTime * 6f);
                ApplyColor(Color.Lerp(baseColor, hitColor, hitFlash));
            }

            if (!Moving) return;

            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f) PickDirection();

            velocity = Vector2.Lerp(velocity, desiredVelocity, 1f - Mathf.Exp(-steering * Time.deltaTime));

            Vector3 local = transform.position - center;
            Vector2 pos = new Vector2(local.x, local.y) + velocity * Time.deltaTime;

            Vector2 limit = new Vector2(Mathf.Max(0.01f, halfExtents.x - radius), Mathf.Max(0.01f, halfExtents.y - radius));
            if (Mathf.Abs(pos.x) > limit.x)
            {
                pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
                velocity.x = -velocity.x;
                desiredVelocity.x = -Mathf.Abs(desiredVelocity.x) * Mathf.Sign(pos.x);
            }
            if (Mathf.Abs(pos.y) > limit.y)
            {
                pos.y = Mathf.Clamp(pos.y, -limit.y, limit.y);
                velocity.y = -velocity.y;
                desiredVelocity.y = -Mathf.Abs(desiredVelocity.y) * Mathf.Sign(pos.y);
            }

            transform.position = center + new Vector3(pos.x, pos.y, 0f);
        }

        void PickDirection()
        {
            float angle = Random.value * Mathf.PI * 2f;
            float speed = Random.Range(speedRange.x, speedRange.y);
            desiredVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            retargetTimer = Random.Range(retargetInterval.x, retargetInterval.y);
        }

        void ApplyColor(Color c)
        {
            if (targetRenderer == null) return;
            block ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
