# RealmForge – Plan de Implementación AoE2
*Fecha: 2026-04-19 | Basado en SPEC.md*

---

## Resumen Ejecutivo

El juego tiene ~15% de las mecánicas de AoE2. El 85% restante se divide en **4 fases** ordenadas por dependencias y valor de gameplay. Cada fase produce un juego más jugable al terminar.

```
FASE 1: RTS Jugable (P0) ──► FASE 2: Economía Completa (P1) ──► FASE 3: Progresión (P2) ──► FASE 4: Contenido y Pulido (P3/P4)
```

---

## FASE 1 — RTS Jugable
*Meta: Partida completa con enemigo, combate y condición de victoria.*

### Tarea 1.1 — Sistema de Población

**Archivos a modificar:** `ResourceManager.cs`, `Building.cs`, `UIManager.cs`  
**Nuevos archivos:** ninguno

**Pasos:**
1. En `ResourceManager`: agregar `int CurrentPopulation`, `int PopulationCap`. Exponer `CanTrainUnit()`.
2. En `Building.cs` → `OnBuildComplete()`: si el edificio es `House`, llamar `ResourceManager.AddPopCap(10)`. Si es `TownCenter`, `AddPopCap(5)`.
3. En toda creación de unidades (a implementar): verificar `CanTrainUnit()` antes de instanciar.
4. En `UIManager`: mostrar "Población X/Y" en el HUD (junto al contador de workers actual).

**Criterio de aceptación:** No se pueden crear unidades cuando pop == cap. Las casas aumentan el cap. El HUD muestra el ratio.

---

### Tarea 1.2 — ScriptableObject `UnitData`

**Archivos nuevos:** `Assets/Scripts/Units/UnitData.cs`  
**Archivos a modificar:** `SceneSetup.cs` (registrar unidades base)

**Estructura:**
```csharp
[CreateAssetMenu]
public class UnitData : ScriptableObject {
    public string unitName;
    public UnitType type;           // Villager, Militia, Archer, Knight, Monk, Trebuchet...
    public RaceType raceRequired;   // None = cualquier raza
    public int age;                 // 1-4
    public int goldCost, foodCost, woodCost;
    public int populationCost;
    public float trainTime;
    public float maxHP;
    public float attack;
    public float attackRange;
    public float attackSpeed;       // ataques/segundo
    public DamageType damageType;   // Melee, Pierce, Siege
    public float meleeArmor;
    public float pierceArmor;
    public float moveSpeed;
    public float lineOfSight;
    public BuildingType trainingBuilding;
    public Color unitColor;
}
```

**Pasos:**
1. Crear `UnitData.cs` como ScriptableObject.
2. Crear instancias en `SceneSetup` (o como assets): Villager, Militia, Spearman, Archer, Scout, Knight, Trebuchet, Monk.
3. Registrar un diccionario `UnitType → UnitData` en un nuevo `UnitRegistry`.

---

### Tarea 1.3 — Clase base `Unit.cs` y tipos militares

**Archivos nuevos:** `Assets/Scripts/Units/Unit.cs`, `Assets/Scripts/Units/MilitaryUnit.cs`  
**Archivos a modificar:** `Villager.cs` (heredar de `Unit`)

**`Unit.cs` (base):**
```
- HP / MaxHP
- MoveSpeed, LineOfSight
- IsAlive bool
- Owner (PlayerID o AIController ref)
- Select() / Deselect()
- TakeDamage(float amount, DamageType type)
- Die()
- CommandMoveTo(Vector3)
- UpdateHealthBar()
- Vision reveal (fog)
```

**`MilitaryUnit.cs` (hereda Unit):**
```
- UnitData stats
- Attack, AttackRange, AttackSpeed
- MeleeArmor, PierceArmor
- State: Idle, Moving, Attacking, Patrolling
- FindNearestEnemy() → aggro
- AttackTarget(Unit target)
- AttackBuilding(Building target)
- Patrol(Vector3 from, Vector3 to)
- ReturnToGuardPosition()
```

**Mesh de unidades** (procedural, sin prefabs):
- Infantería: Capsule + pequeño cubo (escudo)
- Arqueros: Capsule + cilindro delgado (arco)
- Caballería: Capsule grande + rectángulo debajo (caballo)
- Caballeros: Versión caballería con color oscuro

