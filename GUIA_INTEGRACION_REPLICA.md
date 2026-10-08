# Guía de Integración: Réplica en Unity

Guía paso a paso para configurar la escena, componentes, NavMesh, UI y cinemáticas del juego en Unity.

---

## 1. Generación de Misiones (1 Clic)

1. En la barra superior de Unity, ve a **Tools > Réplica > Generar Misiones Predefinidas**.
2. Se crearán 5 ScriptableObjects en `Assets/Data/Quests/` (`Q1_Madre_y_Cachorro.asset` a `Q5_Mujer_y_Tortuga.asset`) con todos los diálogos oficiales.

---

## 2. Configuración del Jugador (Perro)

1. Crea un GameObject en la escena llamado `Player` o usa el prefab del modelo del perro.
2. Agrega los componentes:
   - **`CharacterController`**: Ajusta `Height` (ej. 0.8) y `Radius` (ej. 0.3) al cuerpo del perro.
   - **`PlayerController`** ([`PlayerController.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Player/PlayerController.cs)):
     - *Walk Speed*: `3.5`
     - *Run Speed*: `7.0`
     - *Animator*: Arrastra el Animator del perro (debe tener floats/bools `Speed`, `IsRunning`, `IsGrounded`).
     - *Mount Point Back*: Crea un hijo vacío en el lomo del perro (`Mount_Back`) y arrástralo aquí (para la tortuga).
     - *Mount Point Head*: Crea un hijo vacío sobre la cabeza (`Mount_Head`) y arrástralo aquí (para el cuervo).
   - **`InteractionManager`** ([`InteractionManager.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Interaction/InteractionManager.cs)):
     - *Detection Radius*: `2.5`
     - *SphereCollider* (Opcional): Con `Is Trigger` activado.

---

## 3. Configuración del NavMesh para NPCs

1. Selecciona los suelos, calles y edificios estáticos y márcalos como **Navigation Static**.
2. Ve a **Window > AI > Navigation** (o el componente `NavMeshSurface` si usas NavMesh Packages) y haz clic en **Bake**.

---

## 4. Configuración de los NPCs y Acompañantes

Cada misión consta de 2 NPCs: el **Dueño** y la **Mascota Perdida**.

### A. Mascotas Perdidas (Acompañantes)
Coloca el prefab de la mascota en su ubicación inicial en el mapa. Añade:
- **`NavMeshAgent`**: Ajusta radio y altura.
- **`CompanionAI`** ([`CompanionAI.cs`](file:///home/f/WeJaminRep/Assets/Scripts/AI/CompanionAI.cs)):
  - **Cachorro (Q1), Cachetoncito (Q2), Perro Guía (Q3)**: `Companion Type = GroundNavMesh`.
  - **Cuervo (Q4)**: `Companion Type = Flying`.
  - **Tortuga (Q5)**: `Companion Type = Mounted`.
- **`InteractableNPC`** ([`InteractableNPC.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Interaction/InteractableNPC.cs)):
  - *Role*: `LostPet`
  - *Quest Id*: `Q1`, `Q2`, `Q3`, `Q4` o `Q5`.
  - *Quest Data*: Arrastra el asset correspondiente de `Assets/Data/Quests/`.
  - *Npc Display Name*: Ej. "Cachorro", "Cachetoncito", "Tortuga".
  - *Companion AI*: Arrastra el componente `CompanionAI` del mismo objeto.

### B. Dueños (Humanos)
Coloca el NPC humano en su ubicación. Añade:
- **`InteractableNPC`** ([`InteractableNPC.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Interaction/InteractableNPC.cs)):
  - *Role*: `Owner`
  - *Quest Id*: La misma ID que su mascota (`Q1` a `Q5`).
  - *Quest Data*: El mismo asset de `Assets/Data/Quests/`.
  - *Npc Display Name*: Ej. "Madre", "Anciana", "Humano Ciego".
  - *Companion AI*: Arrastra el `CompanionAI` de la mascota correspondiente.
  - *Delivery Destination Point*: Crea un Transform hijo al lado del humano para que la mascota se quede ahí tras la entrega.

---

## 5. UI y Canvas (Diálogos y HUD)

Crea un **Canvas** en la escena con dos paneles principales:

1. **Panel de Diálogo** (asociado a [`DialogueSystem.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Dialogue/DialogueSystem.cs)):
   - Añade un GameObject `DialogueManager` con el script `DialogueSystem`.
   - Asigna en el Inspector:
     - *Dialogue Panel*: El GameObject raíz del cuadro de diálogo.
     - *Speaker Name Text*: Componente `TextMeshProUGUI` del nombre.
     - *Dialogue Content Text*: Componente `TextMeshProUGUI` del texto del diálogo.
     - *Continue Indicator*: Icono o texto de `[E] Continuar`.
     - *Typing Speed*: `0.02`.

2. **Panel de HUD** (asociado a [`GameHUD.cs`](file:///home/f/WeJaminRep/Assets/Scripts/UI/GameHUD.cs)):
   - Añade el script `GameHUD` al Canvas o a un GameObject HUD.
   - Asigna:
     - *Interaction Prompt Text*: TMP_Text para `"[E] Hablar con..."`.
     - *Interaction Prompt Panel*: Panel que contiene dicho texto.
     - *Quest Counter Text*: TMP_Text para `"Rescates: 0 / 5"`.

---

## 6. Gestor de Misiones y Secuencia Final (Q6)

1. Crea un GameObject vacío `GameManager`.
2. Añade **`QuestManager`** ([`QuestManager.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Quests/QuestManager.cs)):
   - *Quests*: Lista de 5 elementos; arrastra los assets `Q1` a `Q5`.
   - *Play Intro On Start*: `True` (mostrará Q0.1 automáticamente).
   - *Final Sequence Controller*: Arrastra el objeto con `FinalSequenceController`.
3. Crea un GameObject `FinalSequenceManager` con **`FinalSequenceController`** ([`FinalSequenceController.cs`](file:///home/f/WeJaminRep/Assets/Scripts/Quests/FinalSequenceController.cs)):
   - *Gameplay Camera*: Cámara principal del juego.
   - *Cinematic Camera*: Cámara secundaria colocada para los planos finales.
   - *End Screen Canvas*: Canvas de Fin de Juego / Créditos.
   - *UnityEvents*: Conecta aquí tus AudioClips o efectos visuales en `onEmotionalClimax` y `onGameFinished`.

---

## 7. Flujo del Ciclo de Juego

```
[Inicio: Q0.1 Prólogo]
        │
        ▼
[Exploración Libre de la Ciudad] 
        │
        ├──► Hablar con Dueño (Q1..Q5) -> Misión "InProgress"
        ├──► Encontrar y hablar con Mascota -> "CompanionRecruited" (Te sigue)
        └──► Llevar Mascota con el Dueño -> "Completed" (+1 al contador)
        │
        ▼
[Al completar las 5 misiones]
        │
        ▼
[Q6 Secuencia Final Automática]
 ├── Q6F1: Cuervo avisa y guía
 ├── Q6F3: Falso dueño ("Ese no es mi perro")
 ├── Q6F4: Reencuentro con verdadero dueño
 └── Q6F5: Narración final y pantalla de créditos
```
