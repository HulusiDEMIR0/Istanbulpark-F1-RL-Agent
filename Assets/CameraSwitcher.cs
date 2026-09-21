using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera mainCamera;
    public Camera trainingCamera;

    void Start()
    {
        mainCamera.enabled = true;
        trainingCamera.enabled = false;
    }

    void Update()
    {
        // 1 = Araba kamerası
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            mainCamera.enabled = true;
            trainingCamera.enabled = false;
        }

        // 2 = Pist üstü kamera
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            mainCamera.enabled = false;
            trainingCamera.enabled = true;
        }
    }
}