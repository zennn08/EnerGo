using UnityEngine;

namespace EnerGo
{
    // Gentle hover for sparkle stars: bobs up and down, pulses in size and turns slowly.
    // ponytail: animates the whole sparkle group at once; split the stars into separate objects if they must twinkle one by one.
    public class Melayang : MonoBehaviour
    {
        public float bob = 0.02f;      // metres, in the model's own units before the target is scaled
        public float speed = 1.6f;
        public float pulse = 0.08f;    // ±8 % size (the pivot is the mound base, so this also breathes the stars outward)
        public float spinDeg = 25f;    // degrees per second

        private Vector3 basePos, baseScale;
        private float seed;

        private void Start()
        {
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            seed = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t) * bob;
            transform.localScale = baseScale * (1f + Mathf.Sin(t * 2.3f) * pulse);
            transform.Rotate(0f, spinDeg * Time.deltaTime, 0f, Space.Self);
        }
    }
}
