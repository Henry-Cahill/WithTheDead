using UnityEngine;
using UnityEngine.UI;
using CloneZ;

public class BloodHUD : MonoBehaviour
{
    private Text _typeLabel;
    private PlayerBlood _blood;

    void Awake()
    {
        // create a small UI panel in the top‑left corner
        var canvas = UIKit.CreateCanvas("BloodHUD", 300, transform);
        var panel = UIKit.Panel(canvas.transform, "Panel", UIKit.PanelColor);
        UIKit.Stretch(panel.rectTransform, 5f, 5f, -5f, -5f);
        _typeLabel = UIKit.Label(panel.rectTransform, "Blood type: Unknown", 18, UIKit.TextColor);
        UIKit.Stretch(_typeLabel.rectTransform, 4f, 4f, -4f, -4f);
    }

    void Start()
    {
        _blood = FindObjectOfType<PlayerBlood>();
    }

    void Update()
    {
        if (_blood == null) return;
        string display = _blood.TypeKnown ? _blood.BloodType : "Unknown";
        _typeLabel.text = $"Blood type: {display}";
    }

    // automatically add the HUD to the player when the scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachToPlayer()
    {
        var pb = Object.FindObjectOfType<PlayerBlood>();
        if (pb != null && pb.gameObject.GetComponent<BloodHUD>() == null)
        {
            pb.gameObject.AddComponent<BloodHUD>();
        }
    }
}
