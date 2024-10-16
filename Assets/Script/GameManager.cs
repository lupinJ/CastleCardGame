using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager play = null; // �̱���
    public static GameManager Play
    {
        get
        {
            if (null == play)
            {
                return null;
            }
            return play;
        }
    }

    [SerializeField] PlayerRPC RPC; // network ��ä
    [SerializeField] UserInfoControler[] User; // user UI
    [SerializeField] CardManager Card; // Card UI
    [SerializeField] Button[] buttons; // pass submit ��ư
    [SerializeField] Sprite[] ClassImage; // Ŭ���� �̹���
    [SerializeField] TextMeshPro MainText; // ���� text
    [SerializeField] AudioSource[] Audio; // �����

    const int CARD_MAX = 54; // ���
    const int USER_MAX = 4; // �� ������

    string[] PlayerName; // �÷��̾� �̸�
    int[] Playerstate; // 0 = �� 1 = ���� 2 = ��Ȱ��ȭ
    int[] Playerclass; // ��� 0 = ����, 1 = ���, 2 = ��ȣ, 3 = ���ȣ
    int[] PlayerHand; // ������ �ִ� ī�� ��
    int[] PlayerCoin; // �÷��̾� ���� ���� ��

    bool single = false; // single ��� = 0, multi mode = 1
    int PID = 0; // �÷��̾� ���� ID (�⺻�� 0, host 3)
    
    public int turn; // ���� ��

    int winer = 0; // �и� �� �������� ��� ��
    int comedown = 0; // ������ ��� ��
    int round = 0; // ����
    
    int pass; // �н��� Ƚ��
    
    /* -1 = �Ұ���
     *  0 = pass 
     *  1 = submit 
     *  2 = ���� 
     *  3 = 3�޾�ġ�� 
     *  4 = ���� �÷���
     *  8 = 8��ŵ 
     *  9 = 8��ŵ + ����  
     *  10= round ���� ��
    */

    void Awake() // �̱��� ó��
    {
        if (null == play)
        {
            play = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    private void Start() // �ʱ�ȭ
    {
        
        PlayerName = new string[USER_MAX];
        Playerclass = new int[USER_MAX];
        PlayerHand = new int[USER_MAX];
        PlayerCoin = new int[USER_MAX];
        Playerstate = new int[USER_MAX];
        PID = 0;
        round = 0;
        turn = 0;

        for (int i = 0; i < USER_MAX; i++) // UserUI
        {
            Playerstate[i] = 1;
            PlayerName[i] = string.Format("");
            Playerclass[i] = 2;
            PlayerHand[i] = CARD_MAX / USER_MAX;
            PlayerCoin[i] = 0;
            User[i].InitInfo(PlayerName[i], PlayerHand[i], PlayerCoin[i]);
        }
        if (PlayerPrefs.HasKey("bgm"))
        {
            Audio[0].volume = PlayerPrefs.GetFloat("bgm");
            
        }
        if (PlayerPrefs.HasKey("sfx"))
        {
            Audio[1].volume = PlayerPrefs.GetFloat("sfx");
        }

        GameButton(false);
        
    }

    /* ********* public �Լ��� *********** */
    public bool IsPlayerTurn(int pid) // ��Ʈ��ũ���� turn Ȯ��
    {
        if (pid == turn)
            return true;
        return false;
    }
    public void SetMainText(string text) // main text ����
    {
        MainText.text = text;
    }
    public void GameSet(int pid, bool single) // pid ����
    {
        PID = pid;
        this.single = single;
        SetUserName();
        Card.SetDeck(PID);

    }
    public IEnumerator RoundPrepare() // ���� �غ�
    {
        winer = 0; // �ʱ�ȭ
        comedown = 0;
        pass = 0;
        for (int i = 0; i < USER_MAX; i++) // userUI ����
        {
            Playerstate[i] = single ? 0 : 1;
            PlayerHand[i] = CARD_MAX / USER_MAX;
            User[PidToUserIndex(PID, i)].InitInfo(PlayerName[i], PlayerHand[i], PlayerCoin[i]);
        }
        Playerstate[0] = 1;

        MainText.text = string.Format("{0}���� ����!", round + 1);
        Card.RoundPrepare(); // ī�� �غ�
        yield return new WaitForSeconds(0.5f);

        RPC.RPC_SendDeck(Card.GetToString(1));
        yield break;
    }
    public IEnumerator RoundStart(string message) // ���� ����
    {
        Card.RoundSet(message);
        yield return new WaitForSeconds(0.5f);
        if (round != 0)
        {
            turn = -2;
            StartCoroutine(TexPayment()); // ����
        }
        else
        {
            GameStart();
        }
        yield break;
    }
    public IEnumerator NextTurn(int choice) // �� ���� ��ó��
    {
        yield return new WaitForSeconds(0.5f);
        
        if(turn >= 0) // �� ���� ���� �̸� Lighting
            User[PidToUserIndex(PID, turn)].SetColor(0);

        // �¸� ó��
        if (Playerstate[turn] != 2 && PlayerHand[turn] == 0) 
        {
            ClassChange(turn, 3 - winer); // Ŭ���� ����
            MainText.text = string.Format("��� ����!");
            yield return new WaitForSeconds(2.0f);

            // ����
            for (int i=0; i < USER_MAX; i++) 
            {
                if (winer == 1 && i != turn && Playerclass[i] == 3)
                {
                    PlayerComeDown(i);
                    MainText.text = string.Format("����!");
                    yield return new WaitForSeconds(2.0f);
                }
            }
             
        }
       
        // ���� ����
        if ((winer + comedown) == 3)
        {
            MainText.text = string.Format("���� ����!");
            yield return new WaitForSeconds(2.0f);
            
            // ���� �÷��̾� Ŭ���� ����
            for(int i = 0; i < USER_MAX; i++)
            {
                if(Playerstate[i] != 2)
                {
                    if (comedown == 0)
                        ClassChange(i, 0);
                    else
                        ClassChange(i, 1);
                }
            }

            // ���� �й�
            CoinDistribute();

            // ���� ���� ����
            if (round == 2)
            {
                MainText.text = string.Format("���� ����!");
                yield return new WaitForSeconds(2.0f);
                round = 0;
                for (int i = 0; i < USER_MAX; i++)
                {
                    if (round < PlayerCoin[i])
                    {
                        round = PlayerCoin[i];
                        pass = Playerclass[i];
                        winer = i;
                    }
                    else if(round == PlayerCoin[i] && pass < Playerclass[i])
                    {
                        round = PlayerCoin[i];
                        pass = Playerclass[i];
                        winer = i;
                    }
                }
                MainText.text = string.Format("���ڴ� {0}!", PlayerName[winer]);
                yield break;
            }
            else
            {
                round++;
                StartCoroutine(RoundPrepare());
            }    
            yield break;
        }
        
        // Ư�� ��
        switch (choice) 
        {
            case 0: // pass
                if(pass == USER_MAX - 1 - winer - comedown)
                    Card.Dominate();
                break;
            case 2: // ����
                Card.Revolution();
                break;
            case 3: // 3 �޾�ġ��
                Card.Dominate();
                turn--;
                break;
            case 8: // 8��ŵ
                Card.Dominate();
                turn--;
                break;
            case 9: // ���� + 8��ŵ
                Card.Revolution();
                Card.Dominate();
                turn--;
                break;
            case 10: // ù ����
                turn--;
                break;
            default:
                break;
        }


        // �� ����
        if (turn < USER_MAX - 1)
        {
            turn++;
        } 
        else
        {
            turn = 0;
        }
        
        // ���� ������ �ѱ��
        while (true) 
        {
            if (Playerstate[turn] == 2)
            {
                if (turn < USER_MAX - 1)
                {
                    turn++;
                }
                else
                {
                    turn = 0;
                }
            }
            else
                break;
        } 
        
        User[PidToUserIndex(PID, turn)].SetColor(1);
        yield return new WaitForSeconds(0.5f);

        if (turn == PID) // �� ����
            PlayerPlay();
        else if (Playerstate[turn] == 0) // ��
        {
            StartCoroutine(this.Action(turn, 4));
        }
        else
        { } // ����
        
        yield break;
    }
    public IEnumerator Action(int user, int choice, string message = "") // ���ÿ� ���� �ൿ ó�� 
    {
        bool isbot = false;
        int first = -1;

        if(choice == 4) // bot�� ���
        {
            isbot = true;
            choice = Card.AutoPlay(turn);
        }    

        if (choice < 1)
            first = 0;
        else
            first = 1;

        if(first == 1)
            Audio[1].Play();

        yield return new WaitForSeconds(0.2f);
        switch (first) // �⺻ ��
        {
            case 0: // pass
                if(pass < USER_MAX - 1 - winer - comedown)
                    pass++;
                MainText.text = string.Format("pass({0}/{1})", pass, USER_MAX - 1 - winer - comedown);
                break;
            case 1: // submit
                pass = 0;
                MainText.text = string.Format("");
                
                if (PID == user)
                    PlayerHand[user] -= Card.SubmmitCard(user);
                else if (isbot)
                    PlayerHand[user] -= Card.SubmmitCard(user, "", isbot);
                else
                {
                    PlayerHand[user] -= Card.SubmmitCard(user, message, isbot);
                }
                User[PidToUserIndex(PID, user)].SetCard(PlayerHand[user]);
                break;
            default:
                break;
        }

        switch(choice) // Ư�� ��
        {
            case 2: // ����
                MainText.text = string.Format("����!");
                break;
            case 3: // 3 �޾�ġ��
                MainText.text = string.Format("�����̵� 3 �޾�ġ��!");
                break;
            case 8:
                MainText.text = string.Format("8 ��ŵ!");
                break;
            case 9:
                MainText.text = string.Format("����");
                break;
            default:
                break;
        }
        StartCoroutine(NextTurn(choice));
        yield break;
    }
    public void CardSubmit() // submit event
    {
        int result;

        if (turn == -2) // ����
        {
            if (!Card.TaxCheck(PID, Playerclass[PID]))
                return;
            GameButton(false);
            RPC.RPC_SendTex(Card.GetToString(3));
          
        }
        else // �Ϲ����� submit
        {
            result = Card.Isvalid();
            if (result == -1)
                return;
            GameButton(false);
            RPC.RPC_SendHand(Card.GetToString(2), result);
        }
        
    }
    public void TurnPass() // pass event
    {
        if (turn == -2)
            return;

        GameButton(false);
        RPC.RPC_SendHand(Card.GetToString(2), 0);
    }
    public void CardClicked(int number) // ī�� Ŭ�� �̺�Ʈ
    {
        Card.ClickCard(number);
    }
    public void ReciveTex(int pid, string message) // network�� ���� ���� ������ ����
    {
        Card.TaxSubmit(pid, Playerclass[pid], message);

        pass++;
        if (PID == pid)
            comedown = 1;
        if (comedown == 1)
        {
            if(pass == USER_MAX)
                MainText.text = string.Format("���� ����!", pass);
            else
                MainText.text = string.Format("���� �ο� : {0}��", pass);
        }

        if (pass == USER_MAX)
        {
            pass = 0;
            comedown = 0;
            TaxDistribute(); // ���� �й�
            GameStart();
        }
    }
    public void OnBackButtonClicked()
    {
        RPC.DisconnectRunner();
    }
    /********** protected �Լ��� ***********/
    IEnumerator TexPayment() // ��޿� ���� ����
    {
        switch (Playerclass[PID])
        {
            case 0:
                MainText.text = string.Format("�տ��� �� ���� ���� 2���� ������");
                break;
            case 1:
                MainText.text = string.Format("�������� �� ���� ���� 1���� ������");
                break;
            case 2:
                MainText.text = string.Format("��ο��� �� �ƹ� ī�� 1���� ������");
                break;
            case 3:
                MainText.text = string.Format("���뿡�� �� �ƹ� ī�� 2���� ������");
                break;
        }

        // �� ���� ����
        for (int i = 0; i < USER_MAX; i++)
        {
            if (Playerstate[i] == 0)
            {
                Card.AutoTax(i, Playerclass[i]);
                pass++;
            }
        }

        PlayerPlay();
        yield break;
    }
    void TaxDistribute() // ���� �й�
    {
        for (int i = 0; i < USER_MAX; i++)
        {
            Card.TaxSwap(i, Playerclass[i]);
        }
    }
    void GameStart() // ���� ����
    {
        turn = WhoFirst(); // ���� Ȯ��
        StartCoroutine(NextTurn(10)); // ����
    }
    void PlayerPlay() // �÷��̾� �� 
    {
        GameButton(true);
    }
    void GameButton(bool button) // ��ư Ȱ��ȭ
    {
        buttons[0].interactable = button;
        buttons[1].interactable = button;
    }
    void ClassChange(int id, int rank) // Ŭ���� ����
    {
        Playerstate[id] = 2;
        Playerclass[id] = rank; 
        winer++;
        User[PidToUserIndex(PID,id)].SetClass(ClassImage[rank]);
        return;
    }
    void PlayerComeDown(int id) // ����
    {
        Playerstate[id] = 2;
        Playerclass[id] = 0;
        comedown += 1;
        User[PidToUserIndex(PID, id)].SetClass(ClassImage[0]);
        return;
    }
    void CoinDistribute() // ���� �й�
    {
        for(int i = 0; i < USER_MAX; i++)
        {
            switch(Playerclass[i])
            {
                case 0:
                    PlayerCoin[i] += 0;
                    break;
                case 1:
                    PlayerCoin[i] += 10;
                    break;
                case 2:
                    PlayerCoin[i] += 20;
                    break;
                case 3:
                    PlayerCoin[i] += 30;
                    break;
                default:
                    break;
            }
            User[PidToUserIndex(PID, i)].SetCoin(PlayerCoin[i]);
        }
    }
    int WhoFirst() // ���� Ȯ��
    {
        int error = -1;
        if (round == 0)
            return Card.WhoFirst();
        
        for(int i = 0; i <USER_MAX; i++)
        {
            if (Playerclass[i] == 0)
                return i;
        }

        return error;
    }
    int PidToUserIndex(int mypid, int userpid) // ������ pid�� ui index�� ����
    {
        int result = userpid - mypid;
        if (result < 0)
            return result + USER_MAX;
        else
            return result;
    }
    void SetUserName() // pid�� ���� �̸� ����
    {
        if (PID < 0 || PID > 3)
            return;

        for(int pid = 0; pid < USER_MAX; pid++)
        {
            PlayerName[pid] = string.Format("Player{0}", pid);
        }
    }
    
}
