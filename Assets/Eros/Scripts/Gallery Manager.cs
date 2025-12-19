#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Eros/Gallery Manager")]
public class GalleryManager : MonoBehaviour
{
    public static GalleryManager Instance { get; private set; }

    #region Serialized Fields

    [SerializeField] private AlbumSO[] albums = default;

    [SerializeField] private GameObject rawImagePrefab = default; // Prefab de FOTO (RawImage)
    [SerializeField] private GameObject videoPrefab = default; // Prefab de VIDEO (RawImage + VideoPlayer dentro)
    [SerializeField] private RectTransform[] spawnPoints = default;

    [SerializeField, Min(0f)] private float spawnInitialDelay = 1.5f;
    [SerializeField, Min(0f)] private float spawnInterval = 0.05f;
    [Tooltip("Rango (grados) para rotación aleatoria en Z, ej. 0.25 = leve inclinación")]
    [SerializeField] private float spawnRotationRange = 0.25f;

    [Tooltip("Pausa entre lotes cuando todos los spawnPoints ya están ocupados y aún quedan imágenes por mostrar.")]
    [SerializeField, Min(0f)] private float cyclePause = 1.0f;

    [SerializeField] private AudioSource audioSource = default;

    #endregion

    #region Runtime State (read-only in Inspector)

    [SerializeField, HideInInspector] private AlbumSO _currentAlbum;        // backing
    [SerializeField, HideInInspector] private AlbumType _currentAlbumType;  // backing
    [SerializeField, HideInInspector] private AudioClip _currentMusicClip;  // backing

    // Índice global de imagen ya colocada más reciente (0-based). -1 antes de empezar.
    [SerializeField, HideInInspector] private int _currentImageIndex = -1;

    /// <summary>Álbum actual seleccionado en runtime.</summary>
    public AlbumSO currentAlbum => _currentAlbum;
    /// <summary>Tipo del álbum actual.</summary>
    public AlbumType currentAlbumType => _currentAlbumType;
    /// <summary>Clip de música sonando actualmente (si aplica).</summary>
    public AudioClip currentMusicClip => _currentMusicClip;

    /// <summary>Índice 1-based de la última imagen instanciada. 0 si no ha iniciado.</summary>
    public int currentImageIndex => (_currentImageIndex < 0) ? 0 : (_currentImageIndex + 1);

    #endregion

    #region Non Serialized Fields
    [SerializeField] private GameObject galleryAnimator;
    [SerializeField] private bool galleryAnimatorHasAnimated = false;
    [SerializeField] private bool FirstDelayFinished = false;


    #endregion


