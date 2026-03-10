using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

    [Header("Monsters")]
    public List<GameObject> monsters = new List<GameObject>();


    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        Debug.Log($"[BattleRoom] Initialized with {monsters.Count} monster(s).");
    }

    void Update()
    {
        if (isLocked && !isCleared)
        {
            CheckMonsters();
        }
    }


    public void OnPlayerEnter()
    {

    }

    private void CheckMonsters()
    {
        monsters.RemoveAll(m => m == null);

        if (monsters.Count == 0)
        {
            ClearRoom();
        }
    }

    private void ClearRoom()
    {
        isLocked = false;
        isCleared = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnter();
        }
    }
}