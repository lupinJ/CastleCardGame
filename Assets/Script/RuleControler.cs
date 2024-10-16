using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleControler 
{
    List<int> Card;

    RuleControler()
    {
        this.Card = new List<int>();
    }

    public void SetCard(List<int> Card)
    {
        this.Card = Card;
    }

   
}
