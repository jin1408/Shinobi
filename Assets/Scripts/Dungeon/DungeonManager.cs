using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 던전 전체를 관리하는 매니저. 씬에 빈 오브젝트로 배치.
/// Room 배열에 룸들을 순서대로 등록한다.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    [Header("룸 목록 (순서대로)")]
    public Room[] rooms;

    [Header("플레이어")]
    public Transform player;

    [Header("페이드 설정")]
    public float fadeDuration = 0.5f;

    private int currentRoomIndex = 0;
    private bool isTransitioning = false;

    public event Action OnDungeonCleared;
    public event Action<int> OnRoomChanged;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            rooms[i].OnRoomCleared += OnRoomCleared;

            if (i != 0)
                rooms[i].DeactivateRoom();
        }

        if (rooms.Length > 0)
        {
            rooms[0].ActivateRoom();
            MovePlayerToRoom(rooms[0]);
        }
    }

    void OnRoomCleared(Room room)
    {
        if (currentRoomIndex >= rooms.Length - 1)
        {
            OnDungeonCleared?.Invoke();
            Debug.Log("던전 클리어!");
        }
    }

    public void MoveToNextRoom()
    {
        if (isTransitioning) return;
        if (currentRoomIndex >= rooms.Length - 1) return;

        StartCoroutine(TransitionToNextRoom());
    }

    IEnumerator TransitionToNextRoom()
    {
        isTransitioning = true;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.canMove = false;

        yield return new WaitForSeconds(fadeDuration);

        rooms[currentRoomIndex].DeactivateRoom();
        currentRoomIndex++;
        rooms[currentRoomIndex].ActivateRoom();
        MovePlayerToRoom(rooms[currentRoomIndex]);

        OnRoomChanged?.Invoke(currentRoomIndex);

        yield return new WaitForSeconds(fadeDuration);

        if (pc != null)
            pc.canMove = true;

        isTransitioning = false;
    }

    void MovePlayerToRoom(Room room)
    {
        if (room.playerSpawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            player.position = room.playerSpawnPoint.position;
            player.rotation = room.playerSpawnPoint.rotation;

            if (cc != null)
                cc.enabled = true;
        }
    }
}
