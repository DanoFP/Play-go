# RealmForge – Spec de Mecánicas Age of Empires 2
*Fecha: 2026-04-19 | Base: v1.0*

---

## 1. Estado Actual

| Sistema | Estado | Cobertura |
|---|---|---|
| Economía (4 recursos) | Completo | Core |
| Edificios (8 tipos, grid) | Completo | Core |
| Aldeanos (recolección auto) | Completo | Core |
| Terreno procedural | Completo | Básico |
| Niebla de guerra (textura) | Completo | Core |
| Territorio | Funcional | Básico |
| 4 Razas con bonificaciones | Completo | Balanceado |
| Cámara RTS | Completo | Estándar |
| UI (IMGUI) | Completo | Core |
| Combate | **Ausente** | — |
| IA enemiga | **Ausente** | — |
| Árbol tecnológico | **Ausente** | — |
| Unidades militares | **Ausente** | — |
| Muros / Torres (funcionales) | **Ausente** | — |
| Población | **Ausente** | — |
| Diplomacia / Comercio | **Ausente** | — |
| Modos de juego | **Ausente** | — |

---

## 2. Sistemas Requeridos (AoE2)

### 2.1 Población y Límite de Unidades

**Descripción:**  
AoE2 limita el número de unidades activas mediante un sistema de "population cap". Las casas aumentan el límite. El Town Center provee 5 de población base.

**Reglas:**
- Cada unidad consume N puntos de población al crearse.
- Cada Casa agrega +10 al límite de población.
- Town Center base: +5 al límite.
- Límite máximo: 200 (configurable por modo de juego).
- Si el límite está lleno, no se puede crear ninguna unidad nueva.
- UI: Mostrar "Población actual / Límite" en el HUD.

**Impacto en código existente:**
- `ResourceManager` ya tiene `PopulationCapacity` sin uso → activarlo.
- `Building.cs` → registrar capacidad al completar.
- Nuevas unidades deben verificar límite antes de crearse.

---

### 2.2 Árbol Tecnológico (Research)

**Descripción:**  
Los jugadores pueden investigar mejoras en edificios especializados. Las mejoras desbloquean unidades, reducen costos, aumentan estadísticas o habilitan nuevas mecánicas.

**Edificios que proveen investigación:**
| Edificio | Mejoras ejemplo |
|---|---|
| Blacksmith | Armas (+1 ataque), Armadura (+1 defensa), Velocidad de ataque |
| Town Center | Loom (aldeanos +15 HP), Town Watch (visión +4), Town Patrol |
| Lumber Camp | Double Bit Axe (madera más rápida), Bow Saw |
| Mill | Horse Collar (comida más rápida), Heavy Plow |
| Market | Trade, Coinage (tributar sin pérdida), Banking |
| University | Masonry (edificios +10% HP), Fortified Wall, Ballistics |
| Monastery | Redemption, Fervor (monjes más rápidos) |
| Castle | Unique Unit unlock, Hoardings, Conscription |

**Estructura de datos:**
```
ResearchData {
  name, description, buildingRequired, cost, researchTime,
  effects: List<ResearchEffect>  // +stat, unlock unit, reduce cost
}

ResearchEffect {
  type: enum (AttackBonus, DefenseBonus, GatherRate, PopCap, SpeedBonus, UnlockUnit)
  value: float
  target: string  // "Villager", "Archer", "all", etc.
}
```

**Flujo:**
1. Jugador selecciona edificio con investigación disponible.
2. Panel lateral muestra mejoras disponibles/completadas.
3. Investigación toma tiempo real + consume recursos.
4. Al completar, `ResearchManager` aplica el efecto globalmente.

---

### 2.3 Edades (Ages)

**Descripción:**  
AoE2 divide el juego en 4 edades que desbloquean progresivamente más edificios, unidades y mejoras.

| Edad | Nombre | Requisito | Costo |
|---|---|---|---|
| I | Dark Age | — | — (inicio) |
| II | Feudal Age | 500F + 2 edificios distintos | 500F, 500G |
| III | Castle Age | Castle construido o equivalente | 800F, 800G |
| IV | Imperial Age | Todos los Castle Age buildings | 1000F, 1000G |

**Impacto:**
- Cada edificio tiene `minAge` requerida.
- Cada unidad tiene `minAge` requerida.
- Al avanzar de edad: cutscene/banner + todos los edificios se desbloquean.
- `GameManager` expone `CurrentAge` (enum).

---

### 2.4 Unidades Militares

**Descripción:**  
Reemplaza el modelo solo-aldeano por múltiples tipos de unidades con stats de combate.

#### 2.4.1 Clases base de unidades

| Clase | Descripción |
|---|---|
| `Unit` | Base: HP, velocidad, línea de visión, ataque, armadura |
| `MeleeUnit` | Ataque cuerpo a cuerpo (rango 1) |
| `RangedUnit` | Dispara proyectiles (rango variable) |
| `SiegeUnit` | Alta daño a estructuras, lento |
| `NavalUnit` | Solo sobre agua |
| `Monk` | Cura unidades aliadas, convierte enemigos |
| `CivilianUnit` (Villager) | Recolecta, construye, combate básico |

#### 2.4.2 Unidades comunes (compartidas por razas)

| Unidad | Edificio | Edad | Rol |
|---|---|---|---|
| Militia / Man-at-Arms / Long Swordsman | Barracks | I/II/III | Infantería melee |
| Spearman / Pikeman / Halberdier | Barracks | I/II/III | Anti-caballería |
| Scout Cavalry / Light Cav / Hussar | Stable | II/III/IV | Exploración rápida |
| Knight / Cavalier / Paladin | Stable | III/III/IV | Caballería pesada |
| Archer / Crossbowman / Arbalest | Archery Range | II/III/IV | Ranged DPS |
| Skirmisher / Elite Skirmisher | Archery Range | II/III/IV | Anti-archer |
| Trebuchet / Mangonel / Scorpion | Siege Workshop | III/III/III | Asedio |
| Monk | Monastery | III | Curación/Conversión |
| Trade Cart | Market | II | Comercio |
| Transport Ship | Dock | II | Naval transporte |
| War Galley | Dock | II | Naval combate |

#### 2.4.3 Unidades únicas por raza

| Raza | Unidad Única | Descripción |
|---|---|---|
| Humans (Terran Alliance) | Royal Guardsman | Infantería rápida, +10% velocidad |
| Elves (Sylvan Covenant) | Forest Warden | Arquero sigiloso en bosques, +20% daño en terreno forestal |
| Dwarves (Iron Clans) | Ironbreaker | Unidad melee pesada, +50% HP vs estructuras |
| Orcs (Bloodpeak Horde) | Warchief | Unidad melee con aura +5% ataque a unidades cercanas |

---

### 2.5 Sistema de Combate

**Descripción:**  
Las unidades atacan automáticamente a enemigos en rango (aggro). Los edificios pueden ser atacados. El juego termina si el Town Center del jugador es destruido.

**Stats de combate en `Unit`:**
```
Attack (float)
AttackRange (float)       // 1 = melee, >1 = ranged
AttackSpeed (float)       // ataques por segundo
Defense (float)           // daño recibido × (1 - Defense/100)
MeleeArmor (float)
PierceArmor (float)
HP / MaxHP (float)
MovementSpeed (float)
LineOfSight (float)       // radio de visión
```

**Tipos de daño:**
- `Melee` → reducido por `MeleeArmor`
- `Pierce` → reducido por `PierceArmor`
- `Siege` → reducción mínima, bonus vs edificios

**Lógica de aggro:**
1. Cada unidad tiene un radio de aggro (`LineOfSight / 2`).
2. Si detecta una unidad enemiga en radio → automáticamente ataca.
3. Si la unidad atacante se aleja demasiado → vuelve a posición original.
4. Comandos manuales del jugador sobrescriben el aggro.

**Proyectiles:**
- Unidades ranged crean un GameObject proyectil que viaja hacia el objetivo.
- Al impactar aplica daño y se destruye.

**Muerte:**
- Al llegar a 0 HP → animación de caída (escala a 0, 0.3s) → Destroy.
- Edificios al morir → colapso visual (ya implementado en `Building.Die()`).

