using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;



public class BoxingAgent : Agent
{
    public AnimScript animScript;
    public Transform opponent;
    public float arenaWidth = 10f;
    private Health health;
    private Vector3 initialPosition;
    private Vector3 previousPosition;


    //stats
    // === vlastné štatistiky ==========================================
    private int _hits;
    private int _blockAttempts;
    private int _successfulBlocks;
    private bool _winFlag;           // 1 = výhra, 0 = prehra
                                     // =================================================================



    private int comboStep = 0;
    private float lastComboTime = 0f;
    public float comboResetTime = 1.2f; // максимум между ударами

    private float attackCooldown = 0.6f; // ⏱ Кулдаун между ударами
    private float nextAttackTime = 0f;

    public bool isPlayerControlled = false;
    private float episodeStartTime;
    public float maxEpisodeTime = 15f;

    public override void Initialize()
    {
        health = GetComponent<Health>();
        health.ownerAgent = this;
        initialPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        episodeStartTime = Time.time;
        health.ResetHealth();

        // --- reset štatistík ----------------------------------------
        _hits = 0;
        _blockAttempts = 0;
        _successfulBlocks = 0;
        _winFlag = false;
        // -------

        Vector3 startPos = transform.localPosition;

        if (CompareTag("Player"))
            startPos.z = -1f;
        else if (CompareTag("Enemy"))
            startPos.z = 1f;
        else
            startPos.z = Random.Range(-arenaWidth / 2f, arenaWidth / 2f);

        transform.localPosition = startPos;
        animScript.transform.localPosition = startPos;
        previousPosition = transform.position;
    }

    private void LogStats()
    {
        var sr = Academy.Instance.StatsRecorder;

        sr.Add("Hits/PerEpisode", _hits, StatAggregationMethod.MostRecent);

        float blockPct = _blockAttempts > 0
                        ? 100f * _successfulBlocks / _blockAttempts
                        : 0f;
        sr.Add("Blocks/SuccessPct", blockPct, StatAggregationMethod.MostRecent);

        sr.Add("Episode/Length", StepCount, StatAggregationMethod.MostRecent);
        sr.Add("Episode/Win", _winFlag ? 1 : 0, StatAggregationMethod.MostRecent);
    }


    void Update()
    {
        if (isPlayerControlled && Input.GetKeyDown(KeyCode.C))
        {
            PerformComboAttack();
        }

        if (!isPlayerControlled && Time.time - episodeStartTime > maxEpisodeTime)
        {
            AddReward(-0.5f); // штраф за ничью
            LogStats();
            EndEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 selfPos = transform.localPosition;
        Vector3 enemyPos = opponent.localPosition;

        sensor.AddObservation((enemyPos.z - selfPos.z) / arenaWidth);
        sensor.AddObservation(selfPos.z / arenaWidth);
        sensor.AddObservation(Mathf.Sign(enemyPos.z - selfPos.z));
        sensor.AddObservation(health.currentHealth / 100f);
        sensor.AddObservation(Vector3.Distance(selfPos, enemyPos) / arenaWidth);
        sensor.AddObservation(animScript.IsBlocking() ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isPlayerControlled) return;

        int moveAction = actions.DiscreteActions[0];   // 0 = назад, 1 = стоять, 2 = вперёд
        int actAction = actions.DiscreteActions[1];    // 0 = ничего, 1 = атака, 2 = блок

        float move = 0f;
        if (moveAction == 0) move = -1f;
        else if (moveAction == 2) move = 1f;

        animScript.Move(move);

        float distance = Vector3.Distance(transform.position, opponent.position);
        float direction = Mathf.Sign(opponent.position.z - transform.position.z);

        // === Общий пассивный штраф за бездействие ===
        AddReward(-0.0005f);

        // === Штраф за стояние и бездействие ===
        if (Mathf.Abs(move) < 0.01f && actAction == 0)
        {
            AddReward(-0.002f);
        }

        // === Атака ===
        if (actAction == 1)
        {
            animScript.Attack();

            if (distance < 1.2f)
                AddReward(0.01f);
            else
                AddReward(-0.05f); // в пустоту
        }
        else if (actAction == 2)
        {
            animScript.SetBlocking(true);
            if (distance > 2f)
                AddReward(-0.002f);
        }
        else if (actAction == 3)
        {
            PerformComboAttack(); // 💥 теперь только по желанию
        }
        else
        {
            animScript.SetBlocking(false);
        }


        // === Движение к противнику — поощрение ===
        // AddReward(0.002f * move * direction);

        // === Оценка дистанции ===
        if (distance < 1.5f)
            AddReward(-0.002f);
        else if (distance < 4f)
            AddReward(0.001f);

        // === Штраф за неподвижность ===
        float movementDelta = Vector3.Distance(transform.position, previousPosition);
        if (movementDelta < 0.001f)
        {
            AddReward(-0.001f);
        }
        previousPosition = transform.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        float moveInput = Input.GetAxisRaw("Vertical");

        discrete[0] = moveInput < 0 ? 0 : (moveInput > 0 ? 2 : 1);
        discrete[1] = Input.GetMouseButton(0) ? 1 : (Input.GetMouseButton(1) ? 2 : 0);
    }


    public void PerformComboAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        float timeSinceLast = Time.time - lastComboTime;

        if (timeSinceLast > comboResetTime)
            comboStep = 0;

        comboStep++;
        lastComboTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;

        switch (comboStep)
        {
            case 1:
                animScript.PlayAnimation("KickLeft");
                animScript.SpawnHitbox(10);
                break;
            case 2:
                animScript.PlayAnimation("KickRightLow");
                animScript.SpawnHitbox(20);
                break;
            case 3:
                animScript.PlayAnimation("KickRightUp");
                animScript.SpawnHitbox(30);
                AddReward(0.5f); // 🎁 большая награда за завершённую комбинацию!
                comboStep = 0;
                break;
            default:
                comboStep = 0;
                break;
        }
    }





    public void RewardHit()
    {
        _hits++;
        AddReward(+1f);
    }
    public void PenalizeDeath() { _winFlag = false; AddReward(-1f); LogStats(); EndEpisode(); }
    public void RewardBlock()
    {
        _blockAttempts++;
        _successfulBlocks++;   // успешный блок
        AddReward(+0.5f);
    }

    public void SetWinFlag(bool isWinner)
    {
        _winFlag = isWinner;
    }


    public void PenalizeHit()
    {
        _blockAttempts++;      // попытка блока, но удар прошёл
        AddReward(-0.5f);
    }
    public void PenalizeMiss() => AddReward(-0.5f);
}
