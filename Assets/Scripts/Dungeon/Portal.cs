using UnityEngine;

/// <summary>
/// 룸 간 이동 포탈. 룸 내 Portal 오브젝트에 부착.
/// Collider(isTrigger)가 필요하다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    private DungeonManager dungeonManager;

    void Start()
    {
        dungeonManager = FindObjectOfType<DungeonManager>();

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (dungeonManager != null)
                dungeonManager.MoveToNextRoom();
        }
    }
}
