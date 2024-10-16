using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserInfoControler : MonoBehaviour
{
    [SerializeField] SpriteRenderer ClassImage;
    [SerializeField] TextMeshPro NameText;
    [SerializeField] TextMeshPro CardText;
    [SerializeField] TextMeshPro CoinText;

    public void InitInfo(string name, int card, int coin)
    {
        this.SetName(name);
        this.SetCard(card);
        this.SetCoin(coin);
    }
    public void SetCard(int count)
    {
        CardText.text = string.Format("{0}¿Â", count);
    }
    public void SetColor(int select)
    {
        switch(select)
        {
            case 0:
                NameText.color = new Color32(255, 255, 255, 255);
                break;
            case 1:
                NameText.color = new Color32(255, 255, 0, 255);
                break;
            default:
                break;
        }
        
    }
    public void SetCoin(int coin)
    {
        CoinText.text = string.Format("{0}∞≥", coin);
    }
    public void SetName(string name)
    {
        NameText.text = name;
    }
    public void SetClass(Sprite image)
    {
        ClassImage.sprite = image;
    }
 

}
