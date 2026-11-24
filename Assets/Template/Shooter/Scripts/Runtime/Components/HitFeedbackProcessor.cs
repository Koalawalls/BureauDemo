using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Provides reusable networked presentation for successful hits,
    /// including floating damage numbers and impact audio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class HitFeedbackController : NetworkBehaviour
    {
        #region Inspector

        [Header("Damage Numbers")]

        [SerializeField]
        [Tooltip("Whether floating damage numbers should be displayed.")]
        private bool m_ShowDamageNumbers = true;

        [SerializeField]
        [Tooltip("Duration of the floating damage number animation.")]
        [Min(0.1f)]
        private float m_DamageNumberDuration = 1.5f;

        [SerializeField]
        [Tooltip("Vertical offset applied to spawned damage numbers.")]
        private float m_DamageNumberOffset = 0.5f;

        [Header("Audio")]

        [SerializeField]
        [Tooltip("Sound played when this entity receives damage.")]
        private SoundDef m_HitSound;

        #endregion

        #region Public Methods

        /// <summary>
        /// Broadcasts hit feedback to all connected clients.
        /// This should only be invoked from authoritative hit processing.
        /// </summary>
        /// <param name="attackerId">Client ID responsible for the hit.</param>
        /// <param name="damage">Final damage dealt.</param>
        /// <param name="hitPoint">World-space impact position.</param>
        public void BroadcastHitFeedback(
            ulong attackerId,
            float damage,
            Vector3 hitPoint)
        {
            if (!IsSpawned)
            {
                return;
            }

            SendHitFeedbackRpc(
                attackerId,
                damage,
                hitPoint
            );
        }

        #endregion

        #region RPC

        [Rpc(SendTo.Everyone)]
        private void SendHitFeedbackRpc(
            ulong attackerId,
            float damage,
            Vector3 hitPoint)
        {
            bool isLocalAttacker =
                NetworkManager.Singleton != null &&
                NetworkManager.Singleton.LocalClientId == attackerId;

            CreateHitFeedback(
                hitPoint,
                damage,
                isLocalAttacker
            );

            if (m_HitSound != null)
            {
                CoreDirector
                    .RequestAudio(m_HitSound)
                    .WithPosition(hitPoint)
                    .Play();
            }
        }

        #endregion

        #region Feedback

        private void CreateHitFeedback(
            Vector3 hitPoint,
            float damage,
            bool isLocalAttacker)
        {
            if (m_ShowDamageNumbers)
            {
                CreateDamageNumberVisual(
                    hitPoint,
                    damage,
                    isLocalAttacker
                );
            }
        }

        private void CreateDamageNumberVisual(
            Vector3 position,
            float damage,
            bool isLocalAttacker)
        {
            GameObject damageNumberObject =
                new GameObject("DamageNumber");

            damageNumberObject.transform.position =
                position +
                Vector3.up * m_DamageNumberOffset;

            TextMesh textMesh =
                damageNumberObject.AddComponent<TextMesh>();

            textMesh.text =
                $"-{damage:F0}";

            textMesh.fontSize = 48;
            textMesh.characterSize = 0.05f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.red;

            StartCoroutine(
                AnimateDamageNumber(
                    damageNumberObject.transform,
                    textMesh
                )
            );
        }

        private IEnumerator AnimateDamageNumber(
            Transform numberTransform,
            TextMesh textMesh)
        {
            float elapsed = 0f;

            Vector3 startPosition =
                numberTransform.position;

            Color startColor =
                textMesh.color;

            while (elapsed <
                   m_DamageNumberDuration)
            {
                elapsed += Time.deltaTime;

                float progress =
                    elapsed /
                    m_DamageNumberDuration;

                numberTransform.position =
                    startPosition +
                    Vector3.up * progress;

                textMesh.color =
                    new Color(
                        startColor.r,
                        startColor.g,
                        startColor.b,
                        1f - progress
                    );

                if (Camera.main != null)
                {
                    numberTransform.LookAt(
                        Camera.main.transform
                    );

                    numberTransform.Rotate(
                        0f,
                        180f,
                        0f
                    );
                }

                yield return null;
            }

            Destroy(
                numberTransform.gameObject
            );
        }

        #endregion
    }
}