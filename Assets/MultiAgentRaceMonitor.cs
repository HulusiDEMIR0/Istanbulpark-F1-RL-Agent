using UnityEngine;
using Unity.MLAgents;
using System.Collections;
using System.Collections.Generic;

public class MultiAgentRaceMonitor : MonoBehaviour
{
    [Header("Success Rule")]
    [SerializeField] private int minimumEpisodesBeforeCheck = 100;
    [SerializeField] private int recentEpisodeWindow = 30;
    [SerializeField] private int maximumCrashesInWindow = 3;

    [Header("Live Status")]
    [SerializeField] private int completedEpisodes = 0;
    [SerializeField] private int completedLaps = 0;
    [SerializeField] private int crashesInRecentWindow = 0;
    [SerializeField] private bool trainingCompleted = false;

    private F1Agent agent;
    private CarController car;
    private readonly Queue<bool> recentEpisodes = new Queue<bool>();

    void Awake()
    {
        agent = GetComponent<F1Agent>();
        car = GetComponent<CarController>();
    }

    public void RecordEpisode(bool crashed)
    {
        if (trainingCompleted)
        {
            return;
        }

        completedEpisodes++;

        if (crashed)
        {
            crashesInRecentWindow++;
        }
        else
        {
            completedLaps++;
        }

        recentEpisodes.Enqueue(crashed);

        if (recentEpisodes.Count > recentEpisodeWindow &&
            recentEpisodes.Dequeue())
        {
            crashesInRecentWindow--;
        }

        ReportStatistics();

        if (completedEpisodes >= minimumEpisodesBeforeCheck &&
            recentEpisodes.Count == recentEpisodeWindow &&
            crashesInRecentWindow <= maximumCrashesInWindow)
        {
            trainingCompleted = true;
            ReportStatistics();

            Debug.Log(
                name + " basarili. Son " + recentEpisodeWindow +
                " episode'da " + crashesInRecentWindow +
                " kaza ile egitim durduruldu."
            );

            StartCoroutine(StopTrainingAfterEpisode());
        }
    }

    void ReportStatistics()
    {
        Academy.Instance.StatsRecorder.Add(
            "MultiAgent/Completed Episodes",
            completedEpisodes,
            StatAggregationMethod.MostRecent
        );
        Academy.Instance.StatsRecorder.Add(
            "MultiAgent/Completed Laps",
            completedLaps,
            StatAggregationMethod.MostRecent
        );
        Academy.Instance.StatsRecorder.Add(
            "MultiAgent/Crashes In Last 30 Episodes",
            crashesInRecentWindow,
            StatAggregationMethod.MostRecent
        );
        Academy.Instance.StatsRecorder.Add(
            "MultiAgent/Training Completed",
            trainingCompleted ? 1f : 0f,
            StatAggregationMethod.MostRecent
        );
    }

    IEnumerator StopTrainingAfterEpisode()
    {
        yield return null;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        if (car != null)
        {
            car.SetAIInput(0f, 0f);
            car.enabled = false;
        }

        if (agent != null)
        {
            agent.enabled = false;
        }
    }
}
