using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

namespace JKress.AITrainer
{
    public class WalkerAgent : Agent
    {
        [Header("Training Mode")]
        public bool earlyTraining = false;

        [Header("Target Goal (not used)")]
        [SerializeField] Transform targetT; // оставим на всякий случай
        [SerializeField] TargetController targetController;

        [Header("Enemy")]
        public Transform enemyTransform;
        public WalkerAgent enemyAgent;

        [Header("Body Parts")]
        [SerializeField] Transform hips;
        [SerializeField] Transform spine;
        [SerializeField] Transform head;
        [SerializeField] Transform thighL;
        [SerializeField] Transform shinL;
        [SerializeField] Transform footL;
        [SerializeField] Transform thighR;
        [SerializeField] Transform shinR;
        [SerializeField] Transform footR;
        [SerializeField] Transform armL;
        [SerializeField] Transform forearmL;
        [SerializeField] Transform armR;
        [SerializeField] Transform forearmR;

        [Header("Stabilizer")]
        [Range(1000, 4000)] [SerializeField] float m_stabilizerTorque = 4000f;
        [SerializeField] Stabilizer hipsStabilizer;
        [SerializeField] Stabilizer spineStabilizer;

        [Header("Walk Speed")]
        [Range(0.1f, 4)] [SerializeField] float m_TargetWalkingSpeed = 2;

        public bool randomizeWalkSpeedEachEpisode;

        OrientationCubeController m_OrientationCube;
        JointDriveController m_JdController;

        private float health = 1f;
        private float idleTimer = 0f;

        private float episodeTimer = 0f;
        private float maxEpisodeDuration = 30f; // секунда

        public override void Initialize()
        {
            m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
            m_JdController = GetComponent<JointDriveController>();

            m_JdController.SetupBodyPart(hips);
            m_JdController.SetupBodyPart(spine);
            m_JdController.SetupBodyPart(head);
            m_JdController.SetupBodyPart(thighL);
            m_JdController.SetupBodyPart(shinL);
            m_JdController.SetupBodyPart(footL);
            m_JdController.SetupBodyPart(thighR);
            m_JdController.SetupBodyPart(shinR);
            m_JdController.SetupBodyPart(footR);
            m_JdController.SetupBodyPart(armL);
            m_JdController.SetupBodyPart(forearmL);
            m_JdController.SetupBodyPart(armR);
            m_JdController.SetupBodyPart(forearmR);

            hipsStabilizer.uprightTorque = m_stabilizerTorque;
            spineStabilizer.uprightTorque = m_stabilizerTorque;
        }

        public override void OnEpisodeBegin()
        {
            health = 1f;
            idleTimer = 0f;
            episodeTimer = 0f;

            foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
            {
                bodyPart.Reset(bodyPart);
            }

            Vector3 toEnemy = (enemyTransform.position - hips.position).normalized;
            hips.rotation = Quaternion.LookRotation(new Vector3(toEnemy.x, 0, toEnemy.z));
            UpdateOrientationObjects();
        }

        void FixedUpdate()
        {
            episodeTimer += Time.fixedDeltaTime;
            if (episodeTimer > maxEpisodeDuration)
            {
                AddReward(-1f);
                EndEpisode();
            }

            if (health <= 0f)
            {
                SetReward(-1f);
                EndEpisode();
            }

            UpdateOrientationObjects();

            float distanceToEnemy = Vector3.Distance(GetAvgPosition(), enemyTransform.position);
            AddReward(-0.0005f * distanceToEnemy);

            float uprightReward = Vector3.Dot(transform.up, Vector3.up);
            AddReward(uprightReward * 0.03f);

            Vector3 dirToEnemy = (enemyTransform.position - GetAvgPosition()).normalized;
            float targetSpeed = m_TargetWalkingSpeed;
            float projectedVelocity = Vector3.Dot(GetAvgVelocity(), dirToEnemy);
            float speedDiff = Mathf.Abs(targetSpeed - projectedVelocity);
            float speedMatchReward = 1f - Mathf.Clamp01(speedDiff / targetSpeed);
            AddReward(speedMatchReward * 0.02f);

            float movementSpeed = GetAvgVelocity().magnitude;
            if (movementSpeed < 0.2f)
            {
                AddReward(-0.005f);
                idleTimer += Time.fixedDeltaTime;
                if (idleTimer > 3f)
                {
                    AddReward(-0.1f);
                    EndEpisode();
                }
            }
            else
            {
                idleTimer = 0f;
            }

            float angle = Vector3.Angle(transform.up, Vector3.up);
            if (angle > 50f)
            {
                AddReward(-0.01f);
            }

            if (distanceToEnemy > 5f)
            {
                AddReward(-0.01f);
            }

            AddReward(0.001f); // за каждый шаг — выжил
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var avgVel = GetAvgVelocity();
            sensor.AddObservation(avgVel);
            sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(enemyTransform.position));
            sensor.AddObservation(health);

            foreach (var bodyPart in m_JdController.bodyPartsList)
            {
                sensor.AddObservation(bodyPart.rb.linearVelocity);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var bpDict = m_JdController.bodyPartsDict;
            var continuousActions = actionBuffers.ContinuousActions;
            int i = -1;

            bpDict[spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            bpDict[thighL].SetJointTargetRotation(continuousActions[++i], 0, continuousActions[++i]);
            bpDict[thighR].SetJointTargetRotation(continuousActions[++i], 0, continuousActions[++i]);
            bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
            bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);
            bpDict[footR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            bpDict[footL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            bpDict[armL].SetJointTargetRotation(continuousActions[++i], 0, continuousActions[++i]);
            bpDict[armR].SetJointTargetRotation(continuousActions[++i], 0, continuousActions[++i]);
            bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
            bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);
            bpDict[head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

            foreach (var part in m_JdController.bodyPartsList)
            {
                bpDict[part.rb.transform].SetJointStrength(continuousActions[++i]);
            }
        }

        public override void Heuristic(in ActionBuffers actionBuffers)
        {
            // Для ручного теста (опционально)
        }

        void UpdateOrientationObjects()
        {
            m_OrientationCube.UpdateOrientation(hips, enemyTransform);
        }

        Vector3 GetAvgVelocity()
        {
            Vector3 sum = Vector3.zero;
            foreach (var bp in m_JdController.bodyPartsList)
            {
                sum += bp.rb.linearVelocity;
            }
            return sum / m_JdController.bodyPartsList.Count;
        }

        Vector3 GetAvgPosition()
        {
            Vector3 sum = Vector3.zero;
            foreach (var bp in m_JdController.bodyPartsList)
            {
                sum += bp.rb.position;
            }
            return sum / m_JdController.bodyPartsList.Count;
        }
    }
}
