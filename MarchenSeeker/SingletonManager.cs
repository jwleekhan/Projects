using UnityEngine;

public class SingletonManager : MonoBehaviour
{
    public static SingletonManager Instance { get; private set; }

    [SerializeField] GameObject[] singletonPrefabs;

    void Awake()
    {
        Debug.Log($"singletone manager는 {gameObject.name}에 담겨 있음!!");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InstantiateSingletonObjects();
    }

    void InstantiateSingletonObjects()
    {
        foreach (var prefab in singletonPrefabs)
        {
            if (prefab != null)
            {
                Instantiate(prefab).name = prefab.name;
            }
        }
    }
}
