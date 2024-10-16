using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackButtonControler : MonoBehaviour
{
    public void OnBackButtonClick()
    {
        UIManager.UI.OnBackClick();
    }
}
