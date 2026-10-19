using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Un aspecto concreto que puede tomar un zombie: sus cuadros de caminata, impacto y muerte,
    /// mas como se anima. El arte diferencia a cada uno; el codigo solo elige y reproduce.
    /// </summary>
    [Serializable]
    public sealed class ZombieLook
    {
        public string name;
        public Sprite[] walk;
        [Tooltip("Cuando le pegan y aguanta (un golpe comun, o el balazo que no lo mata).")]
        public Sprite[] hit;
        [Tooltip("Segundos que dura la animacion de hit: conviene que cubra lo que queda frenado (golpeFreno).")]
        public float hitSeconds = 0.2f;

        [Tooltip("Caminata lenta (arrastrando los pies) para la pelea a pie. Se usa mientras el zombie camine mas despacio " +
                 "que 'walkSlowSpeedLimit'; los que corren siguen con 'walk'. En la moto no se usa.")]
        public Sprite[] walkSlow;
        [Tooltip("Metros por segundo por debajo de los cuales, a pie, camina en vez de correr.")]
        public float walkSlowSpeedLimit = 2.6f;
        [Tooltip("Metros que avanza en un ciclo completo de la caminata lenta. El ciclo se reparte segun lo que se desplaza " +
                 "de verdad (no segun el reloj): si patina, subir; si parece que corre en el lugar, bajar.")]
        public float walkSlowStrideMeters = 1.3f;

        [Tooltip("Preparando el ataque a pie: se reparte entre el inicio de la preparacion y el golpe. " +
                 "Si esta, el aviso (inclinacion, estocada y color rojo) ya viene dibujado y no se simula.")]
        public Sprite[] attack;

        [Tooltip("Derribado por el remate: cae y queda tirado. Si esta, reemplaza a la rotacion de relleno.")]
        public Sprite[] knockdown;
        [Tooltip("Segundos que tarda en caer del todo; despues se queda en el ultimo cuadro hasta levantarse.")]
        public float knockdownFallSeconds = 0.55f;
        public Sprite[] death;
        [Tooltip("Muerte por bala. Vacio: usa death para toda causa.")]
        public Sprite[] deathBullet;
        public float walkFps = 8f;
        public float deathFps = 10f;

        [Tooltip("Compensa la resolucion de la hoja de origen, no el tamano de juego: las hojas completas " +
                 "(Anciano, HombreRojo) van en 1 y las de Zombies_poses en 2,5 porque sus figuras salen " +
                 "~2,3 veces mas chicas. El tamano en pantalla lo dan HordeView.bodyScale y la escala del tipo. " +
                 "La sombra del zombie descuenta este valor.")]
        public float scale = 1f;
        public bool poseBob;

        /// <summary>Los cuadros de muerte segun la causa: la de bala si el look la tiene.</summary>
        public Sprite[] DeathFor(DeathCause cause) =>
            cause == DeathCause.Bullet && deathBullet != null && deathBullet.Length > 0 ? deathBullet : death;
    }

    /// <summary>Los looks disponibles para un tipo de zombie (Comun, Corredor, Pesado, ...).</summary>
    [Serializable]
    public sealed class TypeLooks
    {
        public string typeName;
        public List<ZombieLook> looks = new List<ZombieLook>();
    }

    /// <summary>
    /// Arquetipos visuales por tipo de zombie: reemplaza el tinte unico por variedad de sprites.
    /// Se balancea desde Settings/ZombieLooks.asset, nunca desde codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Zombie Look Set", fileName = "ZombieLooks")]
    public sealed class ZombieLookSet : ScriptableObject
    {
        public List<TypeLooks> types = new List<TypeLooks>();

        static readonly List<ZombieLook> Empty = new List<ZombieLook>();

        /// <summary>Los looks del tipo pedido, o una lista vacia si no tiene ninguno.</summary>
        public IReadOnlyList<ZombieLook> For(int typeIndex)
        {
            if (types == null || typeIndex < 0 || typeIndex >= types.Count)
                return Empty;
            var t = types[typeIndex];
            return t != null && t.looks != null ? t.looks : Empty;
        }
    }
}
