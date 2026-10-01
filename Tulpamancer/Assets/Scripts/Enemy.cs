using UnityEngine;
using UnityEngine.AI;
public class EnemyAI : MonoBehaviour
{
    public enum TargetType { Player, Marble }

    [Header("Targeting Refs")]
    [SerializeField] private Player player;

    [Header("AI Settings")]
    [SerializeField] private float updateInterval = 0.1f;

    [SerializeField] private NavMeshAgent agent;
    private Transform currTarget;
    private float nextUpdateTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        Transform newTarget = null;


        if (player.IsLaunchingPhase && player.CurrentMarble != null)
        {
            newTarget = player.CurrentMarble.transform;
        }
        else if (player.IsTulpaPhase)
        {
            newTarget = player.transform;
        }

        if (newTarget != null)
        {
            currTarget = newTarget;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(currTarget.position);
            }
        }
    }
}