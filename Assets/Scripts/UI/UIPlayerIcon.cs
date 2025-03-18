using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerIcon : MonoBehaviour
{
    public Image Icon;
    public TMP_Text Text;
    public Sprite SpriteX;
    public Sprite SpriteO;
    public Color ColorX;
    public Color ColorO;

    public void Init(PlayerId id, string name)
    {
        Text.text = name;
        if (id == PlayerId.X)
        {
            Icon.sprite = SpriteX;
            Text.color = ColorX;
        }
        else if (id == PlayerId.O)
        {
            Icon.sprite = SpriteO;
            Text.color = ColorO;
        }
    }
}
