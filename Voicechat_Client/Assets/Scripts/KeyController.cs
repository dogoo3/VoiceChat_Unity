using UnityEngine;
using UnityEngine.InputSystem;

public class KeyController : MonoBehaviour
{
    void OnSpace()
    {
        Debug.Log("sdajklfjsda");

        ArrayTestPacket<int[]> t_arrayTestPacket = new ArrayTestPacket<int[]>();
        t_arrayTestPacket.common.type = "arraytest";
        t_arrayTestPacket.common.content = new int[882];

        for(int i=0;i<t_arrayTestPacket.common.content.Length;i++)
        {
            t_arrayTestPacket.common.content[i] = (i+1) * 3;
        }
        NetworkClient.instance.SendJson(ref t_arrayTestPacket);
    }
}
