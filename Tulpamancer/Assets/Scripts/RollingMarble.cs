using UnityEngine;

public class RollingMarble : MonoBehaviour
{
    [SerializeField] public CharacterController controller;
    [SerializeField] public float radius = 0.5f;
    private Vector3 lastPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<CharacterController>();
        }

        if (controller != null)
        {
            lastPos = controller.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (controller == null) return;

        Vector3 worldDelta = controller.transform.position - lastPos;
        worldDelta.y = 0f;

        float dist = worldDelta.magnitude;

        if (dist > 0.0001f)
        {
            float rotationAngle = (dist / radius) * Mathf.Rad2Deg;

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, worldDelta.normalized);

            transform.Rotate(rotationAxis, rotationAngle, Space.World);
        }

        lastPos = controller.transform.position;
    }
}
