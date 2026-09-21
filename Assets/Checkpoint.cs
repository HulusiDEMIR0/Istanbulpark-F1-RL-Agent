using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex = 0;
    public bool isFinish = false;

    private void OnTriggerEnter(Collider other)
    {
        F1Agent agent = other.GetComponent<F1Agent>();

        if (agent == null)
            return;

        agent.ReachCheckpoint(checkpointIndex, isFinish);
    }
}