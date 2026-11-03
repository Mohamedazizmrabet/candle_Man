using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// One main menu entry. Hovering with the mouse selects it (like moving there with the keyboard),
    /// and selecting it tells the MainMenuController to move the highlight frame here.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private MainMenuController controller;

        public Button Button { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        private void Awake() => Button = GetComponent<Button>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button.interactable) Button.Select();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (controller != null) controller.OnButtonSelected(this);
        }
    }
}
