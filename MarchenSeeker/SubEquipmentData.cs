using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Sub Equipment Data")]
public class SubEquipmentData : ScriptableObject
{
    public enum Category { invalid, normal, special, evolved, passive }
    public enum BulletCategory { invalid, linear, radial, rotate, aiming, lockon, none }

    public string subEquipID;
    public string nameString;
    public Category category;
    public Sprite subEquipResource;
    public Sprite bulletResource;
    public int ableStage;
    public bool penetrate;
    public BulletCategory bulletCategory;
    public bool hasAdditionalEffect;
    public bool isComeBack;

    public GameObject prefab;
}
