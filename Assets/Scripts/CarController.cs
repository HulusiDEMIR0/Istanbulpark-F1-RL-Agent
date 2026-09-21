using UnityEngine;

public class CarController : MonoBehaviour
{

    [Header("Debug Info (Canlı Takip)")]
    [SerializeField] private float currentRealSpeed = 0f; // Anlık gerçek hızı gösterir

    [Header("Car Settings")]
    public float speed = 20f; // Başlangıç için 20 ideal
    public float turnSpeed = 100f;

    [Header("Acceleration Settings (YENİ)")]
    public float accelerationRate = 5f; // Gazın tepki verme hızı (Büyüdükçe daha hızlı hızlanır)
    private float currentThrottle = 0f;  // Anlık gaz değeri (0 ile target arasında yumuşak değişir)

    [Header("Grass Settings")]
    public float grassSpeedMultiplier = 4f;
    public float grassTurnMultiplier = 4f;

    [Header("Gravel Settings")]
    public float gravelSpeedMultiplier = 4f;
    public float gravelTurnMultiplier = 4f;

    private Rigidbody rb;

    private bool onGrass = false;
    private bool onGravel = false;
    private bool crashed = false;

    public bool aiControlled = false;

    private float aiThrottle = 0f;
    private float aiSteering = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (crashed)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        float targetMove;
        float targetTurn;

        if (aiControlled)
        {
            targetMove = aiThrottle;
            targetTurn = aiSteering;
        }
        else
        {
            targetMove = Input.GetAxis("Vertical");
            targetTurn = Input.GetAxis("Horizontal");
        }

        // ==========================================
        // İVMELENME MANTIĞI (Gazın yumuşak geçişi)
        // ==========================================
        currentThrottle = Mathf.MoveTowards(
            currentThrottle,
            targetMove,
            accelerationRate * Time.fixedDeltaTime
        );

        // -- BURAYI EKLE --
        currentRealSpeed = Mathf.Abs(currentThrottle * speed);

        float currentSpeed = speed;
        float currentTurnSpeed = turnSpeed;

        // ÇİM: yavaşlama
        if (onGrass)
        {
            currentSpeed *= grassSpeedMultiplier;
            currentTurnSpeed *= grassTurnMultiplier;
        }

        // ÇAKIL: yavaşlama
        if (onGravel)
        {
            currentSpeed *= gravelSpeedMultiplier;
            currentTurnSpeed *= gravelTurnMultiplier;
        }

        // move yerine yumuşatılmış currentThrottle kullanılıyor
        Vector3 movement =
            transform.forward *
            currentThrottle *
            currentSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);

        Quaternion rotation = Quaternion.Euler(
            0f,
            targetTurn * currentTurnSpeed * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * rotation);
    }

    public void SetAIInput(float throttle, float steering)
    {
        aiThrottle = Mathf.Clamp(throttle, -1f, 1f);
        aiSteering = Mathf.Clamp(steering, -1f, 1f);
    }

    public bool IsCrashed()
    {
        return crashed;
    }

    public bool IsOnGrass()
    {
        return onGrass;
    }

    public bool IsOnGravel()
    {
        return onGravel;
    }

    public void ResetCar()
    {
        crashed = false;
        onGrass = false;
        onGravel = false;

        aiThrottle = 0f;
        aiSteering = 0f;
        currentThrottle = 0f; // Sıfırlandı

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        string objectName =
            GetFullObjectName(collision.collider.transform).ToUpper();

        if (objectName.Contains("GRASS"))
        {
            onGrass = true;
        }

        if (objectName.Contains("GRAVEL"))
        {
            onGravel = true;
        }

        // İsim kontrolü eski sahne düzeni için korunur. Tag ve katman,
        // iç içe GLB collider'larında aynı çarpışmayı güvenilir yakalar.
        bool isWallOrBarrier =
            objectName.Contains("WALL") ||
            objectName.Contains("BARRIER") ||
            collision.collider.CompareTag("Wall") ||
            collision.collider.CompareTag("Barrier") ||
            collision.collider.gameObject.layer == LayerMask.NameToLayer("Obstacles");

        if (isWallOrBarrier)
        {
            crashed = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.Log("KAZA! AI bölümü sona erdi.");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Doğru satır burada:
        string objectName = GetFullObjectName(collision.collider.transform).ToUpper();

        if (objectName.Contains("GRASS"))
        {
            onGrass = false;
        }

        if (objectName.Contains("GRAVEL"))
        {
            onGravel = false;
        }
    }

    private string GetFullObjectName(Transform obj)
    {
        string name = obj.name;

        Transform parent = obj.parent;

        while (parent != null)
        {
            name += " " + parent.name;
            parent = parent.parent;
        }

        return name;
    }
}
