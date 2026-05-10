using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ElderCareModuleCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public ElderCareHomeMenu menu;
    public string moduleId;
    public string moduleTitle;
    public RectTransform cardTransform;
    public Graphic cardGraphic;
    public Graphic glowGraphic;
    public float hoverScale = 1.05f;
    public float pressedScale = 0.96f;
    public float animationSpeed = 10f;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.white;
    public Color glowColor = new Color(1f, 1f, 1f, 0.35f);

    private Button _button;
    private bool _hovered;
    private bool _pressed;

    private void Awake()
    {
        if (cardTransform == null)
        {
            cardTransform = transform as RectTransform;
        }

        _button = GetComponent<Button>();
        _button.onClick.RemoveListener(HandleClick);
        _button.onClick.AddListener(HandleClick);

        if (glowGraphic != null)
        {
            var glow = glowColor;
            glow.a = 0f;
            glowGraphic.color = glow;
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void Update()
    {
        if (cardTransform != null)
        {
            var targetScale = _pressed ? pressedScale : (_hovered ? hoverScale : 1f);
            cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * animationSpeed);
        }

        if (cardGraphic != null)
        {
            cardGraphic.color = Color.Lerp(cardGraphic.color, _hovered ? hoverColor : normalColor, Time.unscaledDeltaTime * animationSpeed);
        }

        if (glowGraphic != null)
        {
            var target = glowColor;
            target.a = _hovered ? glowColor.a : 0f;
            glowGraphic.color = Color.Lerp(glowGraphic.color, target, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    private void HandleClick()
    {
        if (menu != null)
        {
            menu.SelectModule(moduleId, moduleTitle);
        }
    }
}
