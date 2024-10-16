using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CardControler : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer CardFront, CardBack;

    [SerializeField]
    float rotateTime = 0.2f;
    
    int Number = 0;
    int Order = 0;
    bool Cardcolor = true;
   

    public void SetCard(int number, int order, Sprite image) // inisialize
    {
        CardFront.sprite = image;
        this.Number = number;
        this.Order = order;

    }

    public void MoveCard(int mode) // 카드를 움직인다.
    {
        switch(mode)
        {
            case 0:
                transform.position += new Vector3(0, 0.5f, 0);
                break;
            case 1:
                transform.position -= new Vector3(0, 0.5f, 0);
                break;
            default:
                break;
        }
    }
    public void MoveCard(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, Order);
    }
    public void MoveCard(Vector3 position)
    {
        transform.position = position;
        Order = (int)position.z;
    }
    public void MoveOrder(int order)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, order);
    }
    public int GetOrder() { return Order; }
    void SetColor() // enable color
    {
        if (Cardcolor)
            CardFront.color = Color.gray;
        else
            CardFront.color = Color.white;

        Cardcolor = !Cardcolor;
    }

    private void OnMouseDown() // 카드 클릭 이벤트
    {
        if(Cardcolor)
            GameManager.Play.CardClicked(Number);

    }

    private IEnumerator CardRotate(float CardAngle = 180.0f) // 카드뒤집기
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < rotateTime)
        {
            this.transform.Rotate(0.0f, CardAngle * Time.deltaTime / rotateTime, 0.0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }


}
