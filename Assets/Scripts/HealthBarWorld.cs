using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that billboards toward the local camera.
/// Attach to the World Space Canvas child of the player prefab.
/// Wire up 'fillImage' to the RawImage used as the fill bar inside the Canvas.
/// </summary>
public class HealthBarWorld : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RawImage used as the fill bar. Its RectTransform width is scaled to represent current health.")]
    public RawImage fillImage;

    [Header("Bar Colors")]
    public Color fullColor    = new Color(0.18f, 0.85f, 0.35f); // green
    public Color lowColor     = new Color(0.95f, 0.25f, 0.25f); // red
    [Tooltip("Health fraction below which the bar turns red.")]
    public float lowThreshold = 0.3f;

    [Header("Smoothing")]
    [Tooltip("How fast the fill lerps to the target value. Set to a very high number to disable smoothing.")]
    public float lerpSpeed = 8f;

    // Internal
    private float _targetFill = 1f;
    private float _currentFill = 1f;   // tracked manually since RawImage has no fillAmount
    private float _fullWidth;           // original RectTransform width = 100% health
    private RectTransform _fillRect;
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;

        if (fillImage != null)
        {
            _fillRect = fillImage.GetComponent<RectTransform>();
            _fullWidth = _fillRect.sizeDelta.x;  // capture the design-time full width
        }
    }

    void LateUpdate()
    {
        // --- Billboard: face the camera ---
        if (_cam != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - _cam.transform.position
            );
        }

        // --- Smooth fill via RectTransform width ---
        if (_fillRect != null)
        {
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, lerpSpeed * Time.deltaTime);

            // Scale width: left-anchored so it shrinks from the right
            _fillRect.sizeDelta = new Vector2(_fullWidth * _currentFill, _fillRect.sizeDelta.y);

            // Colour shift
            fillImage.color = (_currentFill > lowThreshold)
                ? fullColor
                : Color.Lerp(lowColor, fullColor, _currentFill / Mathf.Max(lowThreshold, 0.001f));
        }
    }

    /// <summary>
    /// Call this whenever health changes. Both values should be > 0.
    /// </summary>
    public void SetHealth(float current, float max)
    {
        _targetFill = Mathf.Clamp01(current / max);
    }
}