---

### 2.6 Edificios Militares y Nuevos Edificios

Edificios faltantes a implementar:

| Edificio | Edad | Función |
|---|---|---|
| **Barracks** | I | Entrena infantería (Militia, Spearman) |
| **Archery Range** | II | Entrena arqueros y escaramuzadores |
| **Stable** | II | Entrena caballería |
| **Siege Workshop** | III | Construye máquinas de asedio |
| **Blacksmith** | II | Investigaciones de combate |
| **University** | III | Investigaciones defensivas y avanzadas |
| **Monastery** | III | Entrena Monjes |
| **Castle** | III | Unidad única, investigaciones de castillo |
| **Dock** | II | Construye barcos, comercio naval |
| **Wall (Stone)** | II | Muro defensivo, soporta ataques |
| **Gate** | II | Muro con apertura automática para aliados |
| **Outpost** | I | Torre de vigilancia ligera |
| **Watch Tower / Guard Tower / Keep** | II/III/IV | Torres con proyectiles |
| **Bombard Tower** | IV | Torre con cañón, daño de asedio |
| **Mill** | I | Investigaciones de granja, eje de granja |
| **Lumber Camp** | I | Investigaciones de madera, punto de depósito |
| **Mining Camp** | I | Investigaciones de minería, punto de depósito |
| **Wonder** | IV | Condición de victoria alternativa |

---

### 2.7 IA Enemiga

**Descripción:**  
Al menos un jugador CPU con comportamiento RTS básico: recolectar recursos, construir base, entrenar unidades y atacar al jugador.

**Niveles de dificultad:**
- **Easy:** Solo defiende, ataca ocasionalmente.
- **Medium:** Estrategia balanceada, usa todas las edades.
- **Hard:** Presión constante, micro de unidades.

**Módulos de IA:**
```
AIController {
  BuildOrder: List<BuildTask>     // qué construir y cuándo
  EconomyPhase()                  // priorizar recolección
  MilitaryPhase()                 // entrenar y organizar ejército
  AttackPhase()                   // enviar grupos de ataque
  DefensePhase()                  // responder a ataques
  AgeUpLogic()                    // cuándo avanzar de edad
}
```

**Árbol de decisiones (FSM):**
```
Bootstrap → EarlyEconomy → MidMilitary → LateAttack
                 ↓                 ↓
           AgeUpCheck        DefenseCheck
```

**Build order básico (AI Medium):**
```
1. 6 Aldeanos → recursos (food, wood)
2. Construir: Mill, Lumber Camp, 4 Casas
3. Avanzar a Feudal Age
4. Construir: Blacksmith, Archery Range / Barracks
5. Entrenar 8 unidades militares
6. Atacar con oleadas de 6-10 unidades cada 3 min
7. Avanzar a Castle Age, construir Castle
8. Entrenar unidades únicas
```

---

### 2.8 Muros y Puertas

**Descripción:**  
Muros son estructuras lineales (1×1 por segmento) que forman barreras. Las puertas se abren automáticamente para unidades aliadas.

**Mecánicas:**
- Click en dos puntos → se colocan segmentos de muro entre ambos.
- Cada segmento tiene HP (Stone Wall: 1500 HP).
- Unidades enemigas atacan muros si bloquean el camino.
- Las puertas detectan unidades aliadas en radio de 2 unidades → se abren.

---

### 2.9 Pathfinding

**Descripción:**  
El movimiento actual de aldeanos es lineal (sin evitar obstáculos). Se necesita pathfinding real para unidades militares y aldeanos.

**Requisitos:**
- Evitar edificios, muros, terreno montañoso.
- Soporte para grupos (formation movement).
- Performance: hasta 200 unidades activas.

**Solución propuesta:**
- Implementar A* sobre el grid existente (GridSize = 2f).
- Marcar celdas como: Free, Building, Wall, Water (impassable).
- Grupos: calculan path para el líder, el resto sigue con offset.
- Usar `NavMeshAgent` de Unity como alternativa más rápida.

---

### 2.10 Comercio

