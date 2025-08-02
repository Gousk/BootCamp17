using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private RaceManager raceManager;

    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();   
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;         // Sadece oyuncular

        PlayerID pid = other.GetComponent<PlayerID>();   
        if (pid != null)
        {
            raceManager.PlayerFinished(pid.id);          // Sýralamaya bildir
        }
    }
}

