using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    private static UIManager ui = null; // 싱글톤
    public static UIManager UI
    {
        get
        {
            if (null == ui)
            {
                return null;
            }
            return ui;
        }
    }

    [SerializeField] GameObject NetworkManager; // Network 객체
    
    [SerializeField] GameObject[] Texts; // UI들
    [SerializeField] GameObject[] buttons;
    [SerializeField] GameObject inputs;
    [SerializeField] GameObject[] Sliders;
    [SerializeField] AudioSource Audio;
    int state = 0; // 현재 UI 상태
    bool connection = false; // 네트워크 연결 상태
    /*
     * 0 = 메인
     * 1 = 멀티
     * 2 = 설정
    */
    void Awake()
    {
        if (null == ui)
        {
            ui = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    private void Start()
    {
        state = 0;
        if (PlayerPrefs.HasKey("bgm"))
        {
            Audio.volume = PlayerPrefs.GetFloat("bgm");
            Sliders[0].GetComponent<Slider>().value = PlayerPrefs.GetFloat("bgm");
        }
        if(PlayerPrefs.HasKey("sfx"))
        {
            Sliders[1].GetComponent<Slider>().value = PlayerPrefs.GetFloat("sfx");
        }
    }

    /* callback func*/
    public void OnSingleClick()
    {
        NetworkManager.GetComponent<BasicSpawner>().StartGame(Fusion.GameMode.Single, "single");
    }
    public void OnMultiClick()
    {
        buttons[0].SetActive(false);
        buttons[1].SetActive(false);
        buttons[2].SetActive(false);
        buttons[3].SetActive(true);
        buttons[4].SetActive(true);
        buttons[5].SetActive(true);
        Texts[0].SetActive(true);
        inputs.SetActive(true);
        
        state = 1;
    }
    public void OnSettingClick()
    {
        buttons[0].SetActive(false);
        buttons[1].SetActive(false);
        buttons[2].SetActive(false);
        buttons[5].SetActive(true);
        Texts[1].SetActive(true);
        Texts[2].SetActive(true);
        Sliders[0].SetActive(true);
        Sliders[1].SetActive(true);
        state = 2;
    }
    public void OnBackClick()
    {
        switch(state)
        {
            case 1:
                buttons[0].SetActive(true);
                buttons[1].SetActive(true);
                buttons[2].SetActive(true);
                buttons[3].SetActive(false);
                buttons[4].SetActive(false);
                buttons[5].SetActive(false);
                Texts[0].SetActive(false);
                inputs.SetActive(false);
                break;
            case 2:
                buttons[0].SetActive(true);
                buttons[1].SetActive(true);
                buttons[2].SetActive(true);
                buttons[5].SetActive(false);
                Texts[1].SetActive(false);
                Texts[2].SetActive(false);
                Sliders[0].SetActive(false);
                Sliders[1].SetActive(false);
                break;
            default:
                break;
        }
    }
    public void OnBgmSliderChanged()
    {
        float temp = Sliders[0].GetComponent<Slider>().value;
        Audio.volume = temp;
        PlayerPrefs.SetFloat("bgm", temp);

    }
    public void OnSfxSliderChanged()
    {
        float temp = Sliders[1].GetComponent<Slider>().value;
        PlayerPrefs.SetFloat("sfx", temp);
    }
    public void RoomCreate()
    {
        SetNetText("Loading...");
        NetButtonActive(false);
        NetworkManager.GetComponent<BasicSpawner>().StartGame(Fusion.GameMode.Host, inputs.GetComponent<TMP_InputField>().text);
    }
    public void RoomAttend()
    {
        if(!connection)
        {
            SetNetText("Find Room...");
            buttons[3].GetComponent<Button>().interactable = false;
            connection = true;
            NetworkManager.GetComponent<BasicSpawner>().StartGame(Fusion.GameMode.Client, inputs.GetComponent<TMP_InputField>().text);
        }
        else
        {
            SetNetText("");
            buttons[3].GetComponent<Button>().interactable = true;
            connection = false;
            NetworkManager.GetComponent<BasicSpawner>().DisconnectRunner();
        }
    }
    public void SetNetText(string text)
    {
        Texts[0].GetComponent<TextMeshPro>().text = text;
    }
    public void NetButtonActive(bool swtich)
    {
        buttons[3].GetComponent<Button>().interactable = swtich;
        buttons[4].GetComponent<Button>().interactable = swtich;
    }
}
