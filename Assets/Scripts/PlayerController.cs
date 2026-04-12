using UnityEngine;

public class PlayerController : MonoBehaviour
{


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 MovInput = playerInput.actions["Move"].ReadValue<Vector2>();
    }
}
