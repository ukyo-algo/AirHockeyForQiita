using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowerGoal : MonoBehaviour
{
    // Start is called before the first frame update

    public AgentManager agentManager;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Puck")
        {
            // End the episode
            agentManager.EndEpisode(1);
        }
    }
}