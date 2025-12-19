using UnityEngine;

public class OpenPortfolioPage : MonoBehaviour
{
    [Header("URL to open")]
    [Tooltip("Full URL of your GitHub Pages sketch or any external page.")]
    public string url;

    [Header("Open in new browser tab")]
    public bool openInNewTab = true;

    public void Open()
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("⚠️ No URL assigned to this button!");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // In WebGL builds, call the JavaScript function defined in OpenURL.jslib
        if (openInNewTab)
            OpenNewTab(url);
        else
            Application.OpenURL(url);
#else
        // In Editor or desktop builds, open normally
        Application.OpenURL(url);
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // This links to our JavaScript plugin method
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void OpenNewTab(string url);
#endif
}