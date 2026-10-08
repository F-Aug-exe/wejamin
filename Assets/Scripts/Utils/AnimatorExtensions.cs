using System.Collections.Generic;
using UnityEngine;

namespace Replica.Utils
{
    /// <summary>
    /// IDs de animación del "3D Stylized Animated Dogs Kit" (parámetro entero "AnimationID"
    /// del controlador Dog_Animator_Controler).
    /// </summary>
    public static class DogAnimationIds
    {
        public const string ParameterName = "AnimationID";

        public const int Idle = 0;        // Breathing
        public const int WiggleTail = 1;  // WigglingTail
        public const int Walk = 2;        // Walking01
        public const int WalkAlt = 3;     // Walking02
        public const int Run = 4;         // Running
        public const int Eat = 5;         // Eating
        public const int Angry = 6;       // Angry
        public const int Sit = 7;         // Sitting
    }

    /// <summary>
    /// Métodos de extensión para el Animator que solo asignan un parámetro si existe en el
    /// controlador. Así un mismo script funciona con distintos modelos (perro, humano, ave, tortuga)
    /// sin llenar la consola de advertencias "Parameter 'X' does not exist".
    /// </summary>
    public static class AnimatorExtensions
    {
        private static readonly Dictionary<int, Dictionary<int, AnimatorControllerParameterType>> Cache =
            new Dictionary<int, Dictionary<int, AnimatorControllerParameterType>>();

        public static bool HasParameter(this Animator animator, string name, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
                return false;

            int id = animator.GetInstanceID();
            if (!Cache.TryGetValue(id, out var parameters))
            {
                if (!animator.isInitialized)
                    return false;

                parameters = new Dictionary<int, AnimatorControllerParameterType>();
                foreach (var p in animator.parameters)
                    parameters[p.nameHash] = p.type;
                Cache[id] = parameters;
            }

            return parameters.TryGetValue(Animator.StringToHash(name), out var foundType) && foundType == type;
        }

        public static void TrySetFloat(this Animator animator, string name, float value)
        {
            if (animator.HasParameter(name, AnimatorControllerParameterType.Float))
                animator.SetFloat(name, value);
        }

        public static void TrySetBool(this Animator animator, string name, bool value)
        {
            if (animator.HasParameter(name, AnimatorControllerParameterType.Bool))
                animator.SetBool(name, value);
        }

        public static void TrySetInteger(this Animator animator, string name, int value)
        {
            if (animator.HasParameter(name, AnimatorControllerParameterType.Int))
                animator.SetInteger(name, value);
        }
    }
}
