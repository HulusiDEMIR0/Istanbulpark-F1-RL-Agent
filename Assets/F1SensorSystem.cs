using UnityEngine;

public class F1SensorSystem : MonoBehaviour
{
    [Header("Layer Masks (YENİ)")]
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;

    [Header("Front Center")]
    public Transform front;

    [Header("Front Wheel Sensors")]
    public Transform frontLeft;
    public Transform frontRight;

    [Header("Rear Wheel Sensors")]
    public Transform rearLeft;
    public Transform rearRight;

    [Header("Center Ground")]
    public Transform groundSensor;

    [Header("Far Ground Sensors")]
    public Transform farCenterGround;
    public Transform farLeftGround;
    public Transform farRightGround;

    [Header("Sensor Settings")]
    public float frontSensorLength = 40f;
    public float groundSensorLength = 5f;
    public float farGroundSensorLength = 8f;

    public enum SurfaceType
    {
        Unknown,
        Asphalt,
        Grass,
        Gravel,
        Curb,
        WhiteLine,
        Wall,
        Barrier
    }

    [System.Serializable]
    public struct SensorData
    {
        public float distance;
        public SurfaceType surface;
    }

    // Ön görüş
    public SensorData frontData;
    public SensorData frontLeftData;
    public SensorData frontRightData;

    // Teker zeminleri
    public SensorData frontLeftGround;
    public SensorData frontRightGround;
    public SensorData rearLeftGround;
    public SensorData rearRightGround;

    // Merkez zemin
    public SensorData centerGround;

    // İlerideki zemin
    public SensorData farCenterGroundData;
    public SensorData farLeftGroundData;
    public SensorData farRightGroundData;

    void FixedUpdate()
    {
        // -----------------------------
        // ÖN SENSÖRLER
        // -----------------------------

        frontData = ReadForwardSensor(
            front,
            frontSensorLength,
            Color.red
        );

        frontLeftData = ReadForwardSensor(
            frontLeft,
            frontSensorLength,
            Color.yellow
        );

        frontRightData = ReadForwardSensor(
            frontRight,
            frontSensorLength,
            Color.yellow
        );

        // -----------------------------
        // 4 TEKER ZEMİNİ
        // -----------------------------

        frontLeftGround = ReadGroundSensor(
            frontLeft,
            groundSensorLength
        );

        frontRightGround = ReadGroundSensor(
            frontRight,
            groundSensorLength
        );

        rearLeftGround = ReadGroundSensor(
            rearLeft,
            groundSensorLength
        );

        rearRightGround = ReadGroundSensor(
            rearRight,
            groundSensorLength
        );

        // -----------------------------
        // MERKEZ ZEMİN
        // -----------------------------

        centerGround = ReadGroundSensor(
            groundSensor,
            groundSensorLength
        );

        // -----------------------------
        // İLERİDEKİ ZEMİNLER
        // -----------------------------

        farCenterGroundData = ReadGroundSensor(
            farCenterGround,
            farGroundSensorLength
        );

        farLeftGroundData = ReadGroundSensor(
            farLeftGround,
            farGroundSensorLength
        );

        farRightGroundData = ReadGroundSensor(
            farRightGround,
            farGroundSensorLength
        );
    }

    // =========================================================
    // ÖN / ENGEL SENSÖRÜ (Optimize Edildi)
    // =========================================================

    SensorData ReadForwardSensor(
        Transform sensor,
        float length,
        Color rayColor)
    {
        SensorData data = new SensorData();

        data.distance = 1f;
        data.surface = SurfaceType.Unknown;

        if (sensor == null)
            return data;

        RaycastHit hit;

        // LayerMask eklendi: Sadece engelleri görür
        if (Physics.Raycast(
            sensor.position,
            sensor.forward,
            out hit,
            length,
            obstacleLayer))
        {
            data.distance =
                Mathf.Clamp01(hit.distance / length);

            data.surface =
                DetectSurface(hit.collider);

            Debug.DrawRay(
                sensor.position,
                sensor.forward * hit.distance,
                rayColor
            );
        }
        else
        {
            Debug.DrawRay(
                sensor.position,
                sensor.forward * length,
                Color.green
            );
        }

        return data;
    }

    // =========================================================
    // ZEMİN SENSÖRÜ (Büyük Oranda Optimize Edildi)
    // =========================================================

    SensorData ReadGroundSensor(
        Transform sensor,
        float length)
    {
        SensorData data = new SensorData();

        data.distance = 1f;
        data.surface = SurfaceType.Unknown;

        if (sensor == null)
            return data;

        RaycastHit hit;

        // RaycastAll kaldırıldı. LayerMask sayesinde sadece zemini görür, arabayı yoksayar.
        if (Physics.Raycast(
            sensor.position,
            Vector3.down,
            out hit,
            length,
            groundLayer))
        {
            data.distance =
                Mathf.Clamp01(
                    hit.distance / length
                );

            data.surface =
                DetectSurface(
                    hit.collider
                );
        }

        Debug.DrawRay(
            sensor.position,
            Vector3.down * hit.distance,
            Color.blue
        );

        return data;
    }

    // =========================================================
    // YÜZEY ALGILAMA (Tag Sistemi - Performans Dostu)
    // =========================================================

    SurfaceType DetectSurface(Collider col)
    {
        if (col.CompareTag("Wall"))
            return SurfaceType.Wall;

        if (col.CompareTag("Barrier"))
            return SurfaceType.Barrier;

        if (col.CompareTag("Grass"))
            return SurfaceType.Grass;

        if (col.CompareTag("Gravel"))
            return SurfaceType.Gravel;

        if (col.CompareTag("Curb"))
            return SurfaceType.Curb;

        if (col.CompareTag("WhiteLine"))
            return SurfaceType.WhiteLine;

        if (col.CompareTag("Asphalt"))
            return SurfaceType.Asphalt;

        return SurfaceType.Unknown;
    }
}