using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBackButtonControler : MonoBehaviour
{
   public void OnButtonClicked()
   {
        GameManager.Play.OnBackButtonClicked();
   }
}
