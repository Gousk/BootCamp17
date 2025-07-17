using UnityEngine;

public class ChestOpener : MonoBehaviour
{
    private bool isOpen = false;

    public GameObject lid; 

    void OnMouseDown()
    {
        if (!isOpen)
        {
            Debug.Log("Sandýk açýldý!");
            isOpen = true;

         
            if (lid != null)
            {
                lid.transform.Rotate(new Vector3(-90, 0, 0)); 
            }

            
            // GetComponent<Animator>().SetTrigger("Open");
        }
    }
}