**Pasos:**
1. Crear `Unit.cs` con toda la lógica compartida.
2. Refactorizar `Villager.cs` para heredar de `Unit` (mover HP, selección, fog reveal).
3. Crear `MilitaryUnit.cs` con FSM de combate.
4. Agregar 4 tipos concretos: `Militia`, `Archer`, `Scout`, `Spearman` (subclases o configurados via UnitData).

---

### Tarea 1.4 — Sistema de Combate

**Archivos a modificar:** `MilitaryUnit.cs`, `Building.cs`  
**Archivos nuevos:** `Assets/Scripts/Units/Projectile.cs`

**Combate melee:**
1. Unidad detecta enemigo en `AttackRange`.
2. Se mueve hasta estar adyacente.
3. Cada `1/AttackSpeed` segundos → llama `target.TakeDamage(Attack, DamageType.Melee)`.
4. Target aplica reducción: `damage = attack - armor`.

**Combate ranged:**
1. Unidad se detiene al llegar a `AttackRange`.
2. Instancia `Projectile` apuntando al target.
3. Proyectil viaja en línea recta a `25f/s`.
4. Al llegar → `TakeDamage(Attack, DamageType.Pierce)` → se destruye.

**`Projectile.cs`:**
```
- Vector3 targetPos (o Transform target para seguimiento)
- float speed, float damage, DamageType type
- Update(): moverse hacia target
- OnTriggerEnter(): aplicar daño
```

**Edificios recibiendo daño:**
- `Building.TakeDamage()` ya existe → solo se necesita que las unidades enemigas lo llamen.
- Agregar `DamageType` al método existente.

**Criterio de aceptación:** Unidades atacan otras unidades y edificios. Proyectiles visibles. Unidades mueren al llegar a 0 HP.

---

### Tarea 1.5 — Edificios militares: Barracks y Archery Range

**Archivos a modificar:** `SceneSetup.cs`, `BuildingManager.cs`, `Building.cs`, `UIManager.cs`  
**Archivos a modificar:** `BuildingData.cs` (agregar nuevos tipos)

**Barracks:**
- Costo: 175 madera
- Edad: I
- Entrena: Militia (60F, 20G, 21s), Spearman (35F, 25W, 22s)
- Al seleccionar → UI muestra cola de entrenamiento

**Archery Range:**
- Costo: 175 madera
- Edad: II
- Entrena: Archer (25W, 45G, 35s), Skirmisher (25F, 35W, 22s)

**Sistema de cola de entrenamiento:**
```
Building {
  Queue<UnitData> trainingQueue;
  float trainingProgress;
  
  EnqueueUnit(UnitData)   // agrega a la cola (máx 5)
  ProcessTraining()       // reduce timer, spawna unidad al llegar a 0
  SpawnUnit(UnitData)     // instancia MilitaryUnit frente al edificio
}
```

**UI de entrenamiento:**
- Panel inferior cuando está seleccionado el edificio.
- Iconos de unidades entrenables (o botones de texto en IMGUI).
- Barra de progreso de entrenamiento actual.
- Lista de unidades en cola.

---

### Tarea 1.6 — IA Enemiga Básica

**Archivos nuevos:** `Assets/Scripts/AI/AIController.cs`, `Assets/Scripts/AI/AIBuildOrder.cs`

**Diseño:**
```
AIController {
  RaceData race
  List<Building> myBuildings
  List<Unit> myUnits
  List<Villager> myVillagers
  
  // FSM de alto nivel
  enum AIPhase { EarlyEconomy, BuildMilitary, Attack, Defend }
  AIPhase currentPhase
  
  Update()  // tick cada 0.5s para performance
}
```

**Build order (Easy):**
```
t=0:    Spawn 3 aldeanos → recolectar
t=60:   Construir House (pop cap)
t=120:  Construir Farm
t=180:  Construir Barracks
t=300:  Entrenar 3 Militia → patrullar base
t=480:  Atacar con grupo de 3 unidades
t=600:  Repetir ataque con 6 unidades
```

**Lógica de ataque:**
1. Reunir N unidades en punto de rally.
2. Calcular camino hacia Town Center del jugador.
3. Mover grupo hacia objetivo.
4. Si hay unidades enemigas en camino → atacarlas.
5. Destruir edificios en orden de prioridad: TownCenter > Barracks > Houses.

