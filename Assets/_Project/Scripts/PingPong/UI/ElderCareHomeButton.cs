using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ElderCareHomeButton : MonoBehaviour
{
    public ElderCareHomeMenu menu;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveListener(HandleClick);
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (menu != null)
        {
            menu.ShowHome();
        }
    }
}
