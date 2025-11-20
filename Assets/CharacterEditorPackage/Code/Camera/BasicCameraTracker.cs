using UnityEngine;
using System.Collections;
//--------------------------------------------------------------------
//Follows the player along the 2d plane, using a continuous lerp
//Plus adds mouse offset within a radius
//--------------------------------------------------------------------
public class BasicCameraTracker : MonoBehaviour {
    [SerializeField] GameObject m_Target = null;
    [SerializeField] float m_InterpolationFactor = 0.0f;
    [SerializeField] bool m_UseFixedUpdate = false;
    [SerializeField] float m_ZDistance = 10.0f;

    [Header("Mouse Follow Settings")]
    [SerializeField] bool m_EnableMouseFollow = true;
    [SerializeField] float m_MouseFollowRadius = 5f;
    [SerializeField] float m_MouseInfluence = 1f;

    private Camera m_Camera;

    void Start()
    {
        m_Camera = GetComponent<Camera>();
    }

	void FixedUpdate () 
	{
        if (m_UseFixedUpdate)
        {
            Interpolate(Time.fixedDeltaTime);
        }
	}

    void LateUpdate()
    {
        if (!m_UseFixedUpdate)
        {
            Interpolate(Time.deltaTime);
        }
    }

    void Interpolate(float a_DeltaTime)
    {
        if (m_Target == null)
        {
            return;
        }

        Vector3 baseTarget = m_Target.transform.position + Vector3.back * m_ZDistance;

        if (m_EnableMouseFollow)
        {
            Vector3 mouseOffset = GetMouseOffset();
            baseTarget += mouseOffset;
        }

        Vector3 diff = baseTarget - transform.position;
        transform.position += diff * m_InterpolationFactor * a_DeltaTime;
    }

    Vector3 GetMouseOffset()
    {
        if (m_Camera == null)
        {
            return Vector3.zero;
        }

        Vector3 mousePos = Input.mousePosition;
        
        // Convert to viewport space (0-1 range)
        Vector3 viewportPos = m_Camera.ScreenToViewportPoint(mousePos);
        
        // Convert to centered coordinates (-1 to 1)
        Vector2 offset = new Vector2(
            (viewportPos.x - 0.5f) * 2f,
            (viewportPos.y - 0.5f) * 2f
        );
        
        offset = Vector2.ClampMagnitude(offset, 1f);
        
        return new Vector3(
            offset.x * m_MouseFollowRadius * m_MouseInfluence,
            offset.y * m_MouseFollowRadius * m_MouseInfluence,
            0f
        );
    }

    void OnDrawGizmosSelected()
    {
        if (m_Target != null && m_EnableMouseFollow)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = m_Target.transform.position + Vector3.back * m_ZDistance;
            Gizmos.DrawWireSphere(center, m_MouseFollowRadius);
        }
    }
}