using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BgmSliderControler : MonoBehaviour
{
   public void OnSliderChaged()
   {
        UIManager.UI.OnBgmSliderChanged();
   }
}
