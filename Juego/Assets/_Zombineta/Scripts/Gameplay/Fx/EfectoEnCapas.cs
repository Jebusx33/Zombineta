using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Fx
{
    /// <summary>
    /// Una instancia de un prefab de efecto armado por capas (el sistema raiz y sus hijos, ver FxBuilder):
    /// recuerda el color y el orden de dibujo relativo con que se armo cada capa, para poder pintarlo de gris
    /// (sin sangre alta) y llevarlo al carril que toca sin perder cual va delante de cual.
    /// </summary>
    public sealed class EfectoEnCapas
    {
        readonly ParticleSystem raiz;
        readonly ParticleSystem[] capas;
        readonly ParticleSystem.MinMaxGradient[] colores;
        readonly ParticleSystemRenderer[] renderers;
        readonly int[] ordenes;
        readonly Transform piso;

        public Transform Transform => raiz.transform;
        public bool Vivo => raiz.IsAlive(true);

        public EfectoEnCapas(ParticleSystem prefab, Transform padre)
        {
            raiz = Object.Instantiate(prefab, padre);
            capas = raiz.GetComponentsInChildren<ParticleSystem>(true);
            colores = new ParticleSystem.MinMaxGradient[capas.Length];
            for (int i = 0; i < capas.Length; i++)
                colores[i] = capas[i].main.startColor;
            renderers = raiz.GetComponentsInChildren<ParticleSystemRenderer>(true);
            ordenes = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                ordenes[i] = renderers[i].sortingOrder;
                renderers[i].sortingLayerID = LaneSorting.GameLayerId;
            }
            piso = raiz.transform.Find("Piso");
        }

        /// <summary>Cada capa queda en "orden" mas el lugar relativo con que se armo.</summary>
        public void Ordenar(int orden)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                int o = orden + ordenes[i];
                if (renderers[i].sortingOrder != o)
                    renderers[i].sortingOrder = o;
            }
        }

        /// <summary>Con un tinte, todas las capas salen de ese color; sin tinte, con el suyo.</summary>
        public void Tenir(Color? tinte)
        {
            for (int i = 0; i < capas.Length; i++)
            {
                var main = capas[i].main;
                main.startColor = tinte.HasValue ? new ParticleSystem.MinMaxGradient(tinte.Value) : colores[i];
            }
        }

        /// <summary>Particulas por segundo de una capa continua (0 es la raiz; los hijos, en orden).</summary>
        public void Tasa(int capa, float porSegundo)
        {
            if (capa < 0 || capa >= capas.Length)
                return;
            var em = capas[capa].emission;
            em.rateOverTime = porSegundo;
        }

        /// <summary>
        /// Reproduce una rafaga desde cero en ese punto, girada esos grados sobre la pantalla. El plano
        /// donde chocan las gotas (y lo que cuelga de el, como un charco) va a la altura de mundo
        /// "piso", derecho aunque el efecto este girado: el prefab lo trae a una distancia fija, pero
        /// el efecto puede nacer alto o bajo.
        /// </summary>
        public void Reproducir(Vector3 en, float grados, float piso)
        {
            raiz.transform.SetPositionAndRotation(en, Quaternion.Euler(0f, 0f, grados));
            if (this.piso != null)
                this.piso.SetPositionAndRotation(new Vector3(en.x, piso, en.z), Quaternion.identity);
            raiz.Clear(true);
            raiz.Play(true);
        }

        /// <summary>Para los efectos continuos: sigue emitiendo desde donde este.</summary>
        public void Encender()
        {
            if (!raiz.isPlaying)
                raiz.Play(true);
        }
    }
}
