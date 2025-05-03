using UnityEngine;
using UnityEngine.UI;

public class ButtonLock : MonoBehaviour
{
    private Button button;
    private bool lastState;

    void Start()
    {
        button = GetComponent<Button>();
        lastState = button.interactable;
        HandleInteractableChange(button.interactable);
    }

    void Update()
    {
        if (button.interactable != lastState)
        {
            lastState = button.interactable;
            HandleInteractableChange(button.interactable);
        }
    }

    void HandleInteractableChange(bool isInteractable)
    {
        Transform secondChild = transform.GetChild(1);
        secondChild.gameObject.SetActive(!isInteractable);
    }
}
