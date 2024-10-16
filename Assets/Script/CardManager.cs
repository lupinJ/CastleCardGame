using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;



public class CardManager : MonoBehaviour
{

    [SerializeField] CardData CardData; // 스프라이트 이미지
    [SerializeField] GameObject CardPrefab;

    Sprite[] CardImage; // 트럼프 이미지

    List<int> Deck; // 카드 전체
    List<int>[] PlayerDeck; // 각 플레이어가 가진 카드
    List<int> ClickDeck; // 클릭된 카드들
    List<int> FieldDeck; // 맨위 필드 카드를 저장
    List<int> BotDeck; // 봇의 선택 카드

    List<GameObject> HandCard; // prefab save
    List<GameObject> FieldCard;
   

    const int CARD_MAX = 54; // 총 카드수
    const int USER_MAX = 4; // 총 유저수
    int PID; // 고유 pid
    bool revolution = false; // 혁명 상태
    
    int[] fieldstate; // [0] = {1 2 3 카드수 5 6 계단(계단은 삭제됨) }, [1] = 숫자
    int[] handstate;
    int[] botstate;
    int[] taxtemp; // 세금 임시 저장 // 0,1 = 광대 2 = 평민 3 = 귀족 4,5 = 왕
  
    public void SetDeck(int pid) // 초기화
    {
        CardImage = CardData.CardSprite;
        this.PID = pid;
        InitDeck(); // 변수 초기화
    }
    public void ClickCard(int number) // 카드가 클릭 되었을 때 발생
    {
        int index = this.IsHand(PID, number);
        if (index == -1) // 손패가 아닌 경우
            return;

        if (this.IsCardClicked(number)) // 눌렀던 카드 인 경우
        {
            ClickDeck.Remove(number);
            HandCard[index].GetComponent<CardControler>().MoveCard(1);
        }
        else
        {
            ClickDeck.Add(number);
            ClickDeck.Sort();

            HandCard[index].GetComponent<CardControler>().MoveCard(0);
        }
        

    }
    public int AutoPlay(int other) // 자동 플레이 0 = pass
    {
        int number = 0, index = 0, joker = 0;
        int[] cardcount = new int[14];
        for (int i = 0; i < cardcount.Length; i++)
            cardcount[i] = 0;

        if (fieldstate[0] > 4) // 계단제외
            return 0;

        for (int i = 0; i < PlayerDeck[other].Count; i++) // 카드 카운팅
        {
            cardcount[PlayerDeck[other][i] / 4]++;
        }

        number = CheckBotCard(other, 0); // 조커 0장 조합
        
        if (number == -1 && cardcount[13] > 0) // 조커 1장 조합
        {
            number = CheckBotCard(other, 1);
            joker = 1;
        }
        if (number == -1 && cardcount[13] > 1) // 조커 2장 조합
        {
            number = CheckBotCard(other, 2);
            joker = 2;
        }
        if (number == -1) // 없으면 pass
            return 0;

        if(revolution)
        {
            for (int i = 0; i < PlayerDeck[other].Count; i++)
            {
                if (PlayerDeck[other][i] / 4 == number)
                {
                    index = i;
                    break;
                }
            }
            if (fieldstate[0] == 0)
            {
                for (int i = 0; i < cardcount[number]; i++)
                {
                    BotDeck.Add(PlayerDeck[other][index + i]);
                }
            }
            else
            {
                for (int i = 0; i < fieldstate[0] - joker; i++)
                {
                    BotDeck.Add(PlayerDeck[other][index + i]);
                }
                for(int i = 0; i < joker; i++)
                    BotDeck.Add(PlayerDeck[other][PlayerDeck[other].Count - i - 1]);
            }
            botstate[1] = BotDeck[0];
        }
        else
        {
            for (int i = PlayerDeck[other].Count - 1; i > -1; i--)
            {
                if ((PlayerDeck[other][i] / 4) == number)
                {
                    index = i;
                    break;
                }
            }
            if (fieldstate[0] == 0)
            {
                for (int i = 0; i < cardcount[number]; i++)
                {
                    BotDeck.Add(PlayerDeck[other][index - i]);
                }
            }
            else
            {
                for (int i = 0; i < fieldstate[0] - joker; i++)
                {
                    BotDeck.Add(PlayerDeck[other][index - i]);
                }
                for (int i = 0; i < joker; i++)
                    BotDeck.Add(PlayerDeck[other][PlayerDeck[other].Count - i - 1]);
            }
            if(number == 13)
                botstate[1] = BotDeck[BotDeck.Count - 1];
            else
                botstate[1] = BotDeck[BotDeck.Count - joker - 1];
        }
       
        if (fieldstate[0] == 0)
            botstate[0] = cardcount[number];
        else
            botstate[0] = fieldstate[0];
        
        BotDeck.Sort();

        if (botstate[0] == 4 && IsCardEight(botstate[1])) // 혁명 + 8스킵
            return 9;
        if (botstate[1] == 3 && IsJoker(fieldstate[1])) // 3 받아치기
            return 3;
        if (botstate[0] == 4) // 혁명
            return 2;
        if (IsCardEight(botstate[1])) // 8스킵
            return 8;
        return 1;
        
    }
    public void AutoTax(int pid, int rank) // 자동 납세
    {
        switch (rank)
        {
            case 0:
                taxtemp[0] = PlayerDeck[pid][PlayerDeck[pid].Count - 2];
                taxtemp[1] = PlayerDeck[pid][PlayerDeck[pid].Count - 1];
                break;
            case 1:
                taxtemp[2] = PlayerDeck[pid][PlayerDeck[pid].Count - 1];
                break;
            case 2:
                taxtemp[3] = PlayerDeck[pid][0];
                break;
            case 3:
                taxtemp[4] = PlayerDeck[pid][0];
                taxtemp[5] = PlayerDeck[pid][1];
                break;
        }

    }
    public int WhoFirst() // 클로버 3 소유자가 먼저 시작
    {
        int error = -1;

        for (int i = 0; i < USER_MAX; i++)
        {
            for (int j = 0; j < PlayerDeck[i].Count; j++)
            {
                if (PlayerDeck[i][j] == 0)
                    return i;
            }
        }
        return error;
    }
    public bool TaxCheck(int pid, int rank) // tax 유효성 검사
    {
        switch (rank)
        {
            case 0:
                if (ClickDeck.Count == 2 && ClickDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 2] && ClickDeck[1] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                {
                    return true;
                }
                break;
            case 1:
                if (ClickDeck.Count == 1 && ClickDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                {
                    return true;
                }
                break;
            case 2:
                if (ClickDeck.Count == 1)
                {
                    return true;
                }
                break;
            case 3:
                if (ClickDeck.Count == 2)
                {
                    return true;
                }
                break;
        }
        return false;
    }
    public int TaxSubmit(int pid, int rank, string message = "") // 세금 지불
    {
        int index = -1;

        if(PID == pid)
        {
            switch (rank)
            {
                case 0:
                    if (ClickDeck.Count == 2 && ClickDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 2] && ClickDeck[1] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                    {
                        taxtemp[0] = ClickDeck[0];
                        taxtemp[1] = ClickDeck[1];
                        for(int i=0; i<2; i++)
                        {
                            index = IsHand(pid, taxtemp[i]);
                            Destroy(HandCard[index]);
                            HandCard.RemoveAt(index);
                            PlayerDeck[pid].RemoveAt(index);
                        }

                        ClickDeck.Clear();
                        return 0;
                    }
                    break;
                case 1:
                    if (ClickDeck.Count == 1 && ClickDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                    {
                        taxtemp[2] = ClickDeck[0];
                        index = IsHand(pid, taxtemp[2]);
                        Destroy(HandCard[index]);
                        HandCard.RemoveAt(index);
                        PlayerDeck[pid].RemoveAt(index);

                        ClickDeck.Clear();
                        return 1;
                    }
                    break;
                case 2:
                    if (ClickDeck.Count == 1)
                    {
                        taxtemp[3] = ClickDeck[0];
                        index = IsHand(pid, taxtemp[3]);
                        Destroy(HandCard[index]);
                        HandCard.RemoveAt(index);
                        PlayerDeck[pid].RemoveAt(index);

                        ClickDeck.Clear();
                        return 2;
                    }
                    break;
                case 3:
                    if (ClickDeck.Count == 2)
                    {
                        taxtemp[4] = ClickDeck[0];
                        taxtemp[5] = ClickDeck[1];
                        for (int i = 4; i < 6; i++)
                        {
                            index = IsHand(pid, taxtemp[i]);
                            Destroy(HandCard[index]);
                            HandCard.RemoveAt(index);
                            PlayerDeck[pid].RemoveAt(index);
                        }

                        ClickDeck.Clear();
                        return 3;
                    }
                    break;
            }

            
        }
        else
        {
            BotDeck = StringToList(message);

            switch (rank)
            {
                case 0:
                    if (BotDeck.Count == 2 && BotDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 2] && BotDeck[1] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                    {
                        taxtemp[0] = BotDeck[0];
                        taxtemp[1] = BotDeck[1];
                        BotDeck.Clear();
                        return 0;
                    }
                    break;
                case 1:
                    if (BotDeck.Count == 1 && BotDeck[0] == PlayerDeck[pid][PlayerDeck[pid].Count - 1])
                    {
                        taxtemp[2] = BotDeck[0];
                        BotDeck.Clear();
                        return 1;
                    }
                    break;
                case 2:
                    if (BotDeck.Count == 1)
                    {
                        taxtemp[3] = BotDeck[0];
                        BotDeck.Clear();
                        return 2;
                    }
                    break;
                case 3:
                    if (BotDeck.Count == 2)
                    {
                        taxtemp[4] = BotDeck[0];
                        taxtemp[5] = BotDeck[1];
                        BotDeck.Clear();
                        return 3;
                    }
                    break;
            }

        }

        return -1; 
    }
    public void TaxSwap(int pid, int rank) // 세금 분배
    {
        switch(rank)
        {
            case 0:
                if(PID != pid)
                {
                    PlayerDeck[pid].Remove(taxtemp[0]);
                    PlayerDeck[pid].Remove(taxtemp[1]);
                }
                PlayerDeck[pid].Add(taxtemp[4]);
                PlayerDeck[pid].Add(taxtemp[5]);
                break;
            case 1:
                if (PID != pid)
                    PlayerDeck[pid].Remove(taxtemp[2]);
                PlayerDeck[pid].Add(taxtemp[3]);
                break;
            case 2:
                if (PID != pid)
                    PlayerDeck[pid].Remove(taxtemp[3]);
                PlayerDeck[pid].Add(taxtemp[2]);
                break;
            case 3:
                if (PID != pid)
                {
                    PlayerDeck[pid].Remove(taxtemp[4]);
                    PlayerDeck[pid].Remove(taxtemp[5]);
                }
                PlayerDeck[pid].Add(taxtemp[0]);
                PlayerDeck[pid].Add(taxtemp[1]);
                break;
            default:
                break;
        }
        PlayerDeck[pid].Sort();

        if (PID == pid)
        {
            for (int i = 0; i < HandCard.Count; i++) // 필드를 비운다
                Destroy(HandCard[i]);
            HandCard.Clear();
            ClickDeck.Clear();
            DrawMyCard();
        }
    }
    public int Isvalid() // 클릭한 카드들을 낼수 있는지 확인
    {
        
        ClickDeck.Sort();
        handstate[0] = ClickDeck.Count;

        if (handstate[0] < 1 || handstate[0] > 5) // 개수 오버
            return -1;
        if (fieldstate[0] != 0  && handstate[0] != fieldstate[0]) // 필드가 0이 아니거나 필드 개수가 다름
            return -1;
        
        if (IsSameNumber()) // 같은 숫자 조합
        {
            HandStateCheck(false);

            if (fieldstate[0] != 0) // 필드에 카드가 있으면
            { 
                if (!revolution && !IsJokerReflect(handstate[1], fieldstate[1]) && handstate[1] < fieldstate[1]) // 낮으면 낼수 없다
                    return -1;
                if (revolution && !IsJoker(handstate[1]) && handstate[1] > fieldstate[1])
                    return -1;
                if (handstate[1] != 3 && IsJoker(fieldstate[1])) // 필드가 조커이면 3빼곤 못이긴다
                    return -1;
            }

            if (handstate[0] == 4 && IsCardEight(handstate[1])) // 혁명 + 8스킵
                return 9;
            if (IsJokerReflect(handstate[1], fieldstate[1])) // 3 받아치기
                return 3;
            if (handstate[0] == 4) // 혁명
                return 2;
            if (IsCardEight(handstate[1])) // 8스킵
                return 8;
            return 1;
        }

        return -1;
    }
    public int SubmmitCard(int pid, string message = "", bool isbot = false) // 카드를 필드에 낸다
    {
        
        if (PID == pid) // 나 자신
        {
            int count = FieldCard.Count;
            for (int i=0; i<ClickDeck.Count; i++) // 선택한 카드를 모두 낸다
            {
                int index = IsHand(pid, ClickDeck[i]);
                FieldCard.Add(HandCard[index]);
                HandCard.RemoveAt(index);
                PlayerDeck[pid].RemoveAt(index);
                
                FieldCard[count + i].GetComponent<CardControler>().MoveCard(new Vector3(0.8f * i - 0.4f * StateToCard(handstate[0]) + 0.4f, 0, CARD_MAX - count - i));
                
            }
            fieldstate[0] = handstate[0];
            fieldstate[1] = handstate[1];
            FieldDeck.Clear();
            FieldDeck = ClickDeck.ToList();
            ClickDeck.Clear();
            return StateToCard(fieldstate[0]);
        } 
        else if(isbot)
        {
            int count = FieldCard.Count;
            for (int i = 0; i < BotDeck.Count; i++) // 봇이 선택한 카드를 모두 낸다
            {
                int index = IsHand(pid, BotDeck[i]);
                FieldCard.Add(CreateCard(PlayerDeck[pid][index], CARD_MAX - count - i, new Vector2(0.8f * i - 0.4f * StateToCard(botstate[0]) + 0.4f, 0)));
                PlayerDeck[pid].RemoveAt(index);

            }
            fieldstate[0] = botstate[0];
            fieldstate[1] = botstate[1];
            FieldDeck.Clear();
            FieldDeck = BotDeck.ToList();
            BotDeck.Clear();
            return StateToCard(fieldstate[0]);
        }
        else // 다른 유저
        {
            BotDeck = this.StringToList(message);
            botstate[0] = BotDeck[BotDeck.Count - 2];
            botstate[1] = BotDeck[BotDeck.Count - 1];
            BotDeck.RemoveAt(BotDeck.Count - 1);
            BotDeck.RemoveAt(BotDeck.Count - 1);

            int count = FieldCard.Count;
            for (int i = 0; i < BotDeck.Count; i++) // 선택한 카드를 모두 낸다
            {
                int index = IsHand(pid, BotDeck[i]);
                FieldCard.Add(CreateCard(PlayerDeck[pid][index], CARD_MAX - count - i, new Vector2(0.8f * i - 0.4f * StateToCard(botstate[0]) + 0.4f, 0)));
                PlayerDeck[pid].RemoveAt(index);

            }
            fieldstate[0] = botstate[0];
            fieldstate[1] = botstate[1];
            FieldDeck.Clear();
            FieldDeck = BotDeck.ToList();
            BotDeck.Clear();
            return StateToCard(fieldstate[0]);
        }
    }
    public void Dominate() // 지배권
    {
        for (int i = 0; i < FieldCard.Count; i++) // 필드를 비운다
            Destroy(FieldCard[i]);
        FieldCard.Clear();
        FieldDeck.Clear();
        fieldstate[0] = 0;
        fieldstate[1] = 0;
    }
    public void Revolution() // 혁명
    {
        revolution = !revolution;
    }
    public void RoundPrepare() // 라운드 시작 전 초기화
    {
        for (int i = 0; i < 2; i++)
        {
            fieldstate[i] = 0;
            handstate[i] = 0;
            botstate[i] = 0;
        }
        for (int i = 0; i < 6; i++)
        {
            taxtemp[i] = -1;
        }
        revolution = false;
        Deck.Clear();
        for(int i = 0;i<USER_MAX; i++)
        {
            PlayerDeck[i].Clear();
        }
        
        ClickDeck.Clear();
        FieldDeck.Clear();
        BotDeck.Clear();
        for (int i = 0; i < HandCard.Count; i++)
            Destroy(HandCard[i]);
        HandCard.Clear();
        for (int i = 0; i < FieldCard.Count; i++)
            Destroy(FieldCard[i]);
        FieldCard.Clear();

        shuffle(); // 카드 셔플  
    }
    public void RoundSet(string message) // 카드 분배 후 시각화
    {
        Deck = StringToList(message);
        DistributeCard(); // 카드 분배
        DrawMyCard(); // 내 카드 시각화
    }
    public string GetToString(int index)
    {
        if (index == 1)
            return ListToString(Deck);
        else if (index == 2)
        {
            ClickDeck.Add(handstate[0]);
            ClickDeck.Add(handstate[1]);
            string str = ListToString(ClickDeck.ToList());
            ClickDeck.RemoveAt(ClickDeck.Count - 1);
            ClickDeck.RemoveAt(ClickDeck.Count - 1);
            return str;
        }
        else if(index == 3)
        {
            string str = ListToString(ClickDeck.ToList());
            return str;
        }
        else
            return null;
    } // 1 = Deck, 2 = ClickDeck + handstate, 3 = ClickDeck
    void DrawMyCard() // 내 패 prefab draw
    {
        for (int i = 0; i < CARD_MAX / USER_MAX; i++)
        {
            HandCard.Add(this.CreateCard(PlayerDeck[PID][i], 13 - i, new Vector2(i * 0.8f - 4.8f, -3.5f)));
        }
    } 
    void InitDeck() // 변수 할당
    {
        HandCard = new List<GameObject>();
        FieldCard = new List<GameObject>();
        Deck = new List<int>();
        FieldDeck = new List<int>();
        ClickDeck = new List<int>();
        BotDeck = new List<int>();
        PlayerDeck = new List<int>[USER_MAX];
        fieldstate = new int[2];
        handstate = new int[2];
        botstate = new int[2];
        taxtemp = new int[6];
        for (int i = 0; i < USER_MAX; i++)
        {
            PlayerDeck[i] = new List<int>();
        }

    }
    void shuffle() // 덱 셔플
    {
        for (int i = 0; i < CARD_MAX; i++) // 카드 추가
            Deck.Add(i);

        for (int i = 0; i < Deck.Count; i++) // 셔플
        {
            int rand = UnityEngine.Random.Range(i, Deck.Count);
            int temp = Deck[i];
            Deck[i] = Deck[rand];
            Deck[rand] = temp;
        }
    }
    void DistributeCard() // 카드를 4명의 플레이어에게 분배
    {
        for (int i = 0; i < CARD_MAX / USER_MAX; i++) //13장씩분배
        {
            for (int j = 0; j < USER_MAX; j++)
            {
                PlayerDeck[j].Add(Deck[USER_MAX*i + j]);
            }
        }
        Deck.Sort();
        for(int i=0; i<USER_MAX; i++)
            PlayerDeck[i].Sort();
    }
    GameObject CreateCard(int number, int order, Vector2 position) // 카드 생성
    {
        GameObject prefab = Instantiate(CardPrefab) as GameObject;
        prefab.GetComponent<CardControler>().SetCard(number, order, CardImage[number]);
        prefab.transform.position = new Vector3(position.x, position.y, order);
        return prefab;
    }
    int IsHand(int pid, int number) // 손에 있는 패의 숫자일 경우 index, 없을 경우 -1 반환
    {
        for (int index = 0; index < PlayerDeck[pid].Count; index++)
        {
            if (number == PlayerDeck[pid][index])
            {
                return index;
            }
        }
        
          
        return -1;
    }
    bool IsCardClicked(int number) // 클릭 됬던 카드 일 경우 true
    {
        for(int i = 0; i < ClickDeck.Count; i++)
        {
            if (number == ClickDeck[i])
                return true;
        }

        return false;
    }
    bool IsJoker(int number) // joker일 경우 true
    {
        if (number == 52 || number == 53)
            return true;
        return false;
    }
    bool IsJokerReflect(int hand, int field) // 조커 받아치기일 경우 true
    {
        return (hand == 3) && IsJoker(field);
    }
    bool IsSameNumber() // 클릭한 카드숫자가 다 같은면 true
    {
        int temp = 0;
        int length = ClickDeck.Count;
        if (length == 1)
            return true;
        temp = ClickDeck[0] / 4;
        for(int i=1; i<length; i++)
        {
            if (temp != (ClickDeck[i] / 4) && !IsJoker(ClickDeck[i]))
                return false;
        }
        return true;
    }
    bool IsCardEight(int number) // 카드가 8이면 반환
    {
        if (number / 4 == 5)
            return true;
        return false;
    }
    int StateToCard(int number) // 1234는 동일 56만 34로 바뀜
    {
        if (number >= 0 && number < 5)
            return number;
        return number - 2;
    }
    void HandStateCheck(bool choice)
    {
        int temp = 0;
        if (choice)
        {
            if(revolution)
            {
                handstate[1] = ClickDeck[0];
            }
            else
            {
                if (IsJoker(ClickDeck[0]))
                    handstate[1] = ClickDeck[0];
                else
                {
                    
                    for (int i = 0; i < handstate[0]; i++)
                    {
                        if (IsJoker(ClickDeck[i]))
                            temp++;
                    }
                    handstate[1] = ClickDeck[handstate[0] - temp - 1];
                }
            }
        }
        else
        {
            if(revolution)
            {
                handstate[1] = ClickDeck[0];
            }
            else
            {
                
                for (int i = 0; i < handstate[0]; i++)
                {
                    if (IsJoker(ClickDeck[i]))
                        temp++;
                }
                if (IsJoker(ClickDeck[0]))
                    handstate[1] = ClickDeck[0];
                else
                    handstate[1] = ClickDeck[handstate[0] - temp - 1];
            }
        }

    }
    int CheckBotCard(int other, int joker) // 낼 수있는 index를 반환
    {
        int index = -1;
        int fieldCard = fieldstate[1];
        int[] cardcount = new int[14];

        for (int i = 0; i < cardcount.Length; i++)
            cardcount[i] = 0;

        for (int i = 0; i < PlayerDeck[other].Count; i++) // 카드 카운팅
        {
            cardcount[PlayerDeck[other][i] / 4]++;
        }
        if (revolution)
        {
            if (fieldstate[0] == 0)
                fieldCard = 55;
            if (IsJoker(fieldCard))
                fieldCard = -1;
            for (int i = PlayerDeck[other].Count - 1; i > -1 ; i--)
            {
                if (!IsJoker(PlayerDeck[other][i]) && PlayerDeck[other][i] < fieldCard && (cardcount[PlayerDeck[other][i] / 4] + joker) >= fieldstate[0])
                {
                    index = PlayerDeck[other][i] / 4;
                    break;
                }
            }
        }
        else
        {
            for(int i=0; i < PlayerDeck[other].Count; i++)
            {
                if (!IsJoker(PlayerDeck[other][i]) && PlayerDeck[other][i] > fieldCard && (cardcount[PlayerDeck[other][i] / 4] + joker) >= fieldstate[0])
                {
                    index = PlayerDeck[other][i] / 4;
                    break;
                }
            }
        }

        if (index == -1 && joker == 1 && fieldstate[0] == 1) // 조커만으로 조합
            index = 13;
        else if (index == -1 && joker == 2 && fieldstate[0] == 2)
            index = 13;

        return index;
    }
    string ListToString(List<int> list) // List<int> -> string
    {
        List<string> str = list.ConvertAll<string>(x => x.ToString());
        return String.Join(",", str);
    }
    List<int> StringToList(string str) // string -> List<int>
    {
        List<string> list = new List<string>(str.Split(","));
        return list.ConvertAll(int.Parse);
    }
}
