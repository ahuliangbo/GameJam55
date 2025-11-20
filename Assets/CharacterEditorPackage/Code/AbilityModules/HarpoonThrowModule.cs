using UnityEngine;
using System.Collections;

//--------------------------------------------------------------------
// Harpoon Throw Module - Throws a harpoon projectile and maintains rope constraint
// Stays active while harpoon is deployed
//--------------------------------------------------------------------
public class HarpoonThrowModule : GroundedControllerAbilityModule
{
    [Header("Harpoon Settings")]
    [SerializeField] GameObject m_HarpoonPrefab;
    [SerializeField] float m_ThrowForce = 20.0f;
    [SerializeField] float m_MaxRopeLength = 15.0f;
    [SerializeField] float m_ThrowCooldown = 0.5f;
    [SerializeField] float m_PullForce = 25.0f;
    [SerializeField] float m_PullCooldown = 1.0f;
    
    [Header("Rope Constraint")]
    [SerializeField] bool m_StopAtRopeLimit = true;
    [SerializeField] float m_RopeDrag = 0.95f; // Multiplier when hitting rope limit
    
    private HarpoonProjectile m_ActiveHarpoon;
    private float m_LastThrowTime;
    private float m_LastPullTime;
    private Vector2 m_ThrowStartPosition;
    
    protected override void ResetState()
    {
        base.ResetState();
        m_ActiveHarpoon = null;
        m_LastThrowTime = -999f;
        m_LastPullTime = -999f;
    }
    
    protected override void StartModuleImpl()
    {
        ThrowHarpoon();
    }
    
    private void ThrowHarpoon()
    {
        if (m_HarpoonPrefab == null)
        {
            Debug.LogError("Harpoon prefab not assigned!");
            return;
        }
        
        // Destroy existing harpoon if any
        DestroyActiveHarpoon();
        
        // Get throw direction from mouse position
        Vector2 playerPos = m_ControlledCollider.transform.position;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - playerPos).normalized;
        
        // Instantiate harpoon
        GameObject harpoonObj = Instantiate(m_HarpoonPrefab, m_ControlledCollider.transform.position, Quaternion.identity);
        m_ActiveHarpoon = harpoonObj.GetComponent<HarpoonProjectile>();
        
        if (m_ActiveHarpoon == null)
        {
            Debug.LogError("HarpoonProjectile component not found on prefab!");
            Destroy(harpoonObj);
            return;
        }
        
