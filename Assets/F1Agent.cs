using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using System.Collections.Generic;
using System.IO;

public class F1Agent : Agent
{
    [Header("References")]
    public CarController car;
    public F1SensorSystem sensors;

    [Header("Checkpoint System")]
    public List<Transform> checkpoints;

    [Header("Reward Settings")]
    public float maxSpeed = 20f;

    private Rigidbody rb;
    private MultiAgentRaceMonitor multiAgentRaceMonitor;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private int nextCheckpointIndex = 0;

    [Header("Lap Statistics")]
    [SerializeField] private int completedLapCount = 0;
    [SerializeField] private int firstCompletedLapAcademyStep = -1;

    [Header("Speed Curriculum")]
    [SerializeField] private bool enableSpeedCurriculum = true;
    [SerializeField] private int successfulLapsPerSpeed = 100;
    [SerializeField] private float[] speedStages = { 35f, 50f, 65f, 80f, 95f };
    [SerializeField] private float baseFrontSensorLength = 40f;
    [SerializeField] private float baseFarGroundSensorLength = 8f;
    [SerializeField] private float frontSensorIncreasePerStage = 10f;
    [SerializeField] private float farGroundSensorIncreasePerStage = 2f;
    [SerializeField] private string trainingRunId = "IstanbulPark_v2";
    [SerializeField] private string trainingBehaviorName = "IstanbulParkAgent";
    [SerializeField] private string initial20KmhModelStep = "1437844";
    [SerializeField] private string modelArchiveDirectory =
        @"C:\Users\05hde\OneDrive\Ekler\Desktop\IstanbulPark\_v2\IstanbulParkAgent";

    [Header("Speed Curriculum Live Status")]
    [SerializeField] private int currentSpeedStageIndex = 0;
    [SerializeField] private int successfulLapsAtCurrentSpeed = 0;
    [SerializeField] private float currentCurriculumSpeed = 35f;
    [SerializeField] private float currentFrontSensorLength = 50f;
    [SerializeField] private float currentFarGroundSensorLength = 10f;
    [SerializeField] private bool archivePending = false;
    [SerializeField] private bool initial20KmhModelArchived = false;
    [SerializeField] private bool curriculumCompleted = false;
    private long archiveRequestedUtcTicks = 0;
    private float nextArchiveCheckTime = 0f;

    [Serializable]
    private class SpeedCurriculumState
    {
        public int currentSpeedStageIndex;
        public int successfulLapsAtCurrentSpeed;
        public bool archivePending;
        public bool initial20KmhModelArchived;
        public bool curriculumCompleted;
        public long archiveRequestedUtcTicks;
    }

    private class ModelSnapshot
    {
        public string onnxPath;
        public string ptPath;
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        if (car == null)
            car = GetComponent<CarController>();

        if (sensors == null)
            sensors = GetComponent<F1SensorSystem>();

        multiAgentRaceMonitor = GetComponent<MultiAgentRaceMonitor>();

        startPosition = transform.position;
        startRotation = transform.rotation;

        completedLapCount = 0;
        firstCompletedLapAcademyStep = -1;

        InitializeSpeedCurriculum();
    }

    void Update()
    {
        if (!enableSpeedCurriculum || !archivePending ||
            Time.unscaledTime < nextArchiveCheckTime)
        {
            return;
        }

        nextArchiveCheckTime = Time.unscaledTime + 5f;
        TryCompleteSpeedTransition();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startPosition;
        transform.rotation = startRotation;

        car.ResetCar();

        // Her bölüm başında indeksi sıfırlıyoruz
        nextCheckpointIndex = 0;
    }

    // =========================================================
    // AI'A VERİLEN BİLGİLER
    // =========================================================

