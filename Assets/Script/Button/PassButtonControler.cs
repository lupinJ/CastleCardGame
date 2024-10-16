using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassButtonControler : MonoBehaviour
{
    public void PassButtonClicked()
    {
        GameManager.Play.TurnPass();
    }

}