        // Initialize and launch harpoon
        m_ActiveHarpoon.Initialize(direction * m_ThrowForce, m_ControlledCollider, m_MaxRopeLength);
        m_ThrowStartPosition = playerPos;
        m_LastThrowTime = Time.fixedTime;
    }
    
    public override void FixedUpdateModule(){
        GroundedCharacterController groundedController = m_CharacterControllerBase as GroundedCharacterController;
        if (groundedController != null)
        {
            groundedController.DefaultUpdateMovement();
        }
        if (m_ActiveHarpoon == null || !m_ActiveHarpoon.IsStuck())
        {
            return;
        }
        // Apply rope constraint
        Vector2 playerPos = m_ControlledCollider.transform.position;
        Vector2 harpoonPos = m_ActiveHarpoon.transform.position;
        float currentDistance = Vector2.Distance(playerPos, harpoonPos);
        
        if (currentDistance > m_MaxRopeLength)
        {
            // Pull player back toward harpoon
            Vector2 directionToHarpoon = (harpoonPos - playerPos).normalized;
            
            if (m_StopAtRopeLimit)
            {
                // Constrain position to rope limit
                Vector2 constrainedPosition = harpoonPos - directionToHarpoon * m_MaxRopeLength;
                m_ControlledCollider.transform.position = constrainedPosition;
                
                // Only restrict velocity component moving away from harpoon
                Vector2 velocity = m_ControlledCollider.GetVelocity();
                float velocityAwayFromHarpoon = Vector2.Dot(velocity, -directionToHarpoon);
                
                if (velocityAwayFromHarpoon > 0)
                {
                    // Remove the component of velocity moving away from harpoon
                    velocity -= (-directionToHarpoon) * velocityAwayFromHarpoon;
                    m_ControlledCollider.SetVelocity(velocity * m_RopeDrag);
                }
            }
        }
    }
    
    protected override void EndModuleImpl()
    {
        Vector2 playerPos = m_ControlledCollider.transform.position;
        Vector2 harpoonPos = m_ActiveHarpoon.transform.position;
        Vector2 directionToHarpoon = (harpoonPos - playerPos).normalized;

        Vector2 velocity = m_ControlledCollider.GetVelocity();
        m_ControlledCollider.SetVelocity(velocity + directionToHarpoon * m_PullForce);
        m_LastPullTime = Time.fixedTime;
        DestroyActiveHarpoon();
    }
    
    public override void InactiveUpdateModule()
    {
        // Clean up if harpoon was destroyed externally
        if (m_ActiveHarpoon == null && m_IsActive)
        {
            m_IsActive = false;
        }
    }
    
    private bool IsPullInputPressed()
    {
        // Check if pull cooldown is active
        if (Time.fixedTime - m_LastPullTime < m_PullCooldown)
        {
            return false;
        }
        
        // Check if pull input is pressed
        if (DoesInputExist("HarpoonPull"))
        {
            ButtonInput pullButton = GetButtonInput("HarpoonPull");
            return pullButton != null && pullButton.m_WasJustPressed;
        }
        else
        {
            return Input.GetMouseButtonDown(1);
        }
    }
    
    public override bool IsApplicable()
    {
        if (m_ActiveHarpoon != null && m_ActiveHarpoon.IsStuck())
        {
            // Check if pull input is pressed (with cooldown check)
            if (IsPullInputPressed())
            {
                return false; // Stop being applicable so pull can take over
            }
            
            return true; // Stay active while harpoon exists (and no pull requested)
        }
        
        // Can throw if no active harpoon and cooldown elapsed
        if (m_ActiveHarpoon != null)
        {
            return true; // Stay active while harpoon exists
        }
        
        if (Time.fixedTime - m_LastThrowTime < m_ThrowCooldown)
        {
            return false;
        }
        
        // Check for left click input
        // Try to get input - if it doesn't exist, we can't throw
        if (!DoesInputExist("HarpoonThrow"))
        {
            // Fallback to direct Unity input for left mouse
            if (Input.GetMouseButton(0))
            {
                return true;
            }
            return false;
        }
        
        ButtonInput throwButton = GetButtonInput("HarpoonThrow");
        if (throwButton != null && throwButton.m_IsPressed)
        {
            return true;
        }
        
        return false;
    }
    
    public override bool CanEnd()
    {
        // Can always end if harpoon doesn't exist
        if (m_ActiveHarpoon == null)
        {
            return true;
        }
        
        // Can end if we're no longer applicable (e.g., pull was requested)
        if (!IsApplicable())
        {
            return true;
        }
        
        return false;
    }
    
    public void DestroyActiveHarpoon()
    {
        if (m_ActiveHarpoon != null)
        {
            Destroy(m_ActiveHarpoon.gameObject);
            m_ActiveHarpoon = null;
        }
    }
    
    // Public getters for other modules
    public HarpoonProjectile GetActiveHarpoon()
    {
        return m_ActiveHarpoon;
    }
    
    public float GetMaxRopeLength()
    {
        return m_MaxRopeLength;
    }
    
    public bool HasActiveHarpoon()
    {
        return m_ActiveHarpoon != null && m_ActiveHarpoon.IsStuck();
    }
    
    public float GetPullCooldownRemaining()
    {
        return Mathf.Max(0, m_PullCooldown - (Time.fixedTime - m_LastPullTime));
    }

    public override string GetSpriteState(){
        return m_CharacterController.GetCurrentSpriteStateForDefault();
    }
}