using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;
using System;

public class MalletAgent : Agent
{
    private float maxSpeed = 20.0f; // 最大速度
    public int agentID;// 自身のエージェントID(Mallet_Bottomなら０、Mallet_Topなら１に設定している)

    public GameObject puck;

    public GameObject enemyMallet;

    public GameObject enemygoal;

    private Vector3 initialPosition;
    private Rigidbody2D rb;
    private Rigidbody2D puckRB;

    private PuckController puckController;

    private int is_puck_in_my_area;

    private int was_puck_in_my_area;

    private float dir;

    private int decisionCounter = 0;

    public float totalEnergyLoss = 0.0f; // エネルギーロスの合計

    private bool hitpack = false; // パックに当たったかどうかのフラグ

    public float attackreturnreward = 0.0f; // 攻撃報酬

    public float attackspeedreward = 0.0f; // 攻撃速度報酬

    public float attackdirectionreward = 0.0f; // 攻撃方向報酬
    public float positionreward = 0.0f; // 位置報酬

    public float attackbehindreward = 0.0f; // 攻撃後ろ報酬

    public float badcontrolreward = 0.0f; // 悪い制御報酬

    private Vector2 rightfakegoal_transform = new Vector2(6.65f, 5.2f);
    private Vector2 leftfakegoal_transform = new Vector2(6.65f, -5.2f);

    public float speedreward = 0.0f; // 速度報酬

    public float myvelx = 0.0f; // 自分の速度のx成分
    public float myvely = 0.0f; // 自分の速度のy成分

    private float threthold_y = 1.0f; // y座標のしきい値. y[m]分なら、敵陣に侵入することを許容する(ただし、相手陣地に入っているときは負の報酬を与える)

    public int stateofpuck = 0; // パックの状態を管理する変数 (自分より下にいる、自分と敵の間にいる、敵より上にいる)

    public override void Initialize()
    {
        dir = (agentID == 0) ? 1.0f : -1.0f;
        rb = GetComponent<Rigidbody2D>();
        puckRB = puck.GetComponent<Rigidbody2D>();
        puckController = puck.GetComponent<PuckController>();
        initialPosition = this.transform.localPosition;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        puckController.Reset();
        is_puck_in_my_area = (puck.transform.localPosition.y * dir >= 0.0f) ? 1 : 0;
        was_puck_in_my_area = is_puck_in_my_area;
        totalEnergyLoss = 0.0f; // エネルギーロスの合計をリセット
        attackreturnreward = 0.0f; // 攻撃報酬をリセット
        attackspeedreward = 0.0f; // 攻撃速度報酬をリセット
        attackdirectionreward = 0.0f; // 攻撃方向報酬をリセット
        positionreward = 0.0f; // 位置報酬をリセット
        badcontrolreward = 0.0f; // 悪い制御報酬をリセット
        speedreward = 0.0f; // 速度報酬をリセット
        attackbehindreward = 0.0f; // 攻撃後ろ報酬をリセット
        decisionCounter = 0; // 決定カウンターをリセット
        hitpack = false; // パックに当たったフラグをリセット
        myvelx = 0.0f; // 自分の速度のx成分をリセット
        myvely = 0.0f; // 自分の速度のy成分をリセット
        stateofpuck = 0; // パックの状態をリセット
        this.transform.localPosition = initialPosition;
        stateofpuck = CheckStateOfPuck(); // パックの状態を初期化
    }

    public override void CollectObservations(VectorSensor sensor)
    {
  
        // 自分の位置に関する情報
        sensor.AddObservation(transform.localPosition.x * dir);
        sensor.AddObservation(transform.localPosition.y * dir);


        myvelx = this.GetComponent<Rigidbody2D>().velocity.x;
        myvely = this.GetComponent<Rigidbody2D>().velocity.y;

        // 自分の速度に関する情報
        sensor.AddObservation(myvelx * dir);
        sensor.AddObservation(myvely * dir);



        sensor.AddObservation(enemyMallet.transform.localPosition.x * dir); // x座標
        sensor.AddObservation(enemyMallet.transform.localPosition.y * dir); // y座標



        sensor.AddObservation(puck.transform.localPosition.x * dir); // x座標
        sensor.AddObservation(puck.transform.localPosition.y * dir); // y座標

        // 速度情報を取得
        Vector2 puck_velocity = puckRB.velocity * dir; // 自分の座標系に変換
        sensor.AddObservation(puck_velocity.x); // x成分
        sensor.AddObservation(puck_velocity.y); // y成分

        // パックが自分の陣地にいるかどうかの情報
        was_puck_in_my_area = is_puck_in_my_area;
        is_puck_in_my_area = (puck.transform.localPosition.y * dir >= 0.0f) ? 1 : 0;



        // 自分の速度の大きさに応じて微小な報酬を与える
        float speed = Mathf.Max((Mathf.Sqrt(myvelx * myvelx + myvely * myvely) - 5
         )* 0.0000001f, 0);
        AddReward(speed);
        speedreward += speed;



        // ボールを相手陣地に返すと報酬
        if (was_puck_in_my_area == 0 && is_puck_in_my_area == 1 && hitpack == true)
        {
            AddReward(0.005f);
            attackreturnreward += 0.005f;

            // パックの位置と速度
            Vector2 puckPos = new Vector2(puck.transform.localPosition.x, puck.transform.localPosition.y);
            Vector2 puckDir = puck_velocity.normalized;

            // ゴールの位置（Transform → Vector2 に変換）
            Vector2 enemyGoalPos = new Vector2(enemygoal.transform.localPosition.x, enemygoal.transform.localPosition.y);


            // パック → 各ゴールの方向ベクトル
            Vector2 toEnemyGoal = (enemyGoalPos - puckPos).normalized;
            Vector2 toRightFakeGoal = (rightfakegoal_transform - puckPos).normalized;
            Vector2 toLeftFakeGoal = (leftfakegoal_transform - puckPos).normalized;

            // 指数関数報酬（cosθベース）すこしずれるだけで報酬が減衰するように
            float k = -5f; // 減衰の強さ
            float rewardToEnemy = Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toEnemyGoal)));
            float rewardToRight =  5*Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toRightFakeGoal)));
            float rewardToLeft = 5*Mathf.Exp(k * (1f - Vector2.Dot(puckDir, toLeftFakeGoal)));

            // 最大の方向報酬を選ぶ
            float maxValue = 0.001f * Mathf.Max(rewardToEnemy, rewardToRight, rewardToLeft, 0) * puck_velocity.magnitude;

            AddReward(maxValue); // 内積が大きいほど報酬を増やす
            attackdirectionreward += maxValue; // 内積が大きいほど報酬を増やす
                                                        //Debug.Log("dotProduct" + maxValue* 0.0025f);
            AddReward(puck_velocity.magnitude / maxSpeed * 0.05f); // パックの速度が大きいほど報酬を増やす
            attackspeedreward += puck_velocity.magnitude / maxSpeed * 0.1f; // パックの速度が大きいほど報酬を増やす
            hitpack = false; // フラグをリセット
        }


        // パックのｙ座標がしきい値を超えたら報酬を与える＆しきいちを下回ったら報酬を与えない
        float puckY = puck.transform.localPosition.y * dir;
        int newstateofpuck = CheckStateOfPuck(); // パックの状態を管理する変数 (自分より下にいる、自分と敵の間にいる、敵より上にいる)

        if (newstateofpuck != stateofpuck)
        {

            if (stateofpuck == 1 && newstateofpuck == 0)
            {// パックがじぶんよりしたにいってしまったので防御失敗したと解釈できるので、負の報酬を与える
                AddReward(-0.0f);
                attackbehindreward += -0.0f; // 防御失敗の報酬を与える
            }
            else if (stateofpuck == 1 && newstateofpuck == 2)
            {// パックが敵より上に行ったことから、攻撃のチャンスと解釈できるので、正の報酬を与える
                AddReward(0.05f);
                attackbehindreward += 0.05f; // 攻撃のチャンスの報酬を与える
            }
            stateofpuck = newstateofpuck; // パックの状態を更新
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float current_acceleration_x = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f) * 1.6f;
        float current_acceleration_y = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f) * 1.6f;

        // 速度を直接設定
        rb.velocity += new Vector2(dir * current_acceleration_x, dir * current_acceleration_y);

        // 斜め45度方向の移動速度が最大200cm/sec であるという制約を考慮
        float motor1_velocity = (-rb.velocity.x + rb.velocity.y) / Mathf.Sqrt(2);
        // 2つめのモーターの速度を計算  
        float motor2_velocity = (-rb.velocity.x - rb.velocity.y) / Mathf.Sqrt(2);


        // プレイヤーの制限
        if (gameObject.transform.localPosition.y * dir >= 0 && rb.velocity.y * dir >= 0)
        {
            float StayEnemyZonePenalty = 1.0f;
            int power = 2;
            AddReward(StayEnemyZonePenalty * Time.deltaTime * Mathf.Pow(Mathf.Abs(gameObject.transform.localPosition.y * dir) / threthold_y, power));
            badcontrolreward += StayEnemyZonePenalty * Time.deltaTime * Mathf.Pow(Mathf.Abs(gameObject.transform.localPosition.y * dir) / threthold_y, power);
        }

        // パックの動く範囲を制限
        if (gameObject.transform.localPosition.y * dir >= threthold_y && rb.velocity.y * dir >= 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
        }


        // ブレーキとアクセルを大きく踏むと正の報酬を与えるとする
        AddReward((Mathf.Abs(current_acceleration_x) * +Mathf.Abs(current_acceleration_y)) * 0.0000001f);
        totalEnergyLoss += (Mathf.Abs(current_acceleration_x) + Mathf.Abs(current_acceleration_y)) * 0.0000001f;


        // 最大速度を超えないようにする
        if (Mathf.Abs(motor1_velocity) > maxSpeed)
        {
            motor1_velocity = maxSpeed;
        }
        if (Mathf.Abs(motor2_velocity) > maxSpeed)
        {
            motor2_velocity = maxSpeed;
        }

        //ボールが自陣で止まっているとマイナス報酬
        if (is_puck_in_my_area == 0)
        {
            if (puckRB.velocity.magnitude < 0.1f)
            {
                AddReward(-0.25f * Time.deltaTime);
                badcontrolreward += -0.25f * Time.deltaTime;
            }
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Puck"))
        {
            AddReward(0.0025f);
            attackreturnreward += 0.0025f;
            hitpack = true; // パックに当たったフラグを立てる
        }
        if (collision.collider.CompareTag("Wall") || collision.collider.CompareTag("UpperWall") || collision.collider.CompareTag("LowerWall"))
        {
            float CollisionPenalty = -1.0f;
            AddReward(CollisionPenalty);
            badcontrolreward += CollisionPenalty;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal") * Mathf.Sqrt(2) / 2; // 左右の移動量
        continuousActionsOut[1] = Input.GetAxis("Vertical") * Mathf.Sqrt(2) / 2; // 上下の移動量
    }
    int CheckStateOfPuck()
    {
        float puckY = puck.transform.localPosition.y * dir;
         if (puckY > enemyMallet.transform.localPosition.y * dir)
        {
            return 2; // 敵より上にいる
        }
        else if (puckY < this.transform.localPosition.y * dir)
        {
            return 0; // 自分より下にいる
        }

        else
        {
            return 1; // 自分と敵の間にいる
        }

    }

}
