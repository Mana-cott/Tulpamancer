using UnityEngine;

public class ShotRange : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;
    }

    public void DrawCircle(Vector3 center, float radius, int segments = 64)
    {
        lineRenderer.positionCount = segments;
        float deltaAngle = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * deltaAngle * Mathf.Deg2Rad;
            Vector3 point = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + 0.02f, center.z + Mathf.Sin(angle) * radius);
            lineRenderer.SetPosition(i, point);
        }

        lineRenderer.enabled = true;
    }

    public void HideCircle()
    {
        lineRenderer.enabled = false;
    }

}
