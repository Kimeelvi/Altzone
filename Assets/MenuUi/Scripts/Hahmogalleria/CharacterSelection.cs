using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelection : MonoBehaviour
{
    public ScrollRect _scrollRect;
    public RectTransform contentPanel;
    public RectTransform sampleListItem;
    public HorizontalLayoutGroup horizontalLayoutGroup;

    private bool IsSnapped;

    float snapSpeed;
    public float snapForce = 1;

    private void Start()
    {
        IsSnapped = false;
    }

    private void Update()
    {
        int currentItem = Mathf.RoundToInt(0 - contentPanel.localPosition.x / (sampleListItem.rect.width + horizontalLayoutGroup.spacing));

        if (_scrollRect.velocity.magnitude < 200 && !IsSnapped)
        {
            _scrollRect.velocity = Vector2.zero;
            snapSpeed += snapForce * Time.deltaTime;
            float targetX = 0 - (currentItem * (sampleListItem.rect.width + horizontalLayoutGroup.spacing));
            contentPanel.localPosition = new Vector3(Mathf.MoveTowards(contentPanel.localPosition.x, targetX, snapSpeed), contentPanel.localPosition.y, contentPanel.localPosition.z);

            if (Mathf.Approximately(contentPanel.localPosition.x, targetX))
            {
                IsSnapped = true;

                // Character selection logic
                SelectCharacter(currentItem);
            }
        }

        if (_scrollRect.velocity.magnitude > 200)
        {
            IsSnapped = false;
            snapSpeed = 0;
        }
    }

    // Handle character selection
    void SelectCharacter(int selectedCharacterIndex)
    {
        // Add your logic to handle character selection here
        Debug.Log("Selected Character Index: " + selectedCharacterIndex);
    }
}
