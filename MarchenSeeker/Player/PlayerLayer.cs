using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLayer : MonoBehaviour
{
    [SerializeField] string firstMainFloor = "P1stMainFloor";
    [SerializeField] string firstSubFloor = "P1stSubFloor";
    [SerializeField] string secondMainFloor = "P2ndMainFloor";
    [SerializeField] string secondSubFloor = "P2ndSubFloor";
    [SerializeField] string minusFloor = "IgnoreGround";
    [SerializeField] float firstMainHeight = 0f;
    [SerializeField] float firstSubHeight = 1f;
    [SerializeField] float secondMainHeight = 3.25f;
    [SerializeField] float secondSubHeight = 4.25f;
    [SerializeField] float heightOffset = 0.0625f;

    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameObject.layer = LayerMask.NameToLayer(firstMainFloor);
    }

    void FixedUpdate()
    {
        float yPos = rb.position.y;

        CheckLayer(minusFloor, firstMainFloor, yPos, firstMainHeight);
        CheckLayer(firstMainFloor, firstSubFloor, yPos, firstSubHeight);
        CheckLayer(firstSubFloor, secondMainFloor, yPos, secondMainHeight);
        CheckLayer(secondMainFloor, secondSubFloor, yPos, secondSubHeight);
    }

    void CheckLayer(string belowLayerName, string upperLayerName, float yPos, float upperFloorHeight)
    {
        if (gameObject.layer <= LayerMask.NameToLayer(belowLayerName) && yPos > upperFloorHeight) // 아래층에서 위층으로 올라가는 경우
        {
            gameObject.layer = LayerMask.NameToLayer(upperLayerName);
        }
        else if (gameObject.layer >= LayerMask.NameToLayer(upperLayerName) && yPos <= upperFloorHeight - heightOffset) // 위층에서 아래층으로 내려가는 경우
        {
            gameObject.layer = LayerMask.NameToLayer(belowLayerName);
        }
    }
}
