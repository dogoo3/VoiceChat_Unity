using UnityEngine;
using UnityEngine.InputSystem;

public class KeyController : MonoBehaviour
{
    void OnSpace()
    {
        Debug.Log("sdajklfjsda");

        ArrayTestPacket<float[]> t_arrayTestPacket = new ArrayTestPacket<float[]>();
        t_arrayTestPacket.common.type = "arraytest";
        t_arrayTestPacket.common.content = new float[1764];

        for(int i=0;i<t_arrayTestPacket.common.content.Length;i++)
        {
            t_arrayTestPacket.common.content[i] = (i+1) * 3.1f;
        }
        NetworkClient.instance.SendJson(ref t_arrayTestPacket);
    }
}
