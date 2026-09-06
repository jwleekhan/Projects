using UnityEngine;

public class FixedAspect : MonoBehaviour
{
    public float targetAspect = 16f / 9f;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateViewport();
    }

    void UpdateViewport()
    {
        // 가로 모드 기준
        float windowAspect = Screen.width / (float)Screen.height;
        
        if (targetAspect > windowAspect)
        {
            // 세로가 긴 화면 (e.g. 4:3) -> 상하 여백
            float scaleHeight = windowAspect / targetAspect;
            Rect rect = new Rect(0, (1f - scaleHeight) / 2f, 1f, scaleHeight);
            cam.rect = rect;
        }
        else
        {
            // 가로가 긴 화면 (e.g. 18:9) -> 좌우 여백
            float scaleWidth = targetAspect / windowAspect;
            Rect rect = new Rect((1f - scaleWidth) / 2f, 0, scaleWidth, 1f);
            cam.rect = rect;
        }
    }
}
