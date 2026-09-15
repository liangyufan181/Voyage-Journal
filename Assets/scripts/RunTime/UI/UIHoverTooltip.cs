using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(2, 4)]
    [SerializeField] private string itemDescription;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TradeDialogView.Instance != null)
        {
            TradeDialogView.Instance.ShowTooltip(itemDescription, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TradeDialogView.Instance != null)
        {
            TradeDialogView.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (TradeDialogView.Instance != null)
        {
            TradeDialogView.Instance.HideTooltip();
        }
    }
}