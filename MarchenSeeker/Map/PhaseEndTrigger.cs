using UnityEngine;
using UnityEngine.Tilemaps;

public class PhaseEndTrigger : MonoBehaviour
{
    [SerializeField] Tilemap tilemap;
    Bounds tilemapBounds;
    public float mapLength;
    public float tilemapRightX;

    bool isScrolling = true;

    void Start()
    {
        tilemap.CompressBounds();
        tilemapBounds = tilemap.localBounds;
        mapLength = tilemapBounds.size.x;
        tilemapRightX = tilemapBounds.max.x + tilemap.transform.position.x;
    }

    void Update()
    {
        if (isScrolling)
        {
            // 페이즈 전환 조건 체크
            float camRightX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x; // 뷰포트 오른쪽 끝의 월드 좌표
            tilemapRightX = tilemapBounds.max.x + tilemap.transform.position.x; // 타일맵 오른쪽 끝의 월드 좌표

            if (camRightX >= tilemapRightX - 0.5f)
            {
                isScrolling = false;
                GetComponent<MapScroll>().isScrolling = false;
                GetComponent<SceneTransition>().StartFadeOut();
            }
        }
    }
}
