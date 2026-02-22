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

    // 방에 접속한 뒤 새로운 유저가 들어왔을 때
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

    // 내가 직접 방을 나가는 버튼을 눌렀을 때
    public void PressExitRoomButton()
    {
        MyExitPacket<string> t_exitpacket = new MyExitPacket<string>();
        t_exitpacket.common.type = "userexit";
        t_exitpacket.common.content = users_tmp[0].text; // 나 자신의 닉네임을 넣어준다(아무 쓸모 없음)

        NetworkClient.instance.SendJson(ref t_exitpacket);
    }

    // 진짜 방을 나갈 때 방 설정(닉네임 정보)을 리셋해 줌
    public void ResetRoomSetting()
    {
        foreach(TMP_Text e in users_tmp)
        {
            e.text = "";
        }
        entry_count = 1;
    }

    // 다른 사람이 방을 나갔을 때 닉네임 삭제하기
    public void ExitAnotherUser(string p_anotherUserNickname)
    {
        int t_pulluserindex = 0; // 삭제된 유저의 ID 위치를 저장하는 변수
        for(int i=1;i<users_tmp.Length;i++)
        {
            if(users_tmp[i].text == p_anotherUserNickname)
            {
                users_tmp[i].text = ""; // 유저 닉네임 삭제;
                entry_count--; // 참여 인원수 1 감소
                t_pulluserindex = i;
                break;
            }
        }
        // 닉네임 당기기 작업 진행
        for (int i = t_pulluserindex; i <= 3; i++)
        {
            users_tmp[i].text = users_tmp[i + 1].text;
        }

        // if(t_pulluserindex == 1)
        // {
        //     users_tmp[1].text = users_tmp[2].text;
        //     users_tmp[2].text = users_tmp[3].text;
        //     users_tmp[3].text = users_tmp[4].text;
        // }
        // if(t_pulluserindex == 2)
        // {
        //     users_tmp[2].text = users_tmp[3].text;
        //     users_tmp[3].text = users_tmp[4].text;
        // }
        // if(t_pulluserindex == 3)
        // {
        //     users_tmp[3].text = users_tmp[4].text;
        // }
    }
}
