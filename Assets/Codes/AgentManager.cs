using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.VisualScripting;

public class AgentManager : MonoBehaviour
{
    public MalletAgent[] mallets;

    public PuckController puck;
 
    void Start()
    {   

    }


    public void Reset()
    {
        // パックの位置、速度をリセットする
        puck.Reset();
        
    }

    public void EndEpisode(int agentID)
    {
        // パックがゴールに入った、または、Maxstep数を超過した時に、エピソードを終了し、ゲームを初期化する
        if (agentID == 0) //上のゴールに入った場合
        {
            mallets[0].AddReward(1.0f);
            mallets[1].AddReward(-1.0f);
            Debug.Log("Player 1 wins!");
        }
        else if (agentID == 1) //下のゴールに入った場合
        {
            mallets[1].AddReward(1.0f);
            mallets[0].AddReward(-1.0f);
            Debug.Log("Player 2 wins!");
        }
        else //どちらのゴールにも入らずにエピソードが終了した場合,もしくは、パックが場外に出た場合
        {
            Debug.Log("Draw!");
        }
        mallets[0].EndEpisode();
        mallets[1].EndEpisode();
        Reset();
    }

    public void PuckOutOfBounds()
    {
        // パックが場外に飛び出した時に、エピソードを終了する
        Debug.Log("Puck Out of Bounds");
        mallets[0].EndEpisode();
        mallets[1].EndEpisode();
        Reset();
    }
}
