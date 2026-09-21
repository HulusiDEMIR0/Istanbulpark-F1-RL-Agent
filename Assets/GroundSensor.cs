using UnityEngine;

public class GroundSensor : MonoBehaviour
{
    public float sensorLength = 5f;

    private string lastSurface = "";

    void Update()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            Vector3.down,
            sensorLength
        );

        RaycastHit closestHit = new RaycastHit();
        bool groundFound = false;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            // Arabanın kendi collider'larını atla
            if (hit.collider.transform.IsChildOf(transform.root))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                groundFound = true;
            }
        }

        string currentSurface;

        if (groundFound)
        {
            string objectName = GetFullObjectName(
                closestHit.collider.transform
            );

            currentSurface = DetectSurface(objectName);

            // Sadece zemin değiştiğinde Console'a yaz
            if (currentSurface != lastSurface)
            {
                Debug.Log(
                    "GROUND SENSOR → " +
                    currentSurface +
                    " | Mesafe: " +
                    closestDistance.ToString("F2") +
                    " m"
                );

                lastSurface = currentSurface;
            }
        }
        else
        {
            currentSurface = "NO GROUND";

            if (currentSurface != lastSurface)
            {
                Debug.Log(
                    "GROUND SENSOR → ZEMİN BULUNAMADI"
                );

                lastSurface = currentSurface;
            }
        }

        // Scene ekranında yeşil sensör çizgisi
        Debug.DrawRay(
            transform.position,
            Vector3.down * sensorLength,
            Color.green
        );
    }

    string GetFullObjectName(Transform objectTransform)
    {
        string objectName = objectTransform.name.ToUpper();

        Transform parent = objectTransform.parent;

        while (parent != null)
        {
            objectName += " " + parent.name.ToUpper();
            parent = parent.parent;
        }

        return objectName;
    }

    string DetectSurface(string objectName)
    {
        // Çim
        if (objectName.Contains("GRASS"))
            return "GRASS";

        // Çakıl
        if (objectName.Contains("GRAVEL"))
            return "GRAVEL";

        // Kerb / Curbs
        if (objectName.Contains("CURB") ||
            objectName.Contains("CURBS"))
            return "CURBS";

        // Beyaz çizgi
        if (objectName.Contains("WHITE"))
            return "WHITE_LINE";

        // Asfalt
        if (objectName.Contains("TARMAC"))
            return "ASPHALT";

        return "UNKNOWN";
    }
}