**Posicionamiento del enemigo:**
- Spawn en esquina opuesta del mapa al jugador.
- `SceneSetup` coloca el Town Center de la IA en (`50, 0, 50`) si el jugador empieza en (`-50, 0, -50`).

**Criterio de aceptación:** La IA construye una base, entrena unidades y las manda a atacar al jugador.

---

### Tarea 1.7 — Condiciones de Victoria y Derrota

**Archivos a modificar:** `GameManager.cs`, `UIManager.cs`

**Lógica:**
```csharp
// En GameManager.Update()
void CheckVictoryConditions() {
    bool playerAlive = PlayerHasTownCenter() || PlayerHasVillagers();
    bool enemyAlive = AI.HasTownCenter() || AI.HasAnyBuilding();
    
    if (!playerAlive)  → GameOver(defeat)
    if (!enemyAlive)   → GameOver(victory)
}
```

**Pantalla de resultado:**
- Overlay negro semi-transparente.
- "VICTORIA" (dorado) o "DERROTA" (rojo).
- Estadísticas básicas: tiempo jugado, edificios construidos, unidades entrenadas.
- Botón "Jugar de nuevo" → recarga la escena.

---

## FASE 2 — Economía Completa
*Meta: Sistema económico a paridad con AoE2.*

### Tarea 2.1 — Lumber Camp, Mining Camp y Mill

**Descripción:** Puntos de depósito especializados. Los aldeanos buscan el depósito más cercano del tipo correcto al regresar.

**Pasos:**
1. Agregar `LumberCamp`, `MiningCamp`, `Mill` a `BuildingType`.
2. En `SceneSetup`, registrar los tipos con sus costos (Lumber Camp: 100W; Mining Camp: 100W; Mill: 100W).
3. En `Villager.ReturnToBase()`: seleccionar depósito correcto según `carriedResource`:
   - Wood → LumberCamp o TownCenter
   - Gold/Stone → MiningCamp o TownCenter
   - Food (granja) → Mill o TownCenter
4. Agregar a la UI de construcción.

---

### Tarea 2.2 — Granjas renovables

**Archivos a modificar:** `ResourceNode.cs`, `Villager.cs`

**Cambios:**
1. `ResourceNode` → agregar tipo `Farm` con `capacity = 175`.
2. Granjas se colocan en el grid como edificios (2×2) con `Building` component.
3. Al agotarse → si el jugador tiene 60 madera, se replanta automáticamente (costo automático).
4. Solo 1 aldeano puede trabajar por granja.
5. `Villager` en modo Farm no regresa a depositar: produce directamente si está en rango del Mill.

---

### Tarea 2.3 — Selección múltiple de unidades

**Archivos a modificar:** `BuildingManager.cs`, `UIManager.cs`  
**Archivos nuevos:** `Assets/Scripts/Core/SelectionManager.cs`

**`SelectionManager.cs`:**
```
List<Unit> selectedUnits
bool isDragging
Vector2 dragStart, dragEnd

// Input
OnLeftMouseDown()    → start drag o click
OnLeftMouseDrag()    → calcula rectángulo de selección
OnLeftMouseUp()      → finaliza drag, selecciona unidades dentro
OnShiftClick()       → añade/quita de la selección
OnRightClick()       → comando a todos los seleccionados

// Helpers
Rect GetScreenRect(Vector2 a, Vector2 b)
bool IsUnitInRect(Unit u, Rect screenRect)
DrawSelectionRect()  → en OnGUI(), dibuja rectángulo de selección
```

**Comandos multi-unidad:**
- Move: cada unidad recibe un offset en formación.
- Attack: todas atacan al mismo target.
- Stop: todas detienen su acción actual.

---

### Tarea 2.4 — Pathfinding con A*

**Archivos nuevos:** `Assets/Scripts/Core/Pathfinder.cs`, `Assets/Scripts/Core/GridMap.cs`

**GridMap:**
- Extiende el grid existente de `BuildingManager`.
- Agrega tipos de celda: `Free`, `Building`, `Wall`, `Water`, `Mountain`.
- `SetCell(Vector2Int, CellType)` → llamado al construir/destruir edificios.

**A\*:**
```
Pathfinder.FindPath(Vector3 start, Vector3 end) → List<Vector3>
```
- Heurística: distancia Manhattan.
- Permite diagonal con costo 1.4.
- Cache de paths frecuentes (TTL: 2 segundos).
- Fallback: si no hay path, unidad reporta "No path found" y espera.

