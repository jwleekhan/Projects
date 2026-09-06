using UnityEngine;

public class MapScroll : MonoBehaviour
{
    Transform scrollTarget;
    Transform playerTransform;
    Transform bulletSetTransform;
    public float scrollSpeed = 4f;
    public bool isScrolling = true;

    void Start()
    {
        if (scrollTarget == null) scrollTarget = transform;
        playerTransform = PlayerManager.Instance.GetPlayer().transform;
        GameObject bulletSet = GameObject.Find("BulletSet");
        if (bulletSet != null) bulletSetTransform = bulletSet.transform;
        scrollSpeed = 4f;
    }

    void Update()
    {
        if (isScrolling)
        {
            // 자동 이동 시스템
            scrollTarget.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
        }
        else
        {
            // 페이즈 전환 연출 시, 플레이어가 화면 우측을 향해 이동
            float camRightX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

            if (camRightX > playerTransform.position.x - 0.5f)
            {
                playerTransform.Translate(Vector3.right * scrollSpeed * Time.deltaTime);
                if (bulletSetTransform != null)
                {
                    bulletSetTransform.Translate(Vector3.right * scrollSpeed * Time.deltaTime);
                }
            }
        }
    }

    public void SetScrollTarget(Transform target)
    {
        scrollTarget = target;
    }
}
