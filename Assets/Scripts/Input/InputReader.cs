using UnityEngine;
using UnityEngine.InputSystem;
public class InputReader : MonoBehaviour
{
    private PlayerInput playerInput;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    private void OnEnable()
    {
        playerInput.actions["Movement"].performed += gaaa;
    }
    private void gaaa(InputAction.CallbackContext context)
    {

    }
    private void OnDisable()
    {
        
    }
}