**Integración con unidades:**
- `Unit.CommandMoveTo(Vector3 target)` → llama `Pathfinder.FindPath()` → guarda `List<Vector3> path` → en `Update()` avanza nodo por nodo.

---

### Tarea 2.5 — Torres funcionales

**Descripción:** Las torres ya existen visualmente pero no atacan. Deben comportarse como unidades ranged estacionarias.

**Pasos:**
1. En `Building.OnBuildComplete()`: si es `Tower`, agregar componente `TowerDefense`.
2. `TowerDefense.cs`:
   ```
   float attackRange = 8f
   float attackDamage = 10f
   float attackSpeed = 1f
   
   Update() → FindNearestEnemy(attackRange) → AttackTarget()
   ```
3. Guardia: Torres atacan a unidades enemigas en rango, igual que `MilitaryUnit`.

---

### Tarea 2.6 — Muros y Puertas

**Archivos nuevos:** `Assets/Scripts/Buildings/WallSegment.cs`

**Pasos:**
1. En modo de construcción de muros: primer click → punto A, segundo click → punto B.
2. `BuildingManager` genera segmentos de muro cada 2 unidades entre A y B.
3. `WallSegment`: HP = 1500, bloquea pathfinding (marca celda como `Wall`).
4. Puerta: segmento especial con `isGate = true`. Detecta unidades aliadas en radio 2 → marca su celda como `Free` temporalmente.
5. Unidades enemigas atacan muros si bloquean el path.

---

## FASE 3 — Progresión
*Meta: Sistema de edades y árbol tecnológico completo.*

### Tarea 3.1 — Sistema de Edades

**Archivos nuevos:** `Assets/Scripts/Core/AgeManager.cs`  
**Archivos a modificar:** `GameManager.cs`, `UIManager.cs`, `BuildingData.cs`, `UnitData.cs`

**`AgeManager.cs`:**
```csharp
public enum Age { DarkAge = 1, FeudalAge = 2, CastleAge = 3, ImperialAge = 4 }

Age currentAge = Age.DarkAge;

bool CanAdvance()     // verifica requisitos de edificios y recursos
void StartAdvance()   // lanza coroutine de avance (60-100s)
void OnAgeComplete()  // desbloquea edificios/unidades, notifica UI

int[] AgeCost = { 0, 0, 500, 800, 1000 };  // Gold por edad
int[] FoodCost = { 0, 0, 500, 800, 1000 }; // Food por edad
```

**Requisitos por edad:**
- Feudal: 2 edificios distintos construidos.
- Castle: Barracks + Archery Range + Blacksmith.
- Imperial: University + Siege Workshop + Monastery.

**UI:**
- Botón "Advance Age" en el panel inferior (visible cuando se puede avanzar).
- Progreso de avance de edad (barra).
- Banner de notificación al completar ("You have advanced to the Feudal Age!").

**Bloqueo de contenido:**
- `BuildingData.minAge` y `UnitData.minAge` → no se muestran en UI si `currentAge < minAge`.

---

### Tarea 3.2 — Sistema de Investigación

**Archivos nuevos:** `Assets/Scripts/Core/ResearchManager.cs`, `Assets/Scripts/Core/ResearchData.cs`

**`ResearchData.cs`:**
```csharp
public class ResearchData {
    public string name, description;
    public BuildingType requiredBuilding;
    public int goldCost, foodCost, woodCost;
    public float researchTime;
    public List<ResearchEffect> effects;
    public ResearchData prerequisite; // si requiere otra investigación antes
}

public class ResearchEffect {
    public EffectType type;  // AttackBonus, DefenseBonus, GatherRate, etc.
    public float value;
    public string targetUnit; // "Villager", "Archer", "all", etc.
}
```

**`ResearchManager.cs`:**
```
Dictionary<string, bool> completedResearch
void StartResearch(ResearchData, Building)
void CompleteResearch(ResearchData)  // aplica efectos globalmente
bool IsResearched(string name)
float GetStatMultiplier(UnitType unit, StatType stat)
```

