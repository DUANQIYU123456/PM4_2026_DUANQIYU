using UnityEngine;

/// <summary>
/// Callout that connects a label to a target point via a bezier curve.
/// Always visible by default. Can be shown/hidden for active scenes.
/// </summary>
public class Callout : MonoBehaviour
{
    [SerializeField, Tooltip("The label/tooltip Transform.")]
    Transform m_Label;

    [SerializeField, Tooltip("The connector line GameObject (BezierCurve).")]
    GameObject m_Curve;

    [SerializeField, Tooltip("Start visible.")]
    bool m_VisibleOnStart = true;

    void Start()
    {
        SetVisible(m_VisibleOnStart);
    }

    public void SetVisible(bool visible)
    {
        if (m_Label != null)
            m_Label.gameObject.SetActive(visible);
        if (m_Curve != null)
            m_Curve.SetActive(visible);
    }

    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
}
