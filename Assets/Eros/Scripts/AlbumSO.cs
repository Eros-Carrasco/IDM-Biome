#if UNITY_EDITOR
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "AlbumSO", menuName = "Scriptable Objects/AlbumSO")]
public class AlbumSO : ScriptableObject
{
    public string albumName;

    public AlbumType albumType;
    public Texture[] imageList;
    public VideoClip[] videoClips;  

    public AudioClip[] musicClips;
}
#endif