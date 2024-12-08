using System;

namespace ruinvault;

public class GameLib {
    public static void MessageBox(string title, string content) {
            MessageBox(title, content, "Continue", () => {}, null, () =>{});
    }
    public static void MessageBox(string title, string content, string acceptText, Action onAccept, string? cancelText, Action onCancel) {
        // https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextSupportedTags.html
        var p = new DialogBoxParams(title, content, acceptText, onAccept, cancelText, onCancel)
        {
            delayBeforeShowingOptions = 0f,
            delayBeforeShowingOptionsPerContentCharacter = 0f,
        };
        var dc = DialogBoxController.Instance;
        dc.settings.fadeInTime = 0f;
        dc.settings.fadeOutTime = 0f;
        dc.Show(p);
    }
    public static void Blackout(bool visible) {
        if (visible) {
            global::Blackout.Instance.ShowImmediate();
        } else {
            global::Blackout.Instance.HideImmediate();
        }
    }
}