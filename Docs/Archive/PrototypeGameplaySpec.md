# *Spin the Globe* — Game Design & Technical Gameplay Specification (Unity Engine)

---

## 1. High-Level Concept & Camera View
* **Perspective:** Fixed first-person desk view facing forward.
* **Environment Composition:**
  * **Center:** 3D Mechanical Globe on a brass pedestal.
  * **Back Wall:** Vintage corkboard world map with defined country boundaries and connective yarn routes.
  * **Left Side (Foreground/Desk):** Collectible figurine pedestal display (multi-tier evolution) and a rainy window with streaming drops.
  * **Right Side (Foreground/Desk):** Spring-loaded brass pin launcher and an analog Brass Tachometer (RPM/Speed gauge).

---

## 2. Core Gameplay Systems & Mathematical Loop

### 2.1. The 3D Mechanical Globe Engine
* **Unidirectional Rotation:**
  * The globe rotates strictly on its vertical polar axis in one direction: **West to East** (counter-clockwise viewed from the North Pole).
  * Any manual drag, flick, or swipe input is clamped so negative (east-to-west) forces are ignored.
* **Momentum & Friction Physics:**
  * Swiping imparts angular velocity $\omega$.
  * Each frame, velocity decays via natural exponential drag:
    $$\omega_{t+1} = \omega_t \times \mu_{\text{friction}} \quad (\text{default } \mu \approx 0.985)$$
  * **Baseline Auto-Spin Floor:** When unpowered, $\omega$ decays to $0$. When the **Auto-Motor** upgrade is active, it establishes a minimum speed floor $\omega_{\text{auto}}$. Velocity decays toward $\omega_{\text{auto}}$ and never drops below it:
    $$\omega_{t+1} = \max(\omega_{\text{auto}},\, \omega_t \times \mu)$$
* **Terminal Velocity & Tachometer Gauge:**
  * Velocity is capped by an upgradeable ceiling $\omega_{\text{max}}$.
  * A physical brass tachometer on the desk displays $\text{ratio} = \frac{\omega}{\omega_{\text{max}}}$.
  * When $\text{ratio} \ge 0.95$, the tachometer enters the **REDLINE ("MAX")** state, pulsing visually and preventing further acceleration.
* **$0^\circ$ Prime Meridian Origin Bonus:**
  * A visual brass notch and vertical ribbon mark $0^\circ$ longitude.
  * Every full $360^\circ$ rotation completed across the forward view triggers a base revolution cash payout:
    $$\text{Payout}_{\text{rev}} = \text{BaseRevolutionReward} \times \text{GlobalMultiplier}$$

---

### 2.2. The Wall Map, Country Pins & Globe Synchronization
* **Targeting:**
  * The corkboard map features discrete clickable country hitboxes (e.g., USA, UK, Belgium, France, Brazil, Japan, Australia, Egypt, etc.).
  * Smaller countries have higher base payout values ($\text{Value}_{\text{base}}$) than massive landmasses to reward sniper precision.
* **Dual-Surface Synchronization:**
  * Clicking a country on the wall map fires a pin from the desk launcher.
  * When a pin embeds into the map, an illuminated 3D marker instantly instantiates at the exact spherical latitude and longitude $(\text{lat}, \text{lon})$ on the 3D globe surface.
  * Red conspiracy yarn connects all active pins across the corkboard map.

---

### 2.3. The Brass Meridian Wire & Payout Trigger
* **The Meridian Wire:** A stationary vertical brass needle/wire runs directly down the front center of the globe.
* **Trigger Condition:**
  * A pinned country generates revenue **only** when its spherical longitude rotates directly beneath the Meridian Wire:
    $$|\text{CurrentLongitude}_{\text{country}} - \text{MeridianWireLongitude}| < \epsilon_{\text{threshold}}$$
* **One-Hit-Per-Revolution Lock:**
  * When a country passes the wire, it immediately executes a payout and sets a lock flag (`IsLocked = true`).
  * The country **cannot trigger again** during the same pass.
  * The lock resets (`IsLocked = false`) only after the globe rotates far enough away and completes a full $360^\circ$ revolution back to the front.
* **Country Payout Formula:**
  $$\text{Payout}_{\text{country}} = \text{Value}_{\text{base}} \times \text{PinMultiplier} \times \text{GlobalMultiplier}$$

---