    public override void CollectObservations(
        VectorSensor sensor)
    {
        // -----------------------------
        // HEDEF CHECKPOINT YÖNÜ (YENİ EKLENDİ)
        // -----------------------------

        if (checkpoints != null &&
            checkpoints.Count > 0 &&
            nextCheckpointIndex < checkpoints.Count)
        {
            Vector3 directionToCheckpoint =
                (checkpoints[nextCheckpointIndex].position - transform.position).normalized;

            Vector3 localDirection =
                transform.InverseTransformDirection(directionToCheckpoint);

            sensor.AddObservation(
                localDirection.x
            );

            sensor.AddObservation(
                localDirection.z
            );
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // -----------------------------
        // 1-3 ÖN MESAFELER
        // -----------------------------

        sensor.AddObservation(
            sensors.frontData.distance
        );

        sensor.AddObservation(
            sensors.frontLeftData.distance
        );

        sensor.AddObservation(
            sensors.frontRightData.distance
        );

        // -----------------------------
        // 4-6 ÖN YÜZEYLER
        // -----------------------------

        sensor.AddObservation(
            SurfaceToAI(
                sensors.frontData.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.frontLeftData.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.frontRightData.surface
            )
        );

        // -----------------------------
        // 7-10 4 TEKER ZEMİNİ
        // -----------------------------

        sensor.AddObservation(
            SurfaceToAI(
                sensors.frontLeftGround.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.frontRightGround.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.rearLeftGround.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.rearRightGround.surface
            )
        );

        // -----------------------------
        // 11 MERKEZ ZEMİN
        // -----------------------------

        sensor.AddObservation(
            SurfaceToAI(
                sensors.centerGround.surface
            )
        );

        // -----------------------------
        // 12-14 İLERİDEKİ ZEMİNLER
        // -----------------------------

        sensor.AddObservation(
            SurfaceToAI(
                sensors.farLeftGroundData.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.farCenterGroundData.surface
            )
        );

        sensor.AddObservation(
            SurfaceToAI(
                sensors.farRightGroundData.surface
            )
        );

        // -----------------------------
        // 15 HIZ
        // -----------------------------

        float currentSpeed =
            rb.linearVelocity.magnitude;

        sensor.AddObservation(
            Mathf.Clamp01(
                currentSpeed / maxSpeed
            )
        );

        // -----------------------------
        // 16 İLERİ HIZ
        // -----------------------------

        float forwardSpeed =
            Vector3.Dot(
                rb.linearVelocity,
                transform.forward
            );

        sensor.AddObservation(
            Mathf.Clamp(
                forwardSpeed / maxSpeed,
                -1f,
                1f
            )
        );

        // -----------------------------
        // 17 YANAL KAYMA
        // -----------------------------

        float sidewaysSpeed =
            Vector3.Dot(
                rb.linearVelocity,
                transform.right
            );

        sensor.AddObservation(
            Mathf.Clamp(
                sidewaysSpeed / maxSpeed,
                -1f,
                1f
            )
        );
    }

    // =========================================================
    // AI'IN VERDİĞİ KARAR
    // =========================================================

    public override void OnActionReceived(
        ActionBuffers actions)
    {
        float steering =
            Mathf.Clamp(
                actions.ContinuousActions[0],
                -1f,
                1f
            );

        float throttle =
            Mathf.Clamp01(
                (actions.ContinuousActions[1] + 1f) / 2f
            );

        car.aiControlled = true;

        car.SetAIInput(
            throttle,
            steering
        );

        CalculateReward();
        ReportLapStatistics();
    }

    // =========================================================
    // REWARD SİSTEMİ
    // =========================================================

    void CalculateReward()
    {

        // -----------------------------
        // ZAMAN BASKISI (Sabit ve Güvenli)
        // -----------------------------
        AddReward(
            -0.0005f
        );

        // -----------------------------
        // KAZA
        // -----------------------------

        if (car.IsCrashed())
        {
            AddReward(-2.0f);

            if (multiAgentRaceMonitor != null)
            {
                multiAgentRaceMonitor.RecordEpisode(true);
            }

            EndEpisode();

            return;
        }

        // -----------------------------
        // ÇİM / ÇAKIL TEKER CEZASI
        // (Asfalt ödülü iptal edildi)
        // -----------------------------

        int grassWheels = 0;
        int gravelWheels = 0;

        if (sensors.frontLeftGround.surface ==
            F1SensorSystem.SurfaceType.Grass)
        {
            grassWheels++;
        }

        if (sensors.frontRightGround.surface ==
            F1SensorSystem.SurfaceType.Grass)
        {
            grassWheels++;
        }

        if (sensors.rearLeftGround.surface ==
            F1SensorSystem.SurfaceType.Grass)
        {
            grassWheels++;
        }

        if (sensors.rearRightGround.surface ==
            F1SensorSystem.SurfaceType.Grass)
        {
            grassWheels++;
        }

        if (sensors.frontLeftGround.surface ==
            F1SensorSystem.SurfaceType.Gravel)
        {
            gravelWheels++;
        }

        if (sensors.frontRightGround.surface ==
            F1SensorSystem.SurfaceType.Gravel)
        {
            gravelWheels++;
        }

        if (sensors.rearLeftGround.surface ==
            F1SensorSystem.SurfaceType.Gravel)
        {
            gravelWheels++;
        }

        if (sensors.rearRightGround.surface ==
            F1SensorSystem.SurfaceType.Gravel)
        {
            gravelWheels++;
        }

        // Çim
        switch (grassWheels)
        {
            case 1:
                AddReward(-0.02f);
                break;

            case 2:
                AddReward(-0.06f);
                break;

            case 3:
                AddReward(-0.12f);
                break;

            case 4:
                AddReward(-0.20f);
                break;
        }

        // Çakıl
        switch (gravelWheels)
        {
            case 1:
                AddReward(-0.04f);
                break;

            case 2:
                AddReward(-0.08f);
                break;

            case 3:
                AddReward(-0.16f);
                break;

            case 4:
                AddReward(-0.30f);
                break;
        }

        // -----------------------------
        // İLERLEME ÖDÜLÜ (Yeni Hedef Odaklı)
        // -----------------------------

        if (checkpoints != null &&
            checkpoints.Count > 0 &&
            nextCheckpointIndex < checkpoints.Count)
        {
            Vector3 directionToCheckpoint =
                (checkpoints[nextCheckpointIndex].position - transform.position).normalized;

            float speedTowardsCheckpoint =
                Vector3.Dot(
                    rb.linearVelocity,
                    directionToCheckpoint
                );

            if (speedTowardsCheckpoint > 0f)
            {
                float speedReward =
                    Mathf.Clamp01(
                        speedTowardsCheckpoint / maxSpeed
                    );

                AddReward(
                    speedReward * 0.003f
                );
            }
            else
            {
                float speedPenalty =
                    Mathf.Clamp01(
                        Mathf.Abs(speedTowardsCheckpoint) / maxSpeed
                    );

                AddReward(
                    -speedPenalty * 0.002f
                );
            }
        }

        // -----------------------------
        // YANAL KAYMA CEZASI
        // -----------------------------

        float sidewaysSpeed =
            Mathf.Abs(
                Vector3.Dot(
                    rb.linearVelocity,
                    transform.right
                )
            );

        AddReward(
            -Mathf.Clamp01(
                sidewaysSpeed / maxSpeed
            ) * 0.001f
        );
    }

    // =========================================================
    // CHECKPOINT (Yeni Mantık)
    // =========================================================

    public void ReachCheckpoint(
        int checkpointIndex,
        bool isFinish)
    {
        // Gelen ID (1,2,3..) ile beklenen index'in bir fazlası eşleşmeli
        if (checkpointIndex != (nextCheckpointIndex))
        {
            return;
        }

        AddReward(1f);

        Debug.Log(
            "Checkpoint geçildi: " +
            checkpointIndex
        );

        nextCheckpointIndex++;

        if (isFinish)
        {
            AddReward(10.0f);

            completedLapCount++;

            int academyStep = Academy.Instance.TotalStepCount;

            if (firstCompletedLapAcademyStep < 0)
            {
                firstCompletedLapAcademyStep = academyStep;

                Debug.Log(
                    "ILK TAM TUR! Academy adimi: " +
                    firstCompletedLapAcademyStep
                );
            }

            Debug.Log(
                "TUR TAMAMLANDI! Toplam tam tur: " +
                completedLapCount +
                " | Academy adimi: " +
                academyStep
            );

            RegisterSuccessfulLapForSpeedCurriculum();
            ReportLapStatistics();

            if (multiAgentRaceMonitor != null)
            {
                multiAgentRaceMonitor.RecordEpisode(false);
            }

            EndEpisode();
        }
    }

    void ReportLapStatistics()
    {
        Academy.Instance.StatsRecorder.Add(
            "Lap/Completed Count",
            completedLapCount,
            StatAggregationMethod.MostRecent
        );

        if (firstCompletedLapAcademyStep >= 0)
        {
            Academy.Instance.StatsRecorder.Add(
                "Lap/First Completion Academy Step",
                firstCompletedLapAcademyStep,
                StatAggregationMethod.MostRecent
            );
        }

        if (enableSpeedCurriculum && speedStages.Length > 0)
        {
            Academy.Instance.StatsRecorder.Add(
                "Curriculum/Current Speed",
                GetCurrentSpeedStage(),
                StatAggregationMethod.MostRecent
            );

            Academy.Instance.StatsRecorder.Add(
                "Curriculum/Successful Laps At Current Speed",
                successfulLapsAtCurrentSpeed,
                StatAggregationMethod.MostRecent
            );
        }
    }

    void InitializeSpeedCurriculum()
    {
        if (!enableSpeedCurriculum || speedStages == null ||
            speedStages.Length == 0)
        {
            return;
        }

        LoadSpeedCurriculumState();
        ApplyCurrentSpeedStage();
        ArchiveInitial20KmhModelIfNeeded();

        if (archivePending)
        {
            nextArchiveCheckTime = Time.unscaledTime;
        }
    }

    void RegisterSuccessfulLapForSpeedCurriculum()
    {
        if (!enableSpeedCurriculum || curriculumCompleted || archivePending)
        {
            return;
        }

        successfulLapsAtCurrentSpeed++;
        SaveSpeedCurriculumState();

        Debug.Log(
            "Hiz " + GetCurrentSpeedStage() +
            " | Basarili tur: " + successfulLapsAtCurrentSpeed +
            "/" + successfulLapsPerSpeed
        );

        if (successfulLapsAtCurrentSpeed >= successfulLapsPerSpeed)
        {
            archivePending = true;
            archiveRequestedUtcTicks = DateTime.UtcNow.Ticks;
            SaveSpeedCurriculumState();

            Debug.Log(
                "Hiz " + GetCurrentSpeedStage() +
                " icin " + successfulLapsPerSpeed +
                " tur tamamlandi. Yeni checkpoint bekleniyor."
            );
        }
    }

    void TryCompleteSpeedTransition()
    {
        DateTime requestedAfter = new DateTime(
            archiveRequestedUtcTicks,
            DateTimeKind.Utc
        );

        ModelSnapshot snapshot = FindLatestModelSnapshot(requestedAfter);

        if (snapshot == null)
        {
            return;
        }

        float completedSpeed = GetCurrentSpeedStage();

        if (!ArchiveModelSnapshot(snapshot, completedSpeed))
        {
            return;
        }

        archivePending = false;

        if (currentSpeedStageIndex >= speedStages.Length - 1)
        {
            curriculumCompleted = true;
            SaveSpeedCurriculumState();

            Debug.Log(
                "Hiz curriculum tamamlandi. Arac " +
                completedSpeed +
                " hizinda egitime devam ediyor."
            );

            return;
        }

        currentSpeedStageIndex++;
        successfulLapsAtCurrentSpeed = 0;
        archiveRequestedUtcTicks = 0;
        ApplyCurrentSpeedStage();
        SaveSpeedCurriculumState();

        Debug.Log(
            "Yeni hiz seviyesi basladi: " +
            GetCurrentSpeedStage()
        );
    }

    void ApplyCurrentSpeedStage()
    {
        float speed = GetCurrentSpeedStage();

        currentCurriculumSpeed = speed;
        car.speed = speed;
        maxSpeed = speed;

        if (sensors == null)
        {
            return;
        }

        int sensorIncreaseCount = currentSpeedStageIndex + 1;

        sensors.frontSensorLength =
            baseFrontSensorLength +
            frontSensorIncreasePerStage * sensorIncreaseCount;

        sensors.farGroundSensorLength =
            baseFarGroundSensorLength +
            farGroundSensorIncreasePerStage * sensorIncreaseCount;

        currentFrontSensorLength = sensors.frontSensorLength;
        currentFarGroundSensorLength = sensors.farGroundSensorLength;
    }

    float GetCurrentSpeedStage()
    {
        if (speedStages == null || speedStages.Length == 0)
        {
            return car != null ? car.speed : maxSpeed;
        }

        int index = Mathf.Clamp(
            currentSpeedStageIndex,
            0,
            speedStages.Length - 1
        );

        return speedStages[index];
    }

    void ArchiveInitial20KmhModelIfNeeded()
    {
        if (initial20KmhModelArchived ||
            string.IsNullOrWhiteSpace(initial20KmhModelStep))
        {
            return;
        }

        string sourceDirectory = GetTrainingModelDirectory();
        string baseName = trainingBehaviorName + "-" + initial20KmhModelStep;
        string onnxPath = Path.Combine(sourceDirectory, baseName + ".onnx");
        string ptPath = Path.Combine(sourceDirectory, baseName + ".pt");

        if (!File.Exists(onnxPath) || !File.Exists(ptPath))
        {
            Debug.LogWarning(
                "20 hiz modeli henuz arsivlenemedi. Beklenen dosya: " +
                baseName
            );

            return;
        }

        ModelSnapshot snapshot = new ModelSnapshot
        {
            onnxPath = onnxPath,
            ptPath = ptPath
        };

        if (ArchiveModelSnapshot(snapshot, 20f))
        {
            initial20KmhModelArchived = true;
            SaveSpeedCurriculumState();
        }
    }

    ModelSnapshot FindLatestModelSnapshot(DateTime requestedAfter)
    {
        string sourceDirectory = GetTrainingModelDirectory();

        if (!Directory.Exists(sourceDirectory))
        {
            Debug.LogWarning(
                "Model klasoru bulunamadi: " + sourceDirectory
            );

            return null;
        }

        FileInfo newestOnnx = null;

        foreach (string onnxPath in Directory.GetFiles(
            sourceDirectory,
            trainingBehaviorName + "-*.onnx"))
        {
            FileInfo onnxFile = new FileInfo(onnxPath);
            string ptPath = Path.ChangeExtension(onnxPath, ".pt");

            if (onnxFile.LastWriteTimeUtc < requestedAfter ||
                !File.Exists(ptPath))
            {
                continue;
            }

            FileInfo ptFile = new FileInfo(ptPath);

            if ((DateTime.UtcNow - onnxFile.LastWriteTimeUtc).TotalSeconds < 3 ||
                (DateTime.UtcNow - ptFile.LastWriteTimeUtc).TotalSeconds < 3)
            {
                continue;
            }

            if (newestOnnx == null ||
                onnxFile.LastWriteTimeUtc > newestOnnx.LastWriteTimeUtc)
            {
                newestOnnx = onnxFile;
            }
        }

        if (newestOnnx == null)
        {
            return null;
        }

        return new ModelSnapshot
        {
            onnxPath = newestOnnx.FullName,
            ptPath = Path.ChangeExtension(newestOnnx.FullName, ".pt")
        };
    }

    bool ArchiveModelSnapshot(ModelSnapshot snapshot, float speed)
    {
        try
        {
            Directory.CreateDirectory(modelArchiveDirectory);

            string speedLabel = Mathf.RoundToInt(speed) + "kmh";
            string onnxDestination = Path.Combine(
                modelArchiveDirectory,
                trainingBehaviorName + "-" + speedLabel + ".onnx"
            );
            string ptDestination = Path.Combine(
                modelArchiveDirectory,
                trainingBehaviorName + "-" + speedLabel + ".pt"
            );

            File.Copy(snapshot.onnxPath, onnxDestination, true);
            File.Copy(snapshot.ptPath, ptDestination, true);

            Debug.Log(
                speedLabel +
                " modeli arsivlendi: " +
                modelArchiveDirectory
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Model arsivlenemedi: " + exception.Message
            );

            return false;
        }
    }

    string GetTrainingModelDirectory()
    {
        return Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "results",
            trainingRunId,
            trainingBehaviorName
        ));
    }

    string GetSpeedCurriculumStatePath()
    {
        return Path.Combine(
            GetTrainingModelDirectory(),
            "speed_curriculum_state.json"
        );
    }

    void LoadSpeedCurriculumState()
    {
        string statePath = GetSpeedCurriculumStatePath();

        if (!File.Exists(statePath))
        {
            return;
        }

        try
        {
            SpeedCurriculumState state = JsonUtility.FromJson<SpeedCurriculumState>(
                File.ReadAllText(statePath)
            );

            if (state == null)
            {
                return;
            }

            currentSpeedStageIndex = Mathf.Clamp(
                state.currentSpeedStageIndex,
                0,
                speedStages.Length - 1
            );
            successfulLapsAtCurrentSpeed = Mathf.Max(
                0,
                state.successfulLapsAtCurrentSpeed
            );
            archivePending = state.archivePending;
            initial20KmhModelArchived = state.initial20KmhModelArchived;
            curriculumCompleted = state.curriculumCompleted;
            archiveRequestedUtcTicks = state.archiveRequestedUtcTicks;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Hiz curriculum durumu okunamadi: " +
                exception.Message
            );
        }
    }

    void SaveSpeedCurriculumState()
    {
        if (!enableSpeedCurriculum)
        {
            return;
        }

        try
        {
            string statePath = GetSpeedCurriculumStatePath();
            string temporaryPath = statePath + ".tmp";
            SpeedCurriculumState state = new SpeedCurriculumState
            {
                currentSpeedStageIndex = currentSpeedStageIndex,
                successfulLapsAtCurrentSpeed = successfulLapsAtCurrentSpeed,
                archivePending = archivePending,
                initial20KmhModelArchived = initial20KmhModelArchived,
                curriculumCompleted = curriculumCompleted,
                archiveRequestedUtcTicks = archiveRequestedUtcTicks
            };

            Directory.CreateDirectory(GetTrainingModelDirectory());
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state));
            File.Copy(temporaryPath, statePath, true);
            File.Delete(temporaryPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Hiz curriculum durumu kaydedilemedi: " +
                exception.Message
            );
        }
    }

    // =========================================================
    // ZEMİNİ SAYISAL DEĞERE ÇEVİR
    // =========================================================

    float SurfaceToAI(
        F1SensorSystem.SurfaceType surface)
    {
        switch (surface)
        {
            case F1SensorSystem.SurfaceType.Asphalt:
                return 0f;

            case F1SensorSystem.SurfaceType.Grass:
                return 0.25f;

            case F1SensorSystem.SurfaceType.Gravel:
                return 0.50f;

            case F1SensorSystem.SurfaceType.Curb:
                return 0.10f;

            case F1SensorSystem.SurfaceType.WhiteLine:
                return 0.15f;

            case F1SensorSystem.SurfaceType.Wall:
                return 1f;

            case F1SensorSystem.SurfaceType.Barrier:
                return 1f;

            default:
                return -1f;
        }
    }

    // =========================================================
    // MANUEL TEST
    // =========================================================

    public override void Heuristic(
        in ActionBuffers actionsOut)
    {
        var actions =
            actionsOut.ContinuousActions;

        actions[0] =
            Input.GetAxis("Horizontal");

        actions[1] =
            Input.GetAxis("Vertical");
    }
}
