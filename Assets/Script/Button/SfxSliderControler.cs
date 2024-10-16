using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SfxSliderControler : MonoBehaviour
{
    public void OnSliderChanged()
    {
        UIManager.UI.OnSfxSliderChanged();
    }
}