**Descripción:**  
Los Markets permiten comerciar recursos entre sí con una tasa de cambio. Con múltiples jugadores, los Trade Carts generan oro viajando entre mercados.

**Comercio interno (single-player):**
- Intercambio en el Market: vender 100 de un recurso → recibir X de otro (tasa variable).
- Tasa varía según abundancia: más vendes, peor tasa (mínimo 20%).
- Coinage mejora el ratio.

**Comercio externo (con IA o multijugador):**
- Trade Cart viaja desde tu Market al Market enemigo/aliado.
- Distancia mayor = más oro generado por viaje.
- Requiere camino sin obstáculos.

---

### 2.11 Diplomacia

**Descripción:**  
Relaciones entre jugadores: aliado, neutral, enemigo. Solo relevante con IA o multijugador.

**Acciones:**
- Declarar alianza / guerra.
- Enviar tributo de recursos.
- Compartir visión con aliados.

---

### 2.12 Condiciones de Victoria y Derrota

**Descripción:**  
Actualmente el juego no tiene condiciones de fin claras.

**Modos de victoria:**
| Modo | Condición |
|---|---|
| Conquest | Destruir todos los Town Centers y Castles enemigos |
| Wonder | Mantener una Wonder construida por 300 segundos |
| Relics | Capturar y mantener todas las reliquias por 500 segundos |
| Time Limit | Mayor puntuación al terminar el tiempo |
| Regicide | Matar al Rey del enemigo (unidad especial) |

**Derrota:**
- Town Center destruido → derrota inmediata.
- Sin unidades vivas Y sin aldeanos → derrota.

---

### 2.13 Minería y Depósitos

**Descripción:**  
En AoE2, los aldeanos deben depositar recursos en el edificio correcto:
- Madera → Lumber Camp (o Town Center)
- Comida (granja) → Mill (o Town Center)
- Oro → Mining Camp (o Town Center)
- Piedra → Mining Camp (o Town Center)

**Reglas:**
- Si no hay un edificio de depósito cerca → el aldeano regresa al Town Center.
- Colocar Lumber Camps/Mining Camps cerca de recursos = eficiencia.
- Granjas se plantan alrededor del Mill (8 casillas adyacentes).

**Impacto en `Villager.cs`:**
- Lógica `ReturnToBase()` debe buscar el depósito más cercano por tipo de recurso.
- Granjas son recursos "infinitos" con tasa de recolección fija (requiere replantar).

---

### 2.14 Granjas

**Descripción:**  
Las granjas en AoE2 son recursos renovables pero agotables. Un aldeano trabaja la granja → produce comida hasta agotar la granja → replanta automáticamente (si tiene recursos) o espera.

**Mecánica:**
- Granja tiene 175 "cosechas" de comida (base).
- Replantar cuesta 60 de madera (mejora con Crop Rotation → gratuito).
- Solo 1 aldeano por granja.
- Granjas deben colocarse adyacentes al Mill para el bonus de eficiencia.

---

### 2.15 Reliquias y Monjes

**Descripción:**  
- Las Reliquias son objetos dispersos en el mapa.
- Los Monjes pueden recoger reliquias y llevarlas al Monastery.
- Cada reliquia en Monastery → +0.5 Gold/s.
- Condición de victoria opcional: controlar todas las reliquias por X tiempo.

**Monjes:**
- Pueden curar unidades aliadas en rango.
- Pueden convertir unidades enemigas (lento, con fe).
- La conversión tiene cooldown de 60s para el mismo tipo de unidad.

---

### 2.16 Naval (Básico)

**Descripción:**  
El terreno ya tiene agua (`SceneSetup`). Se necesita:
- Dock (edificio en la orilla).
- Fishing Ship (recolecta comida de zonas de pesca).
- Transport Ship (lleva unidades tierra→tierra por agua).
- War Galley / Fire Ship / Demolition Ship (combate naval).

**Requisitos de pathfinding:**
- Las unidades navales solo se mueven en celdas de agua.
- Las unidades terrestres no pueden cruzar agua sin Transport Ship.

---

### 2.17 Selección Múltiple de Unidades

