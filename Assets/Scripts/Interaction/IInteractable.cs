using UnityEngine;

namespace Replica.Interaction
{
    /// <summary>
    /// Interfaz base para cualquier entidad u objeto con el que el jugador puede interactuar (NPCs, objetos de misión, etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Texto que se mostrará en pantalla al acercarse (ej. "[E] Hablar con Madre").
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Determina si la entidad está disponible para interactuar en este momento.
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// Ejecuta la acción de interacción cuando el jugador presiona la tecla de acción.
        /// </summary>
        /// <param name="interactor">GameObject del jugador que inicia la interacción.</param>
        void Interact(GameObject interactor);
    }
}
