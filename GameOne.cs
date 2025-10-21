using UnityEngine;
using UnityEngine.UIElements;

public class GameOne : MonoBehaviour
{
    private bool isCtrl = false;
    private bool isShift = false;
    private KeyCode[] keyMap = { KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K };

    TextField note;

    void Start()
    {
        GameObject go = GameObject.Find("UIDocument");
        if (go != null)
        {
            Default defaultScript = go.GetComponent<Default>();
            if (defaultScript != null)
            {
                // 现在可以访问 public 字段
                note = defaultScript.noteInput;
            }
        }
    }

    void Update()
    {
        //按键音
        isCtrl = Input.GetKey(KeyCode.LeftControl);
        isShift = Input.GetKey(KeyCode.LeftShift);
        //按下按键的时候禁用输入框
        note.SetEnabled(!(isCtrl || isShift));

        for (int i = 0; i < keyMap.Length; i++)
        {
            if (Input.GetKeyDown(keyMap[i]))
            {
                int col = i; // 列索引
                string notePrefix = "中音";

                if (isCtrl)
                {
                    notePrefix = "低音";
                }
                else if (isShift)
                {
                    notePrefix = "高音";
                }
                else
                {
                    notePrefix = "中音";
                }
                string fileName = $"{notePrefix}_{col + 1}";
                AudioClip clip = Resources.Load<AudioClip>($"piano_notes/{fileName}");
                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, UnityEngine.Vector3.zero);
                }
            }
        }
    }
}
