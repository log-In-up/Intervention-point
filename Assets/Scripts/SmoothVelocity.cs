using static UnityEngine.Mathf;

namespace InterventionPoint
{
    sealed class SmoothVelocity
    {
        private float current, currentVelocity;

        public float SpeedDamping(float target, float smoothTime)
        {
            return current = SmoothDamp(current, target, ref currentVelocity, smoothTime);
        }
    }
}
