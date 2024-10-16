using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomAttendButtonControler : MonoBehaviour
{
    public void OnButtonClicked()
    {
        UIManager.UI.RoomAttend();
    }
}
