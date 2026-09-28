using UnityEngine;

public class Marble : MonoBehaviour
{
    private Rigidbody rb;
    private bool hasBeenShot;
    public float stopRollThresh = 0.05f;
    public float stopRollDelay = 0.2f;
    private float stopTimer;

    private void Awake()
    {
        hasBeenShot = false;
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Vector3 velocity)
    {
        hasBeenShot = true;
        stopTimer = 0f;
        rb.AddForce(velocity, ForceMode.VelocityChange);
    }

    void Update()
    {
        if (hasBeenShot)
        {
            float currSpeed = rb.linearVelocity.sqrMagnitude;
            if (currSpeed <= stopRollThresh * stopRollThresh)
            {
                stopTimer += Time.deltaTime;

                if (stopTimer >= stopRollDelay)
                {
                    StopMarble();
                }
            }
        }
    }

    private void StopMarble()
    {
        hasBeenShot = false;
        // enter tulpa mode
    }

}
