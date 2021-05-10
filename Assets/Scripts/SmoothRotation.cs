using static UnityEngine.Mathf;

namespace InterventionPoint
{
    public class SmoothRotation
    {
        private float currentAngle, currentAngularVelocity;

        public float CurrentAngle { set => currentAngle = value; }

        public SmoothRotation(float startAngle) => currentAngle = startAngle;

        public float DampRotationAngle(float target, float smoothTime)
        {
            return currentAngle = SmoothDampAngle(currentAngle, target, ref currentAngularVelocity, smoothTime);
        }
    }
}
