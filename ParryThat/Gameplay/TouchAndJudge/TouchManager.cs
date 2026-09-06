using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Touched
{
    public Direction direction;
    public double touchSec;
    public AttackType type;

    public Touched(Direction _direction, double _touchSec, AttackType _type)
    {
        direction = _direction;
        touchSec = _touchSec;
        type = _type;
    }
}

public class TouchManager : MonoBehaviour
{
    [SerializeField] private JudgeSystem judgeSystem;   // ScoreManager -> JudgeSystem
    [SerializeField] public PlayerManager playerManager;
    [SerializeField] private bool isTouchAvailable = false;

    private Vector3 initialPos;
    private Vector3 lastPos;
    private bool isSwiping = false;
    private double sumLength = 0;
    private double swipeStartTime;

    private bool isTapAndSwipe = false;
    private Direction previousDirection;
    private Touch tempTouchs;

    // 입력 감도 상수
    private const float MOUSE_SWIPE_SENSITIVITY = 40f;
    private const float TOUCH_SWIPE_SENSITIVITY = 0.25f;
    private const float SWIPE_TIME_THRESHOLD = 0.1f;

    void Update()
    {
        bool canControl = !StageFlowManager.Instance.isTutorial 
                            && !StageFlowManager.Instance.isPaused 
                            && !StageFlowManager.Instance.is_over 
                            && !StageFlowManager.Instance.isClear;
        if (canControl)
        {
            if (isTouchAvailable)
            {
                TouchChecker();
            }
            else
            {
                //KeyChecker();  // Legacy
                MouseChecker();  // 터치에 중복
            }
        }
    }

    private void KeyChecker()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SendJudge(Direction.Left, StageFlowManager.Instance.currentTime, AttackType.Strong);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SendJudge(Direction.Right, StageFlowManager.Instance.currentTime, AttackType.Strong);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            SendJudge(Direction.Up, StageFlowManager.Instance.currentTime, AttackType.Strong);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SendJudge(Direction.Down, StageFlowManager.Instance.currentTime, AttackType.Strong);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            SendJudge(Direction.None, StageFlowManager.Instance.currentTime, AttackType.Normal);
        }
    }

    private void MouseChecker()
    {
        if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            OnInputStart(mousePos, true);
        }
        else if (Input.GetMouseButton(0))
        {
            if (!isSwiping) OnInputStart(mousePos, false);
            else OnInputUpdate(mousePos, MOUSE_SWIPE_SENSITIVITY);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnInputEnd();
        }

        // 마우스 클릭 해제 시점이 아니더라도 입력이 물리적으로 끊기면 종료 처리
        if (!Input.GetMouseButton(0))
        {
            if (isSwiping) OnInputEnd();
        }
    }

    private void TouchChecker()
    {
        if (Input.touchCount <= 0)
        {
            if (isSwiping) OnInputEnd();
            return;
        }

        tempTouchs = Input.GetTouch(0);

        if (tempTouchs.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(tempTouchs.fingerId))
        {
            return;
        }

        Vector3 touchPos = Camera.main.ScreenToWorldPoint(tempTouchs.position);

        switch (tempTouchs.phase)
        {
            case TouchPhase.Began:
                OnInputStart(touchPos, true);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!isSwiping) OnInputStart(touchPos, false);
                else OnInputUpdate(touchPos, TOUCH_SWIPE_SENSITIVITY);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnInputEnd();
                break;
        }
    }

    private void OnInputStart(Vector3 position, bool isTap)
    {
        initialPos = position;
        lastPos = position;
        sumLength = 0;
        isSwiping = true;
        isTapAndSwipe = isTap;
        swipeStartTime = StageFlowManager.Instance.currentTime;

        if (isTap)
        {
            SendJudge(Direction.None, StageFlowManager.Instance.currentTime, AttackType.Normal);
            previousDirection = Direction.None;
        }
    }

    private void OnInputUpdate(Vector3 currentPos, float sensitivity)
    {
        if (!isSwiping) return;

        sumLength += Vector3.Distance(lastPos, currentPos);
        lastPos = currentPos;

        // 시간 초과 체크
        if (StageFlowManager.Instance.currentTime - swipeStartTime > SWIPE_TIME_THRESHOLD)
        {
            isSwiping = false;
            return;
        }

        // 거리 달성(스와이프) 체크
        if (sumLength > sensitivity)
        {
            isSwiping = false;
            Direction tempDirection = CalculateDirection(lastPos - initialPos);

            if (isTapAndSwipe || previousDirection != tempDirection)
            {
                SendJudge(tempDirection, StageFlowManager.Instance.currentTime, AttackType.Strong);
                previousDirection = tempDirection;
            }
        }
    }

    private void OnInputEnd()
    {
        isSwiping = false;
        SendJudge(Direction.None, StageFlowManager.Instance.currentTime, AttackType.HoldStop);
    }


    private Direction CalculateDirection(Vector2 delta)
    {
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (angle > 150 || angle <= -150) return Direction.Left;
        if (angle > 30) return Direction.Up;
        if (angle > -30) return Direction.Right;
        return Direction.Down;
    }

    private void SendJudge(Direction? _judgeDirection, double _judgeTime, AttackType _type)
    {
        if (_judgeDirection.HasValue && judgeSystem != null)
        {
            judgeSystem.touchQueue.Enqueue(new Touched((Direction)_judgeDirection, _judgeTime, _type));
        }
    }
}
