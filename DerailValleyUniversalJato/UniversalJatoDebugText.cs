using UnityEngine;
using TMPro;

public class UniversalJatoDebugText : MonoBehaviour
{
    private TextMeshPro? _tmp;
    public string Text = "";

    void Start()
    {
        _tmp = gameObject.AddComponent<TextMeshPro>();
        _tmp.fontSize = 16;
        _tmp.alignment = TextAlignmentOptions.Center;
    }

    void LateUpdate()
    {
        if (_tmp == null)
            return;
        _tmp.text = Text;
        var cam = PlayerManager.ActiveCamera;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
