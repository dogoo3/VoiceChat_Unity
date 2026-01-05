using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginServer : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInputfield;
    [SerializeField] private TMP_Text errormessage; // 에러 메시지를 띄우는 TMP text
    public void ClickAccessButton() // 버튼을 클릭했을 때 서버에 로그인을 요청함
    {
        if(nicknameInputfield.text.Length < 4) {
            errormessage.text = "이름은 4글자 이상으로 입력해 주세요.";
        }
        else
        {
            NetworkClient.instance.ConnectToServer(nicknameInputfield.text);
        }
    }
}
