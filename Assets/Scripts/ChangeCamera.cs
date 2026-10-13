using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject mainCamera;
    public GameObject secondCamera;

    public void SwitchCamera()
    {
        bool isMainActive = mainCamera.activeSelf;

        mainCamera.SetActive(!isMainActive);
        secondCamera.SetActive(isMainActive);
    }
}