using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 7.0f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }
    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
    void FixedUpdate()
    {
        switch (m_nState)
        {
            case eState.kIdle:
                // Check if player is too close and transition to HopStart
                if (Vector3.Distance(transform.position, m_player.transform.position) < m_fScaredDistance)
                {
                    m_nState = eState.kHopStart;
                }
                break;
            case eState.kHopStart:
                HopAway();
                break;
            case eState.kHop:
                // Move towards the hop end position.
                if (m_fHopStart + m_fHopTime > Time.time)
                {
                    transform.position = Vector3.MoveTowards(transform.position, m_vHopEndPos, m_fHopSpeed * Time.deltaTime);
                }
                else
                {
                    m_nState = eState.kIdle; // Hop completed, return to idle.
                }
                break;
            case eState.kCaught:
                // The caught logic is already defined in OnTriggerStay2D.
                break;
        }

        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void HopAway()
    {
        m_fHopStart = Time.time; // Record the start time of the hop.
        
        // Basic direction away from the player.
        Vector2 escapeDirection = (transform.position - m_player.transform.position).normalized;

        // Introduce randomness to the escape direction.
        float randomAngle = Random.Range(-60f, 60f); // Random angle range.
        Quaternion rotation = Quaternion.Euler(0, 0, randomAngle);
        Vector2 randomizedDirection = rotation * escapeDirection;

        // Calculate potential end position with randomized direction.
        Vector3 potentialEndPos = transform.position + new Vector3(randomizedDirection.x, randomizedDirection.y, 0) * m_fHopSpeed * m_fHopTime;
        
        // Boolean to keep away from screen boundaries
        Vector3 viewPos = Camera.main.WorldToViewportPoint(potentialEndPos);
        bool tooCloseToBoundary = viewPos.x < 0.05f || viewPos.x > 0.95f || viewPos.y < 0.05f || viewPos.y > 0.95f;

        if (tooCloseToBoundary)
        {
            // Randomly choose between 90 and -90 degrees for the rotation.
            float randomDirection = Random.Range(0, 2) * 180 - 90; // Results in either -90 or 90.
            rotation = Quaternion.Euler(0, 0, randomDirection);

            // Apply the random rotation to the escape direction.
            randomizedDirection = rotation * escapeDirection;
            potentialEndPos = transform.position + new Vector3(randomizedDirection.x, randomizedDirection.y, 0) * m_fHopSpeed * m_fHopTime;
            viewPos = Camera.main.WorldToViewportPoint(potentialEndPos); // Recalculate viewPos for clamping.
        }

        // Clamp end position to screen bounds.
        viewPos.x = Mathf.Clamp01(viewPos.x);
        viewPos.y = Mathf.Clamp01(viewPos.y);
        m_vHopEndPos = Camera.main.ViewportToWorldPoint(viewPos);
        m_vHopEndPos.z = 0; // Ensure z-coordinate stays consistent.

        m_nState = eState.kHop; // Transition to the Hop state.
    }





    
}