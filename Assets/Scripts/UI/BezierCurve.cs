using UnityEngine;

/// <summary>
/// Draws a bezier curve between two transforms using a LineRenderer.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class BezierCurve : MonoBehaviour
{
    [SerializeField, Tooltip("Start point of the curve.")]
    Transform m_StartPoint;

    [SerializeField, Tooltip("End point of the curve.")]
    Transform m_EndPoint;

    [SerializeField, Tooltip("Scale factor for the start handle.")]
    float m_CurveFactorStart = 1.0f;

    [SerializeField, Tooltip("Scale factor for the end handle.")]
    float m_CurveFactorEnd = 1.0f;

    [SerializeField, Tooltip("Number of segments in the curve.")]
    int m_SegmentCount = 30;

    LineRenderer m_LineRenderer;
    Vector3[] m_ControlPoints = new Vector3[4];
    Vector3 m_LastStartPosition;
    Vector3 m_LastEndPosition;

    void Awake()
    {
        m_LineRenderer = GetComponent<LineRenderer>();
    }

    void OnEnable()
    {
        if (m_LineRenderer == null)
            m_LineRenderer = GetComponent<LineRenderer>();
        DrawCurve();
    }

    void LateUpdate()
    {
        DrawCurve();
    }

    [ContextMenu("Draw")]
    public void DrawCurve()
    {
        if (m_StartPoint == null || m_EndPoint == null || m_LineRenderer == null)
            return;

        var startPos = m_StartPoint.position;
        var endPos = m_EndPoint.position;

        if (startPos == m_LastStartPosition && endPos == m_LastEndPosition)
            return;

        var dist = Vector3.Distance(startPos, endPos);

        m_ControlPoints[0] = startPos;
        m_ControlPoints[1] = startPos + m_StartPoint.right * (dist * m_CurveFactorStart);
        m_ControlPoints[2] = endPos - m_EndPoint.right * (dist * m_CurveFactorEnd);
        m_ControlPoints[3] = endPos;

        m_LineRenderer.positionCount = m_SegmentCount + 1;
        for (var i = 0; i <= m_SegmentCount; i++)
        {
            var t = i / (float)m_SegmentCount;
            m_LineRenderer.SetPosition(i, CubicBezier(t, m_ControlPoints[0], m_ControlPoints[1], m_ControlPoints[2], m_ControlPoints[3]));
        }

        m_LastStartPosition = startPos;
        m_LastEndPosition = endPos;
    }

    static Vector3 CubicBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }
}
