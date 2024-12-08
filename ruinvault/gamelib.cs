using System;

namespace ruinvault;

public class GameLib {
    public static void MessageBox(string title, string content) {
            MessageBox(title, content, "", () => {}, "", () =>{});
    }
    public static void MessageBox(string title, string content, string acceptText, Action onAccept, string cancelText, Action onCancel) {
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
}