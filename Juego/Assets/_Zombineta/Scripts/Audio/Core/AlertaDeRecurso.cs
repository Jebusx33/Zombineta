namespace Zombineta.Audio
{
    /// <summary>
    /// Cuando suena el aviso de un recurso bajo: al entrar en el tramo bajo y despues cada tanto,
    /// solo mientras se sigue gastando. Con la bateria guardada o el tanque ya vacio no insiste.
    /// </summary>
    public sealed class AlertaDeRecurso
    {
        readonly float umbral;
        readonly float segundosEntreAvisos;
        float anterior;
        float espera;
        bool bajo;

        public AlertaDeRecurso(float umbral, float segundosEntreAvisos)
        {
            this.umbral = umbral;
            this.segundosEntreAvisos = segundosEntreAvisos;
        }

        /// <summary>Empieza de cero: si arranca bajo, avisa una vez.</summary>
        public void Reiniciar(float fraccion)
        {
            anterior = fraccion;
            bajo = false;
        }

        /// <summary>Devuelve true en el cuadro en que tiene que sonar.</summary>
        public bool Avanzar(float fraccion, float dt)
        {
            bool gastando = fraccion < anterior;
            anterior = fraccion;

            if (fraccion <= 0f || fraccion > umbral)
            {
                bajo = false;
                return false;
            }
            if (!bajo)
            {
                bajo = true;
                espera = segundosEntreAvisos;
                return true;
            }
            if (!gastando)
                return false;

            espera -= dt;
            if (espera > 0f)
                return false;
            espera = segundosEntreAvisos;
            return true;
        }
    }
}
