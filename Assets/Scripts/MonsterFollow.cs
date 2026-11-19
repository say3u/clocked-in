using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class MonsterFollow : MonoBehaviour
{
    public float speed = 2f;
    public float catchDistance = 1.5f;

    NavMeshAgent agent;
    Transform player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        agent.speed = speed;
        agent.SetDestination(player.position);

        // caught the player, restart the level
        if (Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}