**Descripción:**  
Actualmente solo se puede seleccionar 1 unidad a la vez.

**Requisitos:**
- Clic y arrastre → rectángulo de selección → selecciona todas las unidades dentro.
- Shift+Clic → agrega unidad a la selección actual.
- Ctrl+clic → selecciona todas las unidades del mismo tipo en pantalla.
- Panel de selección múltiple en la UI (iconos de cada unidad seleccionada).
- Comandos de movimiento/ataque se aplican a toda la selección.
- Formation movement: unidades se mueven en formación.

---

### 2.18 Formaciones y Comportamiento de Grupo

**Descripción:**  
Cuando se mueve un grupo de unidades:
- Formación "Box": unidades se organizan en cuadro.
- Formación "Line": unidades en fila.
- Las unidades mantienen distancia relativa dentro del grupo.
- Si un enemigo entra en rango de aggro → se rompe la formación y atacan.

---

### 2.19 Puntuación y Estadísticas

**Descripción:**  
AoE2 tiene una pantalla de estadísticas detallada al final de la partida.

**Métricas a rastrear:**
- Recursos recolectados totales por tipo.
- Edificios construidos/destruidos.
- Unidades entrenadas/perdidas.
- Unidades enemigas eliminadas.
- Territorio controlado.
- Tiempo en cada edad.

---

### 2.20 Mejoras de Audio y Retroalimentación Visual

**Descripción:**  
Actualmente no hay audio. AoE2 usa audio extensamente para feedback.

**Requerimientos:**
- Sonido al seleccionar unidad.
- Sonido al ordenar movimiento.
- Sonido de ataque (espada, flechas, catapulta).
- Sonido de muerte de unidad.
- Sonido de construcción completa.
- Sonido de investigación completa.
- Música de fondo por fase de juego (exploración, combate).
- Efectos visuales: partículas de golpe, polvo al moverse, chispa en edificios dañados.

---

## 3. Restricciones Técnicas

| Aspecto | Situación actual | Impacto |
|---|---|---|
| WebGL (960×600) | Build activa | Pathfinding y IA deben ser eficientes (single-thread) |
| IMGUI | Toda la UI | Selección múltiple y paneles complejos son difíciles en IMGUI → migrar a uGUI (Canvas) |
| Runtime bootstrap | No hay prefabs | Crear prefabs para unidades militares es más mantenible |
| Sin audio | — | Necesita AudioSource/AudioClip setup desde cero |
| Grid 2f | Pathfinding base | A* o NavMesh sobre el grid existente |
| Sin ScriptableObjects en uso | BuildingData existe | Crear SO para UnitData, ResearchData, TechTreeData |

---

## 4. Prioridad de Implementación

| Prioridad | Sistema | Motivo |
|---|---|---|
| P0 | Población | Sin esto, el juego no tiene tensión económica |
| P0 | Combate + Unidades militares | Core del RTS |
| P0 | IA enemiga básica | Sin enemigo el juego no tiene objetivo |
| P0 | Condiciones de victoria/derrota | Sin fin, no hay partida |
| P1 | Barracks + Archery Range + Stable | Primer contenido militar |
| P1 | Pathfinding (A*/NavMesh) | Las unidades militares lo necesitan |
| P1 | Árbol tecnológico (Edad I-II) | Progresión básica |
| P1 | Selección múltiple | UX básico de RTS |
| P2 | Muros y Torres funcionales | Defensa |
| P2 | Granjas / Lumber Camps / Mining Camps | Economía completa |
| P2 | Edades completas (I-IV) | Progresión completa |
| P2 | Unidades únicas por raza | Diferenciación |
| P3 | Monjes y Reliquias | Mecánica secundaria |
| P3 | Naval | Requiere mucho trabajo |
| P3 | Diplomacia | Solo relevante con multijugador |
| P3 | Audio | Pulido |
| P4 | Migración a uGUI | Deuda técnica |
| P4 | Estadísticas de fin de partida | Pulido |

---

*Este documento cubre los sistemas necesarios para paridad funcional con Age of Empires 2 en un contexto single-player con IA local.*
