using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmitButtonControler : MonoBehaviour
{
    public void SubmitButtonClicked()
    {
        GameManager.Play.CardSubmit();
    }
}
