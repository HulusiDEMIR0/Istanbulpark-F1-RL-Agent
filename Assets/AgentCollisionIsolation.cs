using UnityEngine;

public class AgentCollisionIsolation : MonoBehaviour
{
    void Start()
    {
        F1Agent[] agents = FindObjectsByType<F1Agent>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int firstIndex = 0; firstIndex < agents.Length; firstIndex++)
        {
            Collider[] firstAgentColliders =
                agents[firstIndex].GetComponentsInChildren<Collider>(true);

            for (int secondIndex = firstIndex + 1;
                 secondIndex < agents.Length;
                 secondIndex++)
            {
                Collider[] secondAgentColliders =
                    agents[secondIndex].GetComponentsInChildren<Collider>(true);

                foreach (Collider firstCollider in firstAgentColliders)
                {
                    foreach (Collider secondCollider in secondAgentColliders)
                    {
                        Physics.IgnoreCollision(firstCollider, secondCollider);
                    }
                }
            }
        }
    }
}
