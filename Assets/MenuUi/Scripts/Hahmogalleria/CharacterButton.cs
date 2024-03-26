using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    public Image targetImage; // the Image component you want to change

    // Function to be called when the button is clicked
    public void ChangeImage()
    {
        // Get the button's sprite
        Sprite newSprite = GetComponent<Image>().sprite;

        // Set the image's sprite to the button's sprite
        targetImage.sprite = newSprite;
    }
}
