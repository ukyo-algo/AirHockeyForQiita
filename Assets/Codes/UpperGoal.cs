using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpperGoal : MonoBehaviour
{
    // Start is called before the first frame update

    public AgentManager agentManager;

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Upper Goal");
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.tag == "Puck")
        {
            // End the episode
            Debug.Log("Upper Goal");
            agentManager.EndEpisode(0);
        }
    }
}
