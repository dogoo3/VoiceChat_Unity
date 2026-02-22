using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    public GameObject[] userObjs;
    private TMP_Text[] users_tmp; // 0번은 나 자신, 1~4번은 다른 참여자.
    
    private int entry_count = 1; // 접속 유저 수. 방이 만들어지면 최소 1명이 있기 때문에 1부터 시작함

    private void Awake() 
    {
        users_tmp = new TMP_Text[5];
        for(int i=0;i<users_tmp.Length;i++)
        {
            users_tmp[i] = userObjs[i].GetComponentInChildren<TMP_Text>();
        }
        gameObject.SetActive(false);
    }

    // 닉네임 입력 후 처음 방에 들어올 때 유저 이름을 설정해 줌
    public void InitializeUserName(string p_mynickname, string[] p_userNames)
    {
        for(int i=0;i<p_userNames.Length;i++)
        {
            if(p_userNames[i] == p_mynickname) // 받은 닉네임들 중 내 닉네임과 같으면
            {
                users_tmp[0].text = p_mynickname;
                continue;
            }
            users_tmp[entry_count].text = p_userNames[i];
            if(entry_count < 5)
                entry_count++;
            else
            {
                Debug.LogError("entry_count가 초과하였습니다.");
                break;
            }
        }
    }

    public void AddUser(string p_newUsername)
    {
        Debug.Log(p_newUsername);
        users_tmp[entry_count].text = p_newUsername;
        if(entry_count < 5)
            entry_count++;
        else
        {
            Debug.LogError("entry_count가 초과하였습니다.");
        }
    }
}
