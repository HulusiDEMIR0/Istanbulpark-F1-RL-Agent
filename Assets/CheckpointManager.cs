using UnityEngine;

[ExecuteAlways] // Bu komut, kodun Play tuşuna basmadan editörde de çalışmasını sağlar
public class CheckpointManager : MonoBehaviour
{
    void Update()
    {
        // Eğer oyun çalışıyorsa (Play modundaysan) bu kodu sürekli boşuna çalıştırma
        if (Application.isPlaying)
            return;

        // Klasörün içindeki tüm checkpoint'leri yukarıdan aşağıya sırayla gezer
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            Checkpoint cp = child.GetComponent<Checkpoint>();

            if (cp != null)
            {
                // Sadece index numarası yanlışsa güncelle (Performans için)
                if (cp.checkpointIndex != i)
                {
                    cp.checkpointIndex = i;
                }

                // Hiyerarşideki ismi yanlışsa otomatik düzelt
                string expectedName = "Checkpoint_" + i;
                if (child.name != expectedName)
                {
                    child.name = expectedName;
                }
            }
        }
    }
}