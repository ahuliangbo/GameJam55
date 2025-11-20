using UnityEngine;
using System.Collections;

//--------------------------------------------------------------------
// Harpoon Projectile - The physical harpoon object
// Flies through the air and sticks to surfaces
// Uses 3D collider for a 2D game
//--------------------------------------------------------------------
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HarpoonProjectile : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] float m_Gravity = 9.81f;
    [SerializeField] LayerMask m_StickToLayers;
    
    [Header("Visual")]
    [SerializeField] LineRenderer m_RopeRenderer;
    [SerializeField] int m_RopeSegments = 20;
    [SerializeField] float m_RopeSag = 0.5f;
    
    
    private Rigidbody m_Rigidbody;
    private bool m_IsStuck;
    protected ControlledCapsuleCollider m_ControlledCollider;
    private float m_SpawnTime;
    private Vector3 m_GravityForce;
    float maxRopeLength = 4f;


    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.useGravity = false; // We'll apply gravity manually
        m_GravityForce = new Vector3(0, -m_Gravity, 0);
        m_SpawnTime = Time.time;
        
        // Setup rope renderer if not assigned
        if (m_RopeRenderer == null)
        {
            m_RopeRenderer = gameObject.AddComponent<LineRenderer>();
            m_RopeRenderer.startWidth = 0.05f;
            m_RopeRenderer.endWidth = 0.05f;
            m_RopeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            m_RopeRenderer.startColor = Color.gray;
            m_RopeRenderer.endColor = Color.gray;
        }
        
        m_RopeRenderer.positionCount = m_RopeSegments;
    }
    
    public void Initialize(Vector2 velocity, ControlledCapsuleCollider m_ControlledCollider, float setMaxRopeLength)
    {
        // Convert 2D velocity to 3D (keep Z at 0)
        Vector3 velocity3D = new Vector3(velocity.x, velocity.y, 0);
        m_Rigidbody.linearVelocity = velocity3D;

        this.m_ControlledCollider = m_ControlledCollider;
        
        maxRopeLength = setMaxRopeLength;
        // Initial rotation based on throw direction
        UpdateRotationFromVelocity(velocity3D);
        
    }
    
    private void UpdateRotationFromVelocity(Vector3 velocity)
    {
        if (velocity.magnitude > 0.1f)
        {
            // Calculate angle in XY plane
            float angle = Mathf.Atan2(velocity.y, velocity.x);
            float angleDeg = (180 / Mathf.PI) * angle - 90; 
            transform.rotation = Quaternion.Euler(0, 0, angleDeg);
        }
    }
    

    private void FixedUpdate()
    {
        
        // Apply manual gravity and update rotation when not stuck
        if (!m_IsStuck)
        {
            // Apply gravity force
            m_Rigidbody.AddForce(m_GravityForce, ForceMode.Acceleration);
            // Continuously rotate to face current velocity direction (following the arc)
            UpdateRotationFromVelocity(m_Rigidbody.linearVelocity);
            
            float currentDistance = Vector2.Distance(m_ControlledCollider.transform.position, transform.position);
            if (currentDistance > maxRopeLength)
            {
                // Get direction from player to harpoon (radial direction)
                Vector2 radialDirection = ((Vector2)m_ControlledCollider.transform.position - (Vector2)transform.position).normalized;

                Vector2 constrainedPosition = (Vector2)m_ControlledCollider.transform.position - radialDirection * maxRopeLength*.99f;
                m_Rigidbody.MovePosition(constrainedPosition);
                radialDirection = new Vector2(radialDirection.x, radialDirection.y-Mathf.Abs(radialDirection.y)*.5f);
                m_Rigidbody.AddForce(radialDirection * 4, ForceMode.Impulse);
                
            }
        }
    }
    private void LateUpdate()
    {
        // Update rope visual
        if (m_ControlledCollider != null && m_RopeRenderer != null)
        {
            // DrawRope(m_ControlledCollider.transform.position, transform.position);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we should stick to this object
        if (!m_IsStuck && ((1 << collision.gameObject.layer) & m_StickToLayers) != 0)
        {
            Debug.Log("Harpoon collided with: " + collision.gameObject.name);
            StickToSurface(collision);
        }
        

    }
    
    private void StickToSurface(Collision collision)
    {
        m_IsStuck = true;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;
        
        // Optional: parent to the object we stuck to (for moving platforms)
        // Store the original scale before parenting

        transform.SetParent(collision.transform, true);

    }
    
    private void DrawRope(Vector2 startPos, Vector2 endPos)
    {
        for (int i = 0; i < m_RopeSegments; i++)
        {
            float t = i / (float)(m_RopeSegments - 1);
            
            // Linear interpolation between start and end
            Vector2 position = Vector2.Lerp(startPos, endPos, t);
            
            // Add sag in the middle (parabolic curve)
            float sag = m_RopeSag * Mathf.Sin(t * Mathf.PI);
            position.y -= sag;
            
            m_RopeRenderer.SetPosition(i, position);
        }
    }
    
    public bool IsStuck()
    {
        return m_IsStuck;
    }
    
    public Vector2 GetStuckPosition()
    {
        return transform.position;
    }
}