using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class MonsterFollow : MonoBehaviour
{
    public float speed = 2f;
    public float catchDistance = 1.5f;
    public float updateRate = 0.1f;

    private NavMeshAgent agent;
    private Transform player;
    private float updateTimer;
    private bool caughtPlayer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("Monster needs a NavMeshAgent component.");
            enabled = false;
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("No object with the Player tag was found.");
            enabled = false;
            return;
        }

        player = playerObject.transform;
        agent.speed = speed;
        agent.stoppingDistance = catchDistance;
    }

    void Update()
    {
        if (caughtPlayer || !agent.isOnNavMesh)
            return;

        updateTimer -= Time.deltaTime;

        if (updateTimer <= 0f)
        {
            agent.SetDestination(player.position);
            updateTimer = updateRate;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= catchDistance)
        {
            caughtPlayer = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}