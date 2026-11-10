using UnityEngine;

namespace Zombineta.Auxilio
{
    public enum PoseAPie { Quieta, Camina, GolpeBajo, GolpeAlto, Lastimada, Derribada, Dispara }

    /// <summary>
    /// Elige que animacion de la protagonista a pie se ve y por que cuadro va. Derribada le gana a
    /// todo; un golpe, un disparo o el dolor se terminan de ver y despues vuelve a quieta o
    /// caminando. El disparo arranca en el cuadro en que el brazo apunta: el tiro sale al apretar.
    /// </summary>
    public sealed class ReproductorAPie
    {
        readonly AnimacionesAPie animaciones;
        float tiempo;

        public ReproductorAPie(AnimacionesAPie animaciones) => this.animaciones = animaciones;

        public PoseAPie Actual { get; private set; }

        ClipAPie Clip => animaciones.Clip(Actual);

        public Sprite Cuadro => Clip.cuadros[Clip.Indice(tiempo)];

        public void Golpear(bool remate)
        {
            if (Actual != PoseAPie.Derribada)
                Empezar(remate ? PoseAPie.GolpeAlto : PoseAPie.GolpeBajo);
        }

        public void Disparar()
        {
            if (Actual == PoseAPie.Derribada)
                return;
            Empezar(PoseAPie.Dispara);
            tiempo = animaciones.cuadroDelTiro / animaciones.dispara.cuadrosPorSegundo;
        }

        public void Lastimar()
        {
            if (Actual != PoseAPie.Derribada)
                Empezar(PoseAPie.Lastimada);
        }

        public void Reiniciar() => Empezar(PoseAPie.Quieta);

        public void Avanzar(float dt, bool camina, bool derribada)
        {
            tiempo += dt;
            if (derribada)
            {
                Seguir(PoseAPie.Derribada);
                return;
            }
            bool terminaSola = Actual == PoseAPie.GolpeBajo || Actual == PoseAPie.GolpeAlto ||
                               Actual == PoseAPie.Lastimada || Actual == PoseAPie.Dispara;
            if (!terminaSola || tiempo >= Clip.Duracion)
                Seguir(camina ? PoseAPie.Camina : PoseAPie.Quieta);
        }

        void Seguir(PoseAPie pose)
        {
            if (Actual != pose)
                Empezar(pose);
        }

        void Empezar(PoseAPie pose)
        {
            Actual = pose;
            tiempo = 0f;
        }
    }
}