    #region Monobehaviours

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        galleryAnimatorHasAnimated = false;
    }

    #endregion

    #region Public API (también usados por el editor)

    /// <summary>Selecciona un álbum aleatorio y construye la galería.</summary>


    public void SetCurrentAlbum(AlbumType albumType)
    {

        AlbumSO album = null;
        foreach (var a in albums)
        {
            if (a != null && a.albumType == albumType)
            {
                album = a;
                break;
            }
        }

        if (album == null) return;

        _currentAlbum = album;
        _currentAlbumType = album.albumType;

        StopAllCoroutines();
        StartCoroutine(BuildAlbumCoroutine(album));
    }
    public void BuildRandomAlbum()
    {
        if (albums == null || albums.Length == 0) return;
        var idx = Random.Range(0, albums.Length);
        var album = albums[idx];
        if (album == null) return;

        _currentAlbum = album;
        _currentAlbumType = album.albumType;

        StopAllCoroutines();
        StartCoroutine(BuildAlbumCoroutine(album));
    }

    /// <summary>Reconstruye usando el álbum actual, si existe.</summary>
    public void RebuildCurrentAlbum()
    {
        if (_currentAlbum == null) return;
        StopAllCoroutines();
        StartCoroutine(BuildAlbumCoroutine(_currentAlbum));
    }

    /// <summary>Detiene la música actual (si hay AudioSource asignado).</summary>
    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        _currentMusicClip = null;
    }

    #endregion

    #region Core

    private IEnumerator BuildAlbumCoroutine(AlbumSO album)
    {
        if (galleryAnimatorHasAnimated == false)
        {
            galleryAnimator.SetActive(true);
            galleryAnimatorHasAnimated = true;
        }
        // Reset de progreso
        _currentImageIndex = -1;

        // Limpia todos los contenedores (sin logs)
        if (spawnPoints != null)
        {
            for (int c = 0; c < spawnPoints.Length; c++)
            {
                var ct = spawnPoints[c];
                if (!ct) continue;

                for (int i = ct.childCount - 1; i >= 0; i--)
                {
                    var child = ct.GetChild(i);
                    if (child) Destroy(child.gameObject);
                }
            }
        }
        yield return null;

        // Validaciones mínimas silenciosas
        if (album == null) yield break;
        if (album.imageList == null || album.imageList.Length == 0) yield break;
        if (spawnPoints == null || spawnPoints.Length == 0) yield break;

        _currentAlbumType = album.albumType;

        // Música (opcional, sin logs)
        if (audioSource != null && album.musicClips != null && album.musicClips.Length > 0)
        {
            _currentMusicClip = album.musicClips[Random.Range(0, album.musicClips.Length)];
            audioSource.clip = _currentMusicClip;
            audioSource.Play();
        }
        else
        {
            _currentMusicClip = null;
        }

        // Shuffle global de imágenes (trabajamos sobre índices para alinear videoClips)
        int total = album.imageList.Length;
        int[] imageOrder = CreateShuffledIndices(total);

        // Espera inicial
        if (spawnInitialDelay > 0f)
            if (FirstDelayFinished == false)
            {
                yield return new WaitForSeconds(spawnInitialDelay);
                FirstDelayFinished = true;
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

        int cursor = 0; // apunta al siguiente índice en imageOrder que falta por colocar

        while (cursor < total)
        {
            // Barajamos contenedores en cada ciclo para variar la disposición por lote
            int[] containerOrder = CreateShuffledIndices(spawnPoints.Length);

            // ¿Cuántos vamos a colocar en este lote?
            int remaining = total - cursor;
            int batchCount = Mathf.Min(spawnPoints.Length, remaining);

            // Instanciamos el lote
            for (int i = 0; i < batchCount; i++)
            {
                int globalOrderedIndex = cursor + i;
                int imageIdx = imageOrder[globalOrderedIndex];

                var tex = album.imageList[imageIdx];
                var target = spawnPoints[containerOrder[i]];

                if (tex == null || !target)
                {
                    _currentImageIndex = globalOrderedIndex;
                    if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
                    else yield return null;
                    continue;
                }

                bool isVideo = tex is RenderTexture;
                GameObject prefabToUse = isVideo ? (videoPrefab ?? rawImagePrefab) : rawImagePrefab;
                if (prefabToUse == null)
                {
                    _currentImageIndex = globalOrderedIndex;
                    if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
                    else yield return null;
                    continue;
                }

                var go = Instantiate(prefabToUse, target);

                var ri = go.GetComponent<RawImage>();
                if (!ri)
                {
                    Destroy(go);
                    _currentImageIndex = globalOrderedIndex;
                    if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
                    else yield return null;
                    continue;
                }

                ri.texture = tex;

                if (isVideo)
                {
                    var vp = go.GetComponent<VideoPlayer>();
                    var rt = tex as RenderTexture;

                    if (vp != null && rt != null)
                    {
                        vp.renderMode = VideoRenderMode.RenderTexture;
                        vp.targetTexture = rt;

                        if (album.videoClips != null &&
                            album.videoClips.Length > imageIdx &&
                            album.videoClips[imageIdx] != null)
                        {
                            vp.clip = album.videoClips[imageIdx];
                        }
                    }
                }

                if (Mathf.Abs(spawnRotationRange) > Mathf.Epsilon)
                {
                    float randomZ = Random.Range(-spawnRotationRange, spawnRotationRange);
                    go.transform.localEulerAngles = new Vector3(0f, 0f, randomZ);
                }

                _currentImageIndex = globalOrderedIndex;

                if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
                else yield return null;
            }

            cursor += batchCount;

            if (cursor < total)
            {
                if (cyclePause > 0f) yield return new WaitForSeconds(cyclePause);

                for (int c = 0; c < spawnPoints.Length; c++)
                {
                    var ct = spawnPoints[c];
                    if (!ct) continue;

                    for (int i = ct.childCount - 1; i >= 0; i--)
                    {
                        var child = ct.GetChild(i);
                        if (child) Destroy(child.gameObject);
                    }
                }
                yield return null;
            }
        }

        yield break;
    }

    // Fisher–Yates
    private static int[] CreateShuffledIndices(int len)
    {
        if (len <= 0) return new int[0];
        int[] idx = new int[len];
        for (int i = 0; i < len; i++) idx[i] = i;

        for (int i = len - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        return idx;
    }

    #endregion

#if UNITY_EDITOR
    [CustomEditor(typeof(GalleryManager))]
    private class GalleryManagerEditor : Editor
    {
        private SerializedProperty _albums;
        private SerializedProperty _rawImagePrefab;
        private SerializedProperty _videoPrefab;
        private SerializedProperty _spawnPoints;
        private SerializedProperty _spawnInitialDelay;
        private SerializedProperty _spawnInterval;
        private SerializedProperty _spawnRotationRange;
        private SerializedProperty _cyclePause;
        private SerializedProperty _audioSource;

        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _box;
        private GUIStyle _signatureStyle;

        private void OnEnable()
        {
            _albums = serializedObject.FindProperty("albums");
            _rawImagePrefab = serializedObject.FindProperty("rawImagePrefab");
            _videoPrefab = serializedObject.FindProperty("videoPrefab");
            _spawnPoints = serializedObject.FindProperty("spawnPoints");
            _spawnInitialDelay = serializedObject.FindProperty("spawnInitialDelay");
            _spawnInterval = serializedObject.FindProperty("spawnInterval");
            _spawnRotationRange = serializedObject.FindProperty("spawnRotationRange");
            _cyclePause = serializedObject.FindProperty("cyclePause");
            _audioSource = serializedObject.FindProperty("audioSource");

            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _subTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            _box = new GUIStyle("Box");
            _signatureStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var gm = (GalleryManager)target;

            GUILayout.Space(4);
            GUILayout.Label("Gallery Manager", _titleStyle);
            GUILayout.Space(2);
            GUILayout.Label("by Eros Carrasco — v0.0.1 \nIDM - Immersive Media Exercise - 09/23/2025", _signatureStyle);
            GUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gm), typeof(MonoScript), false);
            }

            GUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
            GUILayout.Space(6);

            EditorGUILayout.LabelField("Data", _subTitleStyle);
            EditorGUILayout.PropertyField(_albums, new GUIContent("Albums"), true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("UI", _subTitleStyle);
            EditorGUILayout.PropertyField(_rawImagePrefab);
            EditorGUILayout.PropertyField(_videoPrefab);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Spawn", _subTitleStyle);
            EditorGUILayout.PropertyField(_spawnInitialDelay);
            EditorGUILayout.PropertyField(_spawnInterval);
            EditorGUILayout.PropertyField(_spawnRotationRange);
            EditorGUILayout.PropertyField(_cyclePause);
            EditorGUILayout.PropertyField(_spawnPoints, true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Audio", _subTitleStyle);
            EditorGUILayout.PropertyField(_audioSource);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("(reading only)", _subTitleStyle);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Current Album", gm.currentAlbum, typeof(AlbumSO), false);
                EditorGUILayout.EnumPopup("Current Album Type", gm.currentAlbumType);
                EditorGUILayout.IntField("SpawnPoints in Gallery", gm.spawnPointsSafeCount);
                EditorGUILayout.Space(6);

                EditorGUILayout.IntField("Images Total", gm.imagesTotal);
                EditorGUILayout.IntField("Current Image Index", gm.currentImageIndex);
                EditorGUILayout.IntField("Images Remaining", gm.imagesRemaining);
                EditorGUILayout.Space(6);

                EditorGUILayout.ObjectField("Music Clip", gm.currentMusicClip, typeof(AudioClip), false);
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Random (Play Mode)"))
                {
                    if (Application.isPlaying) gm.BuildRandomAlbum();
                }
                if (GUILayout.Button("Rebuild Current"))
                {
                    if (Application.isPlaying) gm.RebuildCurrentAlbum();
                }
                if (GUILayout.Button("Stop Audio"))
                {
                    if (Application.isPlaying) gm.StopAudio();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    // Helpers para el Editor (solo lectura, sin exponer en inspector normal)
    private int spawnPointsSafeCount => spawnPoints != null ? spawnPoints.Length : 0;
    private int imagesSafeCount => _currentAlbum != null && _currentAlbum.imageList != null ? _currentAlbum.imageList.Length : 0;

    // 👇 NUEVOS helpers de progreso
    public int imagesTotal => imagesSafeCount;
    public int imagesRemaining
    {
        get
        {
            if (_currentAlbum == null || _currentAlbum.imageList == null) return 0;
            return Mathf.Max(0, _currentAlbum.imageList.Length - currentImageIndex);
        }
    }
}
#endif