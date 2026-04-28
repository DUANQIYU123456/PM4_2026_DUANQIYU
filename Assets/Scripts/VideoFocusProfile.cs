using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Tuning profile for VideoFocusController — drives a VideoPlayer's audio fade based on a Focusable's focus state.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Video Focus Profile")]
    public class VideoFocusProfile : ScriptableObject
    {
        //==================== AUDIO =====================
        [Header("Audio")]
        [Tooltip("Target audio volume while the object IS focused")]
        [Range(0f, 1f)]
        public float focusedVolume = 1f;

        //==================== TWEEN =====================
        [Header("Tween")]
        [Tooltip("Seconds to fade audio in on focus")]
        [Min(0f)]
        public float fadeInDuration = 0.5f;

        [Tooltip("Seconds to fade audio out on unfocus")]
        [Min(0f)]
        public float fadeOutDuration = 0.5f;

        [Tooltip("Easing for both fades")]
        public Ease ease = Ease.InOutSine;
    }
}