**Investigaciones base (Edad I-II):**
- Loom (Town Center): Aldeanos +15 HP, +1 armor. Costo: 50G. Tiempo: 25s.
- Double Bit Axe (Lumber Camp): Madera +15%. Costo: 100F. Tiempo: 25s.
- Horse Collar (Mill): Comida granjas +15%. Costo: 75F. Tiempo: 20s.
- Fletching (Blacksmith): Arqueros +1 ataque, +1 rango. Costo: 100F, 75G. Tiempo: 30s.
- Scale Mail (Blacksmith): Infantería +1 melee armor. Costo: 100F. Tiempo: 40s.
- Town Watch (Town Center): LOS +4 para aldenos y edificios. Costo: 75F. Tiempo: 25s.

**Integración con Building:**
- `Building` tiene `List<ResearchData> availableResearch`.
- Al seleccionar un edificio con investigaciones → panel lateral muestra las disponibles.
- Click en investigación → `ResearchManager.StartResearch()` si puede pagarlo.

---

### Tarea 3.3 — Edificios restantes (Blacksmith, University, Monastery, Castle, Siege Workshop)

Para cada edificio:

**Blacksmith** (Edad II, 150W):
- Solo provee investigaciones de combate.
- Visual: Cubo + cilindro (yunque encima).

**University** (Edad III, 200W, 200S):
- Investigaciones defensivas: Masonry (+10% HP edificios), Fortified Wall, Ballistics.

**Monastery** (Edad III, 175W):
- Entrena Monks (100G, 45s).
- Investiga: Redemption, Fervor, Sanctity.

**Siege Workshop** (Edad III, 200W):
- Entrena: Mangonel (160W, 135G, 46s), Trebuchet (200W, 200G, 50s, desplegable).
- Trebuchet: debe desplegarse/recogerse antes/después de atacar.

**Castle** (Edad III, 650S):
- Entrena Unique Unit de la raza.
- Investiga mejoras de Castle (Hoardings, Conscription).
- Visual: Grande (3×3), con torres en las esquinas.

---

### Tarea 3.4 — Unidades únicas por raza

**Pasos:**
1. En `UnitData`, agregar `RaceType raceRequired`.
2. Castle muestra solo la unidad única de la raza del jugador.
3. Implementar 4 unidades únicas (stats y visual diferenciados):
   - **Royal Guardsman** (Humans): Milicia rápida. +10% velocidad. Color plateado.
   - **Forest Warden** (Elves): Arquero sigiloso. +20% daño en terreno forestal. Color verde oscuro.
   - **Ironbreaker** (Dwarves): Melee pesado. +50% HP. +10 melee armor. Color gris oscuro.
   - **Warchief** (Orcs): Melee con aura. Unidades aliadas en radio 5 tienen +5% ataque. Color rojo.

---

### Tarea 3.5 — Monjes y Reliquias

**Archivos nuevos:** `Assets/Scripts/Units/Monk.cs`, `Assets/Scripts/World/Relic.cs`

**Relic:**
- Objeto pequeño (esfera dorada) colocado en el mapa por `SceneSetup` (3-5 reliquias).
- Monk que llega a una reliquia → la recoge (Monk.carriedRelic = true).
- Si Monk con reliquia llega al Monastery → relic queda "guardada".
- Cada reliquia guardada → +0.5G/s en `ResourceManager`.

**Monk.cs** (hereda Unit):
```
bool isCarryingRelic
Unit healTarget
float faith (0-100, se regenera)
float conversionRange = 10f
float healRange = 5f

HealNearestAlly()       // +10HP/s al aliado más cercano
ConvertEnemy(Unit)      // requiere 25 faith, tarda 15-25s
PickUpRelic(Relic)
DepositRelic()
```

---

## FASE 4 — Contenido y Pulido
*Meta: UX completo, audio, modos de juego adicionales.*

### Tarea 4.1 — Naval básico

**Nuevos archivos:** `Assets/Scripts/Units/NavalUnit.cs`, `Assets/Scripts/Buildings/Dock.cs`

**Pasos:**
1. `GridMap` marca celdas de agua en `SceneSetup`.
2. Dock: edificio que debe colocarse en la orilla (celda adyacente a agua).
3. Dock entrena: Fishing Ship (recolecta comida), Transport Ship, War Galley.
4. `NavalUnit` solo se mueve en celdas de agua. Pathfinding acuático separado.
5. Fishing Ship busca zonas de pesca (resource nodes de tipo Fish, colocados en el agua).

---

### Tarea 4.2 — Comercio en Market

