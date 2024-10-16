using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleButtonControler : MonoBehaviour
{
    public void OnSingleButtonClick()
    {
        UIManager.UI.OnSingleClick();
    }
}
