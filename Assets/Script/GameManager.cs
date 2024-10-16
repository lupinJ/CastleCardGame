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
    private static GameManager play = null; // 싱글톤
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

    [SerializeField] PlayerRPC RPC; // network 객채
    [SerializeField] UserInfoControler[] User; // user UI
    [SerializeField] CardManager Card; // Card UI
    [SerializeField] Button[] buttons; // pass submit 버튼
    [SerializeField] Sprite[] ClassImage; // 클래스 이미지
    [SerializeField] TextMeshPro MainText; // 메인 text
    [SerializeField] AudioSource[] Audio; // 오디오

    const int CARD_MAX = 54; // 상수
    const int USER_MAX = 4; // 총 유저수

    string[] PlayerName; // 플레이어 이름
    int[] Playerstate; // 0 = 봇 1 = 유저 2 = 비활성화
    int[] Playerclass; // 계급 0 = 대빈민, 1 = 빈민, 2 = 부호, 3 = 대부호
    int[] PlayerHand; // 가지고 있는 카드 수
    int[] PlayerCoin; // 플레이어 현재 코인 수

    bool single = false; // single 모드 = 0, multi mode = 1
    int PID = 0; // 플레이어 고유 ID (기본값 0, host 3)
    
    public int turn; // 현재 턴

    int winer = 0; // 패를 다 내려놓은 사람 수
    int comedown = 0; // 몰락한 사람 수
    int round = 0; // 라운드
    
    int pass; // 패스한 횟수
    
    /* -1 = 불가능
     *  0 = pass 
     *  1 = submit 
     *  2 = 혁명 
     *  3 = 3받아치기 
     *  4 = 오토 플레이
     *  8 = 8스킵 
     *  9 = 8스킵 + 혁명  
     *  10= round 시작 값
    */

    void Awake() // 싱글톤 처리
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
    private void Start() // 초기화
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

    /* ********* public 함수들 *********** */
    public bool IsPlayerTurn(int pid) // 네트워크에서 turn 확인
    {
        if (pid == turn)
            return true;
        return false;
    }
    public void SetMainText(string text) // main text 변경
    {
        MainText.text = text;
    }
    public void GameSet(int pid, bool single) // pid 설정
    {
        PID = pid;
        this.single = single;
        SetUserName();
        Card.SetDeck(PID);

    }
    public IEnumerator RoundPrepare() // 라운드 준비
    {
        winer = 0; // 초기화
        comedown = 0;
        pass = 0;
        for (int i = 0; i < USER_MAX; i++) // userUI 갱신
        {
            Playerstate[i] = single ? 0 : 1;
            PlayerHand[i] = CARD_MAX / USER_MAX;
            User[PidToUserIndex(PID, i)].InitInfo(PlayerName[i], PlayerHand[i], PlayerCoin[i]);
        }
        Playerstate[0] = 1;

        MainText.text = string.Format("{0}일차 시작!", round + 1);
        Card.RoundPrepare(); // 카드 준비
        yield return new WaitForSeconds(0.5f);

        RPC.RPC_SendDeck(Card.GetToString(1));
        yield break;
    }
    public IEnumerator RoundStart(string message) // 라운드 시작
    {
        Card.RoundSet(message);
        yield return new WaitForSeconds(0.5f);
        if (round != 0)
        {
            turn = -2;
            StartCoroutine(TexPayment()); // 납세
        }
        else
        {
            GameStart();
        }
        yield break;
    }
    public IEnumerator NextTurn(int choice) // 턴 종료 후처리
    {
        yield return new WaitForSeconds(0.5f);
        
        if(turn >= 0) // 턴 잡은 유저 이름 Lighting
            User[PidToUserIndex(PID, turn)].SetColor(0);

        // 승리 처리
        if (Playerstate[turn] != 2 && PlayerHand[turn] == 0) 
        {
            ClassChange(turn, 3 - winer); // 클레스 변경
            MainText.text = string.Format("계급 변경!");
            yield return new WaitForSeconds(2.0f);

            // 몰락
            for (int i=0; i < USER_MAX; i++) 
            {
                if (winer == 1 && i != turn && Playerclass[i] == 3)
                {
                    PlayerComeDown(i);
                    MainText.text = string.Format("몰락!");
                    yield return new WaitForSeconds(2.0f);
                }
            }
             
        }
       
        // 라운드 종료
        if ((winer + comedown) == 3)
        {
            MainText.text = string.Format("라운드 종료!");
            yield return new WaitForSeconds(2.0f);
            
            // 남은 플레이어 클래스 지정
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

            // 코인 분배
            CoinDistribute();

            // 다음 라운드 시작
            if (round == 2)
            {
                MainText.text = string.Format("게임 종료!");
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
                MainText.text = string.Format("승자는 {0}!", PlayerName[winer]);
                yield break;
            }
            else
            {
                round++;
                StartCoroutine(RoundPrepare());
            }    
            yield break;
        }
        
        // 특수 룰
        switch (choice) 
        {
            case 0: // pass
                if(pass == USER_MAX - 1 - winer - comedown)
                    Card.Dominate();
                break;
            case 2: // 혁명
                Card.Revolution();
                break;
            case 3: // 3 받아치기
                Card.Dominate();
                turn--;
                break;
            case 8: // 8스킵
                Card.Dominate();
                turn--;
                break;
            case 9: // 혁명 + 8스킵
                Card.Revolution();
                Card.Dominate();
                turn--;
                break;
            case 10: // 첫 시작
                turn--;
                break;
            default:
                break;
        }


        // 턴 증가
        if (turn < USER_MAX - 1)
        {
            turn++;
        } 
        else
        {
            turn = 0;
        }
        
        // 끝난 유저를 넘긴다
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

        if (turn == PID) // 내 차례
            PlayerPlay();
        else if (Playerstate[turn] == 0) // 봇
        {
            StartCoroutine(this.Action(turn, 4));
        }
        else
        { } // 유저
        
        yield break;
    }
    public IEnumerator Action(int user, int choice, string message = "") // 선택에 따른 행동 처리 
    {
        bool isbot = false;
        int first = -1;

        if(choice == 4) // bot일 경우
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
        switch (first) // 기본 룰
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

        switch(choice) // 특수 룰
        {
            case 2: // 혁명
                MainText.text = string.Format("혁명!");
                break;
            case 3: // 3 받아치기
                MainText.text = string.Format("스페이드 3 받아치기!");
                break;
            case 8:
                MainText.text = string.Format("8 스킵!");
                break;
            case 9:
                MainText.text = string.Format("혁명");
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

        if (turn == -2) // 납세
        {
            if (!Card.TaxCheck(PID, Playerclass[PID]))
                return;
            GameButton(false);
            RPC.RPC_SendTex(Card.GetToString(3));
          
        }
        else // 일반적인 submit
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
    public void CardClicked(int number) // 카드 클릭 이벤트
    {
        Card.ClickCard(number);
    }
    public void ReciveTex(int pid, string message) // network로 전달 받은 세금을 저장
    {
        Card.TaxSubmit(pid, Playerclass[pid], message);

        pass++;
        if (PID == pid)
            comedown = 1;
        if (comedown == 1)
        {
            if(pass == USER_MAX)
                MainText.text = string.Format("라운드 시작!", pass);
            else
                MainText.text = string.Format("납세 인원 : {0}명", pass);
        }

        if (pass == USER_MAX)
        {
            pass = 0;
            comedown = 0;
            TaxDistribute(); // 세금 분배
            GameStart();
        }
    }
    public void OnBackButtonClicked()
    {
        RPC.DisconnectRunner();
    }
    /********** protected 함수들 ***********/
    IEnumerator TexPayment() // 계급에 따른 납세
    {
        switch (Playerclass[PID])
        {
            case 0:
                MainText.text = string.Format("왕에게 줄 가장 높은 2장을 고르세요");
                break;
            case 1:
                MainText.text = string.Format("귀족에게 줄 가장 높은 1장을 고르세요");
                break;
            case 2:
                MainText.text = string.Format("평민에게 줄 아무 카드 1장을 고르세요");
                break;
            case 3:
                MainText.text = string.Format("광대에게 줄 아무 카드 2장을 고르세요");
                break;
        }

        // 봇 세금 납부
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
    void TaxDistribute() // 세금 분배
    {
        for (int i = 0; i < USER_MAX; i++)
        {
            Card.TaxSwap(i, Playerclass[i]);
        }
    }
    void GameStart() // 게임 시작
    {
        turn = WhoFirst(); // 선턴 확인
        StartCoroutine(NextTurn(10)); // 시작
    }
    void PlayerPlay() // 플레이어 턴 
    {
        GameButton(true);
    }
    void GameButton(bool button) // 버튼 활성화
    {
        buttons[0].interactable = button;
        buttons[1].interactable = button;
    }
    void ClassChange(int id, int rank) // 클레스 변경
    {
        Playerstate[id] = 2;
        Playerclass[id] = rank; 
        winer++;
        User[PidToUserIndex(PID,id)].SetClass(ClassImage[rank]);
        return;
    }
    void PlayerComeDown(int id) // 몰락
    {
        Playerstate[id] = 2;
        Playerclass[id] = 0;
        comedown += 1;
        User[PidToUserIndex(PID, id)].SetClass(ClassImage[0]);
        return;
    }
    void CoinDistribute() // 코인 분배
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
    int WhoFirst() // 선턴 확인
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
    int PidToUserIndex(int mypid, int userpid) // 유저의 pid를 ui index로 변경
    {
        int result = userpid - mypid;
        if (result < 0)
            return result + USER_MAX;
        else
            return result;
    }
    void SetUserName() // pid에 따른 이름 설정
    {
        if (PID < 0 || PID > 3)
            return;

        for(int pid = 0; pid < USER_MAX; pid++)
        {
            PlayerName[pid] = string.Format("Player{0}", pid);
        }
    }
    
}
