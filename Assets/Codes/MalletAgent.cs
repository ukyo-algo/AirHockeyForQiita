using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;


public class MalletAgent : Agent
{
    public float maxSpeed = 20.0f; // 最大速度
    public int agentID; // 自身のエージェントID(Mallet_Bottomなら０、Mallet_Topなら１に設定している)
    public GameObject puck; // パックのゲームオブジェクト
    public GameObject enemyMallet;
    public GameObject enemygoal;
    private Vector3 initialPosition;
    private Rigidbody2D rb; // 自身のRigidbody2Dコンポーネント
    private Rigidbody2D puckRB; // パックのRigidbody2Dコンポーネント

    private int is_puck_in_my_area; // パックが自分の陣地にいるかどうかのフラグ(自分の陣地にいるなら0、いないなら1)

    private int was_puck_in_my_area;

    private float dir;// エージェントから見た相手ゴールの方向(Mallet_Bottomなら1.0、Mallet_Topなら-1.0)

    public float totalEnergyLoss = 0.0f; // エネルギーロスの合計

    private bool hitpack = false; // パックに当たったかどうかのフラグ


    public float attackreturnreward = 0.0f; // 攻撃報酬

    public float attackspeedreward = 0.0f; // 攻撃速度報酬

    public float attackdirectionreward = 0.0f; // 攻撃方向報酬
    public float positionreward = 0.0f; // 位置報酬

    public float badcontrolreward = 0.0f; // 悪い制御報酬

    private Vector2 rightfakegoal_transform = new Vector2(6.65f, 5.2f);
    private Vector2 leftfakegoal_transform = new Vector2(6.65f, -5.2f);

    private float attackrewardmagnitude = 0.2f; // 攻撃報酬の大きさ

