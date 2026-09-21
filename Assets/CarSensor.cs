using UnityEngine;

public class CarSensor : MonoBehaviour
{
    public float sensorLength = 15f;

    void Update()
    {
        RaycastHit hit;

        // Sensörün önüne doğru ışın gönder
        if (Physics.Raycast(
            transform.position,
            transform.forward,
            out hit,
            sensorLength))
        {
            string objectName = hit.collider.gameObject.name.ToUpper();

            string surfaceType = DetectSurface(objectName);

            // Console spam yapmaması için Debug.Log kaldırıldı.
            // Sensör yine yüzeyi algılıyor.
        }

        // Scene ekranında sensörü görebilmek için
        Debug.DrawRay(
            transform.position,
            transform.forward * sensorLength,
            Color.red
        );
    }

    string DetectSurface(string objectName)
    {
        // Duvar
        if (objectName.Contains("WALL"))
        {
            return "WALL";
        }

        // Bariyer
        if (objectName.Contains("BARRIER"))
        {
            return "BARRIER";
        }

        // Çim
        if (objectName.Contains("GRASS"))
        {
            return "GRASS";
        }

        // Çakıl
        if (objectName.Contains("GRAVEL"))
        {
            return "GRAVEL";
        }

        // Beyaz çizgi
        if (objectName.Contains("WHITE"))
        {
            return "WHITE_LINE";
        }

        // Asfalt
        if (objectName.Contains("TARMAC"))
        {
            return "ASPHALT";
        }

        return "UNKNOWN";
    }
}