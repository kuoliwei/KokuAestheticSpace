using UnityEngine;
using UnityEngine.UI;

public class DebugButtonInspector : MonoBehaviour
{
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn == null) return;

        int count = btn.onClick.GetPersistentEventCount();
        Debug.Log($"[DebugButtonInspector] PersistentEventCount = {count}");
        for (int i = 0; i < count; i++)
        {
            var target = btn.onClick.GetPersistentTarget(i);
            var method = btn.onClick.GetPersistentMethodName(i);
            Debug.Log($"Event {i}: {target} ¡÷ {method}");
        }
    }
}
