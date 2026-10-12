using System.Collections.Generic;
using UnityEngine;

public class CircleSegmenter : MonoBehaviour
{
    public int segments = 12; // Default number of segments
    public Vector3 center = new Vector3(625, 467, 1);
    public float radius = 78f;
    public Material lineMaterial;
    public Color lineColor = Color.blue;
    public float lineWidth = 1.5f;

    private List<GameObject> segmentLines = new List<GameObject>();

    public void UpdateSegments(int newSegmentCount)
    {
        // Update the number of segments
        segments = newSegmentCount;

        // Clear existing lines
        foreach (var line in segmentLines)
        {
            Destroy(line);
        }
        segmentLines.Clear();

        // Redraw the segments
        CreateCircleSegments();
    }

    private void CreateCircleSegments()
    {
        if (segments < 1) return;

        float startAngle = 270f;
        float anglePerSegment = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (startAngle + anglePerSegment * i);

            Vector3 edgePoint = new Vector3(
                center.x + radius * Mathf.Cos(angle),
                center.y + radius * Mathf.Sin(angle),
                center.z
            );

            GameObject lineObject = new GameObject($"Segment {i + 1}");
            lineObject.transform.SetParent(this.transform);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;

            lineRenderer.SetPosition(0, center);
            lineRenderer.SetPosition(1, edgePoint);

            segmentLines.Add(lineObject);
        }
    }
}
