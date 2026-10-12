using UnityEngine;

public class CircleSegmenterInspector : MonoBehaviour
{
    public Vector3 center = new Vector3(625, 467, 1); // Center of the circle
    public float radius = 78f; // Radius of the circle
    public int segments = 12; // Number of segments
    public Color gizmoColor = Color.blue; // Color for Gizmos

    private void OnDrawGizmos()
    {
        if (segments < 1) return; // Avoid errors if segments are less than 1

        Gizmos.color = gizmoColor;

        // Start the first segment angle at 270° to ensure the downward line
        float startAngle = 270f;
        float anglePerSegment = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            // Calculate the angle for the current segment
            float angle = Mathf.Deg2Rad * (startAngle + anglePerSegment * i);

            // Calculate the edge point
            Vector3 edgePoint = new Vector3(
                center.x + radius * Mathf.Cos(angle),
                center.y + radius * Mathf.Sin(angle),
                center.z
            );

            // Draw a line from the center to the edge point
            Gizmos.DrawLine(center, edgePoint);
        }
    }
}
