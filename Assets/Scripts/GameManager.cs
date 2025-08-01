using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void LeaveGame()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