    public override void Initialize()
    {
        dir = (agentID == 0) ? 1.0f : -1.0f;
        rb = GetComponent<Rigidbody2D>();
        puckRB = puck.GetComponent<Rigidbody2D>();
        initialPosition = transform.localPosition;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = initialPosition;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        puck.GetComponent<PuckController>().Reset();
        is_puck_in_my_area = (puck.transform.localPosition.y * dir >= 0.0f) ? 1 : 0;
        was_puck_in_my_area = is_puck_in_my_area;


        totalEnergyLoss = 0.0f; // エネルギーロスの合計をリセット
        attackreturnreward = 0.0f; // 攻撃報酬をリセット
        attackspeedreward = 0.0f; // 攻撃速度報酬をリセット
        attackdirectionreward = 0.0f; // 攻撃方向報酬をリセット
        positionreward = 0.0f; // 位置報酬をリセット
        badcontrolreward = 0.0f; // 悪い制御報酬をリセット
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 自分の位置に関する情報
        sensor.AddObservation(transform.localPosition.x * dir);
        sensor.AddObservation(transform.localPosition.y * dir);
        
        // 敵の相対位置に関する情報
        Vector2 relative_position_of_enemy = new Vector2(
            (enemyMallet.transform.localPosition.x - transform.localPosition.x) * dir,
            (enemyMallet.transform.localPosition.y - transform.localPosition.y) * dir
        );

        sensor.AddObservation(relative_position_of_enemy.x); // x座標
        sensor.AddObservation(relative_position_of_enemy.y); // y座標

        // パックの相対位置に関する情報
        Vector2 relative_position_of_puck = new Vector2(
            (puck.transform.localPosition.x - transform.localPosition.x) * dir,
            (puck.transform.localPosition.y - transform.localPosition.y) * dir
        );

        sensor.AddObservation(relative_position_of_puck.x); // x座標
        sensor.AddObservation(relative_position_of_puck.y); // y座標

        // 速度情報を取得
        Vector2 puck_velocity = puckRB.velocity * dir; // 自分の座標系に変換
        sensor.AddObservation(puck_velocity.x); // x成分
        sensor.AddObservation(puck_velocity.y); // y成分


        // パックが自分の陣地にいるかどうかの情報
        was_puck_in_my_area = is_puck_in_my_area;
        is_puck_in_my_area = (puck.transform.localPosition.y * dir >= 0.0f) ? 1 : 0;



        // ボールを相手陣地に返すと報酬
        if (was_puck_in_my_area == 0 && is_puck_in_my_area == 1 && hitpack == true)
        {
            AddReward(0.005f * attackrewardmagnitude);
            attackreturnreward += 0.005f * attackrewardmagnitude;
            // パックの速度ベクトルと、ぱっくと敵ゴールの相対位置ベクトルの内積を計算
            AddReward(puck_velocity.magnitude / maxSpeed * 0.01f * attackrewardmagnitude); // パックの速度が大きいほど報酬を増やす
            attackspeedreward += puck_velocity.magnitude / maxSpeed * 0.01f * attackrewardmagnitude; // パックの速度が大きいほど報酬を増やす
            hitpack = false; // フラグをリセット
            
                        Vector2 puckPos = new Vector2(puck.transform.localPosition.x, puck.transform.localPosition.y);
            Vector2 puckDir = puckRB.velocity.normalized;

            // ゴールの位置（Transform → Vector2 に変換）
            Vector2 enemyGoalPos = new Vector2(enemygoal.transform.localPosition.x, enemygoal.transform.localPosition.y);


            // パック → 各ゴールの方向ベクトル
            Vector2 toEnemyGoal = (enemyGoalPos - puckPos).normalized;
            Vector2 toRightFakeGoal = (rightfakegoal_transform - puckPos).normalized;
            Vector2 toLeftFakeGoal = (leftfakegoal_transform - puckPos).normalized;
            float k = -5f; // 減衰の強さ
            float rewardToEnemy = Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toEnemyGoal)));
            float rewardToRight = Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toRightFakeGoal)));
            float rewardToLeft = Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toLeftFakeGoal)));

            // 最大の方向報酬を選ぶ
            float maxValue = Mathf.Max(rewardToEnemy, rewardToRight, rewardToLeft, 0) * puckRB.velocity.magnitude;

            AddReward(maxValue * 0.005f * attackrewardmagnitude); // 内積が大きいほど報酬を増やす
            attackdirectionreward += maxValue * 0.005f * attackrewardmagnitude; // 内積が大きいほど報酬を増やす
        }


    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        float CollisionPenalty = -1.0f;
        // パックに衝突した場合の処理
        if (collision.collider.CompareTag("Puck"))
        {
            AddReward(0.00025f * attackrewardmagnitude);
            attackreturnreward += 0.00025f * attackrewardmagnitude;
            hitpack = true; // パックに当たったというフラグを立てる


        }

        // 壁に衝突した場合の処理
        if (collision.collider.CompareTag("Wall") || collision.collider.CompareTag("UpperWall") || collision.collider.CompareTag("LowerWall"))
        {
            AddReward(CollisionPenalty);
            badcontrolreward += CollisionPenalty;
        }

    }



    public override void OnActionReceived(ActionBuffers actions)
    {
        // x軸・y軸方向の速度成分
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f) * maxSpeed;
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f) * maxSpeed;

        // プレイヤーの制限
        if (gameObject.transform.localPosition.y * dir >= 0.0f && moveY > 0)
        {
            moveY = 0;
            AddReward(-1.00f * Time.deltaTime);
            badcontrolreward += -1.00f * Time.deltaTime;
        }


        // 速度を直接設定
        rb.velocity = new Vector2(dir * moveX, dir * moveY);

        // エネルギーロス報酬とトラッキング
        AddReward((Mathf.Abs(moveX / maxSpeed) * +Mathf.Abs(moveY / maxSpeed)) * -0.0000001f);
        totalEnergyLoss += (Mathf.Abs(moveX / maxSpeed) + Mathf.Abs(moveY / maxSpeed)) * -0.0000001f;

        //ボールが自陣で止まっているとマイナス報酬
        if (is_puck_in_my_area == 0 && puckRB.velocity.magnitude < 0.1f)
        {
            AddReward(-0.0005f * Time.deltaTime);
            badcontrolreward += -0.0005f * Time.deltaTime;
        }
        
    
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = (Input.GetAxis("Horizontal")) * Mathf.Sqrt(2)/2; // 左右の移動量
        continuousActionsOut[1] = (Input.GetAxis("Vertical")) * Mathf.Sqrt(2)/2; // 上下の移動量
    }



}