### 2.4. Pin Durability & Visual Degradation
* **Base Tier (Level 0 Durability):**
  * Pins have $1$ durability charge.
  * Upon striking the Meridian Wire, the payout triggers, and the pin immediately pops out of the corkboard and falls to the desk with particle dust. The marker on the globe vanishes simultaneously.
* **Upgraded Durability (Level 1+):**
  * Pins possess $N$ charges ($N = 1 + \text{DurabilityLevel}$).
  * **Intermediate Passes:** When struck by the Meridian Wire, the pin stays in the cork, tilts sideways at an angle ($30^\circ\text{–}45^\circ$), wobbles violently, and sheds cork dust.
  * **Final Pass ($N = 0$):** The pin detaches completely and falls away.

---

### 2.5. Collectible Figurine Multipliers (Desk Restorations)
* Three pedestal slots on the left desk allow progressive figurine restoration across 4 visual tiers:
  1. **Tier 0 — Broken Rubble:** Shattered porcelain fragments ($1.0\times$ multiplier).
  2. **Tier 1 — Glued Stone:** Pieced together with visible dark adhesive seams ($1.5\times$ multiplier).
  3. **Tier 2 — Kintsugi Gold Veins:** Repaired with glowing gold lacquer seams ($2.0\times$ multiplier).
  4. **Tier 3 — Solid Polished Gold Masterpiece:** Gleaming metallic statue casting specular highlights ($3.0\times$ multiplier).
* The global multiplier applies across all base spins and country needle strikes:
  $$\text{GlobalMultiplier} = \sum \text{FigurineMultipliers}$$

---

## 3. Unity Component Architecture (MonoBehaviours & ScriptableObjects)

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs          // Central game loop, currency tracker, multipliers
│   │   └── AudioManager.cs         // Procedural/clip audio (spins, clicks, chimes)
│   ├── Globe/
│   │   ├── GlobeController.cs      // Drag input, unidirectional clamp, friction, auto-floor
│   │   ├── MeridianTrigger.cs      // Raycast / angular check for wire passes & locks
│   │   └── GlobePinMarker.cs       // Spherical coord positioning & marker instantiation
│   ├── Map/
│   │   ├── WallMapManager.cs       // Detects raycast clicks on country colliders
│   │   ├── CountryTarget.cs        // Base data: country ID, lat/lon coords, base value
│   │   ├── PinInstance.cs          // Durability tracking, tilt/wobble tweening, physics drop
│   │   └── YarnRenderer.cs         // LineRenderer connecting active pins on the wall
│   ├── Desk/
│   │   ├── TachometerUI.cs         // Needle rotation mapped to current/max velocity
│   │   └── FigurinePedestal.cs     // Mesh swapping & upgrade logic for desk idols
│   └── ScriptableObjects/
│       ├── CountryData.cs          // Data definition for country lat, lon, and payouts
│       └── UpgradeConfig.cs        // Exponential cost curves for speed, auto-spin, durability
```

---

## 4. Key Script Responsibilities & Execution Details

### 4.1. `GlobeController.cs`
* Handles input via Unity’s `OnMouseDrag` / `InputAction` (Pointer Delta X).
* Adds positive angular speed to an internal float `currentVelocity`.
* Clamps `currentVelocity` between `autoSpinBaseline` and `maxTerminalVelocity`.
* Applies `currentVelocity` to `transform.Rotate(Vector3.up, currentVelocity * Time.deltaTime, Space.Self)`.
* Emits an event when local $Y$ rotation passes $360^\circ$ to credit the Base Revolution Bonus.

### 4.2. `MeridianTrigger.cs`
* Checks all active pins on the globe against the stationary wire vector $(0, 0, 1)$ or local angle threshold.
* Enforces `bool isLocked`:
  * Sets to `true` when within trigger threshold.
  * Resets to `false` when angular delta exceeds $30^\circ$ away from the wire.
* Calls `pinInstance.ConsumePass()` on the matching pin when struck.

### 4.3. `PinInstance.cs`
* Attached to each physical pin on the wall map.
* Holds reference to its corresponding `GlobePinMarker` on the 3D globe.
* When `ConsumePass()` is called:
  * Decrements `currentPasses`.
  * If `currentPasses > 0`: Triggers a wobble and tilt animation (e.g., via DOTween or manual quaternion slerp).
  * If `currentPasses <= 0`: Enables `Rigidbody` with gravity so the pin falls off the board, disables the globe marker, and unregisters itself from `YarnRenderer`.