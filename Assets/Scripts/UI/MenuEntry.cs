using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// One main menu entry. Hovering with the mouse selects it (like moving there with the keyboard),
    /// and selecting it moves the MenuHighlight frame here.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class MenuEntry : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private MenuHighlight highlight;

        private Selectable selectable;

        public Selectable Selectable => selectable != null ? selectable : selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Selectable.interactable) Selectable.Select();
        }

        public void OnSelect(BaseEventData eventData) => MoveHighlightHere();

        public void MoveHighlightHere()
        {
            if (highlight != null) highlight.MoveTo((RectTransform)transform);
        }
    }
}
