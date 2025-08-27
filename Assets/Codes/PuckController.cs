using Unity.VisualScripting;
using UnityEngine;

public class PuckController : MonoBehaviour
{
    private Rigidbody2D rb;

    private Vector3 initial_position;

    public AgentManager agentManager;

    public bool Initialize_position_randomly = true;

    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        initial_position = this.transform.localPosition;
    }

    void FixedUpdate()
    {
        // もしパックがコートの外に出たら、AgentManagerのPuckOutOfBoundsを呼び出す
        if (Mathf.Abs(this.transform.localPosition.x) > 6)
        {
            //Debug.Log("Puck Out of Bounds: " + this.transform.localPosition.x);
            agentManager.PuckOutOfBounds();
        }
        else if (Mathf.Abs(this.transform.localPosition.y) > 8)
        {
            //Debug.Log("Puck Out of Bounds: " + this.transform.localPosition.x);
            agentManager.PuckOutOfBounds();
        }
    }

    public void Reset()
    {
        //int plusOrMinus = Random.Range(0, 2) * 2 - 1; //どちら側からスタートするかをランダムに決定
        //Debug.Log("Puck Reset: " + plusOrMinus);
        // Reset the puck
        // パックの初期位置がコートの堺にはならないように修正
        if (!Initialize_position_randomly)
        {
            // どちらの側からスタートするかをランダムに決定するが、初期位置はinitial_positionと同じにする
            int plusOrMinus = Random.Range(0, 2) * 2 - 1; //どちら側からスタートするかをランダムに決定
            this.transform.localPosition = new Vector3(initial_position.x * plusOrMinus, initial_position.y, -1); 
        }
        else
        {
            int plusOrMinus = Random.Range(0, 2) * 2 - 1; //どちら側からスタートするかをランダムに決定
            this.transform.localPosition = new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(1.0f, 4.0f) * plusOrMinus, -1);
        }
   
        this.GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);
        this.GetComponent<Rigidbody2D>().angularVelocity = 0;

    }
}
