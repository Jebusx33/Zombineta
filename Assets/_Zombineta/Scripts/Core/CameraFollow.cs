using UnityEngine;

namespace Zombineta.Core
{
    /// <summary>
    /// Sigue a la jugadora dejandola a la derecha del centro, para que quede
    /// aire a la izquierda y se vea venir a la horda. Sin ese aire la
    /// persecucion no se lee.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("Cuanto se corre la camara por delante de la jugadora, en metros.")]
        [SerializeField] float lookAhead = 4f;

        [Tooltip("0 = pegada, valores mas altos = mas suave.")]
        [SerializeField] float smoothing = 0.12f;

        [SerializeField] float fixedY = 0f;

        float velocityX;

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            float targetX = run.Sim.State.PlayerX + lookAhead;
            float x = smoothing <= 0f
                ? targetX
                : Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothing);

            transform.position = new Vector3(x, fixedY, transform.position.z);
        }
    }
}
