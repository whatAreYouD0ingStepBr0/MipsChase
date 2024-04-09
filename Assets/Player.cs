using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 4.0f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 2f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.3f;
    public float m_fFastRotateSpeed = 3.0f;
    public float m_fFastRotateMax = 0.69f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);
        vScreenPos.z = 0; // Ensure we're working in 2D.

        float distanceToMouse = Vector3.Distance(transform.position, vScreenPos);

        // Adjust target speed based on distance to the mouse cursor.
        const float minDistanceForMaxSpeed = 2.0f; // Minimum distance for mouse cursor to reach max speed.
        const float stopThreshold = 0.5f; 

        if (distanceToMouse > minDistanceForMaxSpeed)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (distanceToMouse <= stopThreshold)
        {
            // Close enough to consider stopping.
            m_fTargetSpeed = 0f;
        }
        else
        {
            // Adjust target speed decreases as the player approaches the cursor
            float normalizedDistance = (distanceToMouse - stopThreshold) / (minDistanceForMaxSpeed - stopThreshold);
            m_fTargetSpeed = Mathf.Lerp(0, m_fMaxSpeed, normalizedDistance);
        }

        // Calculate the target angle towards the mouse cursor.
        Vector2 directionToMouse = (vScreenPos - transform.position).normalized;
        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;
    }

    void FixedUpdate()
    {
        UpdateDirectionAndSpeed();

        // Lerp update m_fSpeed to approach m_fTargetSpeed.
        m_fSpeed = Mathf.Lerp(m_fSpeed, m_fTargetSpeed, Time.deltaTime * m_fIncSpeed); 

        CheckForDive();

        // State transition logic based on speed.
        if (m_nState != eState.kDiving && m_nState != eState.kRecovering)
        {
            if (m_fSpeed >= m_fMaxSpeed * 0.95f) // at least 95% of max speed "Move Fast"
            {
                m_nState = eState.kMoveFast;
            }
            else
            {
                m_nState = eState.kMoveSlow;
            }
        }

        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        switch (m_nState)
        {
            case eState.kMoveSlow:
                MoveSlow();
                break;
            case eState.kMoveFast:
                MoveFast();
                break;
            case eState.kDiving:
                Dive();
                break;
            case eState.kRecovering:
                Recover();
                break;
        }
    }

    void MoveTowardsMouse(float speed)
    {
        // Calculate mouse position in world space.
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        mouseWorldPosition.z = 0; // Ensure that the target position is not affected by the z value.

        // Calculate the direction from the player to the mouse position.
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;

        // Update the angle smoothly towards the target angle.
        Quaternion targetRotation = Quaternion.Euler(0, 0, m_fTargetAngle);
        
        if (m_nState == eState.kMoveSlow)
        {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_fFastRotateSpeed * Time.deltaTime * 360);
        }
        
        if (m_nState == eState.kMoveFast)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_fFastRotateMax * Time.deltaTime * 360);

        }
        // Move the player towards the mouse position.
        transform.position = Vector3.MoveTowards(transform.position, transform.position + direction, speed * Time.deltaTime);


        // Ensure the player is within the screen bounds.
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        viewPos.x = Mathf.Clamp01(viewPos.x);
        viewPos.y = Mathf.Clamp01(viewPos.y);
        transform.position = Camera.main.ViewportToWorldPoint(viewPos);
    }

    void MoveSlow()
    {
        MoveTowardsMouse(m_fSlowSpeed);
    }

    void MoveFast()
    {
        MoveTowardsMouse(m_fMaxSpeed);
    }

    void Dive()
    {
        // Calculate the lerp value based on the time since the dive started.
        float diveProgress = (Time.time - m_fDiveStartTime) / m_fDiveTime;
        transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, diveProgress);

        // When the dive is complete, switch to recovering state.
        if (diveProgress >= 1.0f)
        {
            m_nState = eState.kRecovering;
            m_fDiveStartTime = Time.time; // Reset the timer for recovery.
        }
    }

    void Recover()
    {
        // Recovery is just a timer-based state with no movement.
        if ((Time.time - m_fDiveStartTime) >= m_fDiveRecoveryTime)
        {
            m_nState = eState.kMoveSlow; // Go back to slow movement.
        }
    }



}