**Pasos:**
1. En `UIManager`, cuando Market está seleccionado → mostrar panel de trading.
2. Panel: 6 botones (comprar/vender Wood, Stone, Food).
3. Precio inicial: 100 del recurso = 100 oro.
4. Precio empeora con cada transacción (±3 por transacción).
5. Precio mejora lentamente con el tiempo (+1 por 30s de inactividad).
6. Investigación "Coinage" → mejora todos los ratios en 15%.

---

### Tarea 4.3 — Migración a uGUI (Canvas)

**Motivación:** IMGUI tiene limitaciones para paneles complejos, animaciones y responsive layout.

**Pasos:**
1. Crear Canvas con CanvasScaler (960×600 reference, Scale With Screen Size).
2. Crear prefabs de UI: TopBar, BottomBar, BuildPanel, SelectionPanel, TrainingPanel.
3. Migrar cada sección de `UIManager.OnGUI()` a un GameObject de UI independiente.
4. Reemplazar las llamadas `GUI.*` por referencias a Text/Image/Button components.
5. `UIManager` pasa a ser un orquestador de paneles en vez de renderizador directo.

---

### Tarea 4.4 — Audio

**Archivos nuevos:** `Assets/Scripts/Core/AudioManager.cs`  
**Assets necesarios:** AudioClips (descargar de Open Game Art o generar proceduralmente)

**`AudioManager.cs`:**
```
void PlaySFX(SFXType type)    // selección, ataque, muerte, construcción
void PlayMusic(MusicTrack track, bool loop)
void SetMusicVolume(float v)
void SetSFXVolume(float v)
```

**SFX mínimos:**
- `unit_select`, `unit_move`, `unit_attack_melee`, `unit_attack_ranged`
- `unit_death`, `building_complete`, `research_complete`
- `error` (recursos insuficientes)
- `age_advance`

---

### Tarea 4.5 — Modos de juego y menú principal

**Nuevos modos:**
- **Standard** (actual): sin tiempo límite, victoria por conquista.
- **Deathmatch**: inicio con todos los recursos en máximo, solo combate.
- **Time Limit**: 20/40/60 minutos, gana el jugador con mayor puntuación.
- **Wonder Race**: gana el primero en construir y defender un Wonder por 300s.

**Menú principal:**
- Pantalla antes de la selección de raza.
- Opciones: Modo de juego, Dificultad de IA, Tamaño del mapa, Volumen.

---

### Tarea 4.6 — Pantalla de estadísticas

**Al terminar la partida:**
- Recursos recolectados por tipo.
- Edificios construidos / destruidos.
- Unidades entrenadas / perdidas.
- Unidades enemigas eliminadas.
- Tiempo en cada edad.
- Puntuación final.

---

## Estimación de Dependencias

```
1.2 UnitData ──► 1.3 Unit base ──► 1.4 Combate ──► 1.6 IA
                      │
                      └──► 1.5 Barracks ──► 1.6 IA
1.1 Población ──────────────────────────────► 1.6 IA

2.4 Pathfinding ──► 1.4 Combate (mejorar) ──► 2.6 Muros
2.3 Selección múltiple ──► 2.5 Torres ──► 2.6 Muros

3.1 Ages ──► 3.2 Research ──► 3.3 Edificios ──► 3.4 Unique Units

4.3 uGUI ──► (todo lo demás de UI)
```

---

## Orden de implementación sugerido (sprint-by-sprint)

| Sprint | Tareas | Entregable |
|---|---|---|
| S1 | 1.1, 1.2, 1.3 | Unidades militares entrenables, pop cap |
| S2 | 1.4, 1.5 | Combate funcional, Barracks y Archery Range |
| S3 | 1.6, 1.7 | IA enemiga básica, Victoria/Derrota |
| S4 | 2.4, 2.3 | Pathfinding A*, selección múltiple |
| S5 | 2.1, 2.2, 2.5, 2.6 | Economía completa, torres activas, muros |
| S6 | 3.1, 3.2 | Sistema de edades, investigaciones |
| S7 | 3.3, 3.4 | Edificios restantes, unidades únicas |
| S8 | 3.5 | Monjes y reliquias |
| S9 | 4.2, 4.5 | Comercio, modos de juego |
| S10 | 4.3 | Migración uGUI |
| S11 | 4.1 | Naval |
| S12 | 4.4, 4.6 | Audio, estadísticas, pulido final |

---

*Este plan asume un desarrollador solitario trabajando en Unity 6 con el código base actual de RealmForge v1.0.*
