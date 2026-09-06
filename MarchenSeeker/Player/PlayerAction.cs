using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerAction : MonoBehaviour
{
    [Header("Jump Settings")]
    public  float jumpForce      =   5f;    // 점프 힘
    public  float gravityY       = -20f;    // 중력
    public  int   maxJumpCount   =    2;    // 최대 점프 횟수
    [SerializeField] private int   jumpCount;                // 현재 사용한 점프 횟수
    [SerializeField] private bool  isJumping      = true;
    // [SerializeField] private bool  isGround       = false;   // 착지 상태인지 판별 -> 변수 삭제 이유는 OnCollisionExit2D 주석 참고

    [SerializeField] private float landLockout = 0.03f; // 착지 리셋 금지 시간 (1~2 프레임 정도)
    private float lockLandingUntil = -1f;

    [Header("Slide Settings")]
    public  float slideShrinkY   = 0.5f;    // 슬라이드 시 변형 정도
    public  float slideIncreaseX = 2.0f;   // 슬라이드 시 변형 정도
    public  float dropWindow     = 0.5f;    // 드롭 트리거 윈도우
    public  float ignoreDuration = 0.5f;    // 드롭 시 충돌 무시 시간
    private float lastSlideTime  =  -1f;    // 마지막 슬라이드 종료 시점
    private bool  isSliding      = false;   // 슬라이드 상태
    private bool  slideQueued    = false;   // 슬라이드 대기
    private bool  isDropping;               // 드롭(충돌 무시) 상태
    private Vector3 defaultScale = Vector3.one;

    HashSet<int> jumpTouchIds = new();
    HashSet<int> slideTouchIds = new();

    public GameObject SmokeEffect;

    PlayerStat playerStat;

    private Rigidbody2D rb;
    private Collider2D col;
    private Collider2D[] fieldCols;

    private MoonlightSkill moonlightSkill;

    private int baseSmokeShellCount = 2; // 기본 연막탄 개수
    private const int maxSmokeShellCount = 3; // 최대 연막탄 개수
    private int smokeShellCount;       // 현재 연막탄 개수

    private Animator animator;

    [SerializeField] string charname = ""; //캐릭터 구분 및 효과음 이름 구성용

    void Awake()
    {
        smokeShellCount = baseSmokeShellCount;
        defaultScale = transform.localScale;

#if UNITY_EDITOR
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
#endif
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerStat = GetComponent<PlayerStat>();
        Physics2D.gravity = new Vector2(0, gravityY);

        InitFieldCollection();

        animator = GetComponent<Animator>(); //애니메이션
        animator.SetTrigger("IsJumping");
    }

    public void SetSkillCounts(int count1, int count2)
    {
        if (moonlightSkill != null) moonlightSkill.AddCharge(count1 - moonlightSkill.CurrentCharges);
        AddSmokeShell(count2 - smokeShellCount);
    }

    /// <summary>
    /// situation의 앞 글자는 대문자, Jump, Slide, Hurt(피격) 등
    /// </summary>
    /// <param name="situation"></param>
    public void PlaySfx(string situation)
    {
        string sfxClipName = "sfx" + charname + situation;
        SoundManager.Instance.PlaySFX(sfxClipName, 0.6f);
    }

    public void SetMoonLight(MoonlightSkill script)
    {
        moonlightSkill = script;
    }
    public void InitFieldCollection()
    {
        StartCoroutine(CollectFields());
    }

    IEnumerator CollectFields()
    {
        yield return null;  // Collider 수집을 한 프레임 늦게 시작

        // Field 태그 가진 모든 오브젝트의 Collider2D 수집
        var fields = GameObject.FindGameObjectsWithTag("Field");
        var colsList = new System.Collections.Generic.List<Collider2D>();
        foreach (var go in fields)
        {
            string layerName = LayerMask.LayerToName(go.layer);
            if (layerName != "1stMainFloor")
                colsList.AddRange(go.GetComponents<Collider2D>());
        }
        fieldCols = colsList.ToArray();
    }

    public void TryUseSmokeShell()
    {
        if (smokeShellCount <= 0 || playerStat.IsInvincible()) return;
        Instantiate(SmokeEffect, playerStat.GetCenterPos(), Quaternion.identity);
        playerStat.makeInvincible(playerStat.invincibilityTime); // 플레이어 무적 처리
        playerStat.triggerHitBlink(playerStat.invincibilityTime, false);
        smokeShellCount--;
    }

    public bool IsSmokeShellAvailable()
    {
        return smokeShellCount > 0 && !playerStat.IsInvincible();
    }

    public int GetSmokeShellCount()
    {
        return smokeShellCount;
    }

    public void AddSmokeShell(int amount)
    {
        smokeShellCount += amount;
        if (smokeShellCount > maxSmokeShellCount)
            smokeShellCount = maxSmokeShellCount;
    }

    void Update()
    {
        if (Time.timeScale == 0 || PlayerManager.Instance.playerActionable == false)
        {
            ClearTouch();
            return;
        }

        if (isSliding)
        {
            lastSlideTime = Time.time;
        }

        if (Touchscreen.current != null)
        {
            bool anyTouch = false;
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed) anyTouch = true;

                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                var phase = touch.phase.ReadValue();

                bool isJumpArea = pos.x < Screen.width * 0.5f;
                bool isSlideArea = !isJumpArea;
                bool isUIArea = EventSystem.current.IsPointerOverGameObject(id);
                bool isDropCondition = !isDropping && Time.time - lastSlideTime <= dropWindow;

                // 점프랑 드롭은 터치 시작 위치가 해당 영역이어야 함. 슬라이드는 터치가 영역 안에 감지되면 함.

                // 터치 시작
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // 점프 시작
                    if (isJumpArea && !isUIArea)
                    {
                        if (!jumpTouchIds.Contains(id))
                        {
                            TryJump();
                            jumpTouchIds.Add(id);
                        }
                    }

                    // 슬라이드/드롭 시작
                    else if (isSlideArea && !isUIArea)
                    {
                        if (!isSliding)
                        {
                            StartSlide();
                        }

                        if (!slideTouchIds.Contains(id))
                        {
                            if (isDropCondition)
                            {
                                StartDrop();
                            }
                            slideTouchIds.Add(id);
                        }
                    }
                }

                // 터치 유지 혹은 이동
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    // 슬라이드 시작
                    if (isSlideArea && !isUIArea)
                    {
                        if (!isSliding)
                        {
                            StartSlide();
                        }
                        slideTouchIds.Add(id);
                    }

                    // 슬라이드 종료
                    else
                    {
                        slideTouchIds.Remove(id);
                        if (slideTouchIds.Count <= 0)
                        {
                            EndSlide();
                        }
                    }
                }

                // 터치 종료
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    jumpTouchIds.Remove(id);
                    slideTouchIds.Remove(id);
                    if (slideTouchIds.Count <= 0)
                    {
                        EndSlide();
                    }
                }
            }

            if (!anyTouch)
            {
                ClearTouch();
            }
        }
        else
        {
            Debug.Log("TouchScreen.current is null");
            ClearTouch();
        }
    }

    public void ClearTouch()
    {
        jumpTouchIds.Clear();
        slideTouchIds.Clear();
        if (isSliding) EndSlide();
    }

    public void TryActivateMoonLight()
    {
        moonlightSkill.TryActivate();
    }

    public int GetMoonSkillCount()
    {
        if (moonlightSkill != null)
        {
            return moonlightSkill.CurrentCharges;
        }
        else return 0;
    }

    public bool IsMoonSkillAvailable()
    {
        if (moonlightSkill != null)
        {
           return !moonlightSkill.IsActive && GetMoonSkillCount() > 0;
        }
        return false;
    }
    private void TryJump()
    {
        if (jumpCount < maxJumpCount)
        {
            isJumping = true;
            rb.linearVelocityY = 0f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;

            lockLandingUntil = Time.time + landLockout;

            // 슬라이드/드롭 상태 해제
            if (isSliding)
            {
                EndSlide();
            }
            if (isDropping)
            {
                EndDrop();
            }

            PlaySfx("Jump");
            animator.ResetTrigger("IsRunning");
            animator.ResetTrigger("IsSliding");
            animator.SetTrigger("IsJumping");
        }
    }

    public void ResetJumpCount()
    {
        jumpCount = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Field"))
            return;

        // 진정한 착지만 점프 리셋
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)    // ***0.7->0.5로 바꿔봤음
            {
                if (Time.time <= lockLandingUntil || rb.linearVelocityY > 0.01f)
                    break;

                jumpCount = 0;
                if (isJumping)
                {
                    isJumping = false;

                    animator.ResetTrigger("IsJumping");
                    if (slideQueued)
                    {
                        StartSlide();
                    }
                    else animator.SetTrigger("IsRunning");
                }

                if (isDropping)
                {
                    // 드롭 도중에 착지하면 즉시 드롭 상태 해제
                    EndDrop();
                }

                break;
            }
        }
    }

    // 일반 모션 <-> 슬라이드 모션 전환 시 순간적으로 isGround가 false가 되어, 점프나 드롭이 씹히는 현상이 발생했음.
    // isGround의 사용처가 dropCondition 판정 뿐이었고, 이 조건이 없어도 드롭 로직에 문제가 없기 때문에 isGround 변수 자체를 제거함.
    //void OnCollisionExit2D(Collision2D collision)
    //{
    //    if (!collision.gameObject.CompareTag("Field"))
    //        return;
    //    isGround = false;
    //}

    void StartSlide()
    {
        if (isSliding) return;
        if (isJumping)
        {
            slideQueued = true;
            return;
        }
        slideQueued = false;
        isSliding = true;
        transform.localScale = new Vector3(defaultScale.x * slideIncreaseX, defaultScale.y * slideShrinkY, defaultScale.z);

        PlaySfx("Slide");
        animator.ResetTrigger("IsRunning");
        animator.SetTrigger("IsSliding");
    }

    void EndSlide()
    {
        slideQueued = false;
        if (!isSliding)
        {
            return;
        }

        isSliding = false;
        transform.localScale = defaultScale;

        animator.ResetTrigger("IsSliding");
        animator.SetTrigger("IsRunning");
    }

    // 드롭 시 Player↔Field 충돌 무시 시작
    private void StartDrop()
    {
        if (isDropping || gameObject.layer == LayerMask.NameToLayer("P1stMainFloor"))
            return;

        isDropping = true;
        foreach (var fCol in fieldCols)
        {
            if (fCol.gameObject.layer == gameObject.layer - 4)
            {
                Physics2D.IgnoreCollision(col, fCol, true);
            }
        }

        // 잠시 후 복구
        Invoke("EndDrop", ignoreDuration);
    }

    private void EndDrop()
    {
        if (!isDropping) return;

        foreach (var fCol in fieldCols)
            Physics2D.IgnoreCollision(col, fCol, false);
        isDropping = false;
    }
}
