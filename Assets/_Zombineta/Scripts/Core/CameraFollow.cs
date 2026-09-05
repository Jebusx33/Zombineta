using UnityEngine;

namespace Zombineta.Core
{
    /// <summary>
    /// Sigue a la jugadora quedandose atras de ella, para que aparezca a la
    /// derecha de la pantalla y quede aire a la izquierda donde se ve venir a la
    /// horda. Sin ese aire la persecucion no se lee: la amenaza queda fuera de
    /// cuadro justo cuando importa.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("0 = pegada, valores mas altos = mas suave.")]
        [SerializeField] float smoothing = 0.12f;

        [SerializeField] float fixedY = 0f;

        float velocityX;

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            float targetX = run.ToWorldX(run.Sim.State.PlayerX - run.Config.cameraTrailMeters);
            float x = smoothing <= 0f
                ? targetX
                : Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothing);

            transform.position = new Vector3(x, fixedY, transform.position.z);
        }
    }
}
