# WICKFALL — Art Prompt Pack (Gemini / ChatGPT)

Every prompt is in English (image models follow English best).
Sizes and names come from the design doc (`Wickfall_Idee_Complete.pdf`).

---

## 0. How to use this (read once)

1. **Always paste a style block first**, then the asset prompt, in the same message.
   - Use **SPRITE STYLE** for characters, items, tiles, UI and effects.
   - Use **SCENE STYLE** for concept art and mood pictures (full images, not game assets).
2. **Make the reference images first** (section 1). After that, **attach them to every new
   prompt** and start with: *"Match the attached reference exactly: same proportions, palette
   and outline style."* This is the only way to keep the character and world consistent.
3. **One asset per request.** AI models break down when asked for big sprite sheets with many
   animations. Ask for one animation row, or one tile group, at a time.
4. **Background:**
   - ChatGPT can give a real transparent background: replace the magenta line with
     *"transparent background, PNG"*.
   - Gemini usually can't, so keep the flat magenta (`#FF00FF`) and remove it in Aseprite
     with the Magic Wand (contiguous off).
5. **The AI does not draw real pixel art.** It draws big images that *look* like pixel art.
   Clean every asset in Aseprite:
   - Resize down with **Nearest Neighbor** until one "fake pixel" equals one real pixel
     (for example, the player should end up 32×40).
   - Reduce colors to your palette (Sprite → Color Mode → Indexed).
   - Fix stray pixels and line the frames up on the grid.
6. **No text in images.** AI misspells words (the resources sheet had "HIGHLIGHED"). Make buttons
   and panels empty, and put the text on them in Unity with a pixel font.
7. **Unity import settings** for every sprite: Texture Type *Sprite (2D and UI)*,
   **Pixels Per Unit 16**, **Filter Mode Point (no filter)**, **Compression None**.
   Slice sheets with the Sprite Editor (Grid By Cell Size).

---

## STYLE BLOCKS (paste one before every prompt)

### SPRITE STYLE
```
16-bit pixel art game asset for a 2D top-down action RPG (3/4 top-down view, like Death's Door or Hyper Light Drifter). Dark, melancholic gothic world made of candles, wax and ash. Limited palette: charcoal black, bone white, desaturated ochre and grey-brown; ONLY flames and light sources are saturated and warm. Crisp square pixels clearly visible, clean 1-pixel dark outlines, no anti-aliasing, no blur, no soft gradients (dithering only), no text, no letters, no watermark. Light comes from the top-left. Each sprite is centered in its own cell on an even grid, with nothing cropped. Flat solid pure magenta #FF00FF background.
```

### SCENE STYLE
```
16-bit pixel art scene for a 2D top-down action RPG (3/4 top-down view, like Death's Door or Hyper Light Drifter). Dark, melancholic gothic world made of candles, wax and ash. Limited palette: charcoal black, bone white, desaturated ochre and grey-brown; ONLY flames and light sources are saturated and warm, glowing out of deep darkness. Crisp square pixels, no anti-aliasing, no text, no watermark. 16:9 composition.
```

---

## 1. Reference images (make these FIRST)

### 1.1 World style sheet
```
A style guide sheet for the game world: a row of 8 color swatches (charcoal black, dark grey, ash grey, bone white, pale cream wax, desaturated ochre, warm amber flame, bright yellow flame core), then small examples of: a stone floor tile, a hardened wax floor tile, a lit candle, a dripping wax puddle, an unlit iron brazier, and a black shadow creature. Everything in one consistent style.
```

### 1.2 Player character turnaround (the most important image)
```
Character reference sheet for the hero, "the Wickbearer": a small candle-golem, 32x40 pixels, body made of pale cream wax with drips running down its shoulders and arms, short stubby legs, no mouth, two small dark hollow eyes, a glowing amber flame burning from the wick on top of its head. Melancholic but determined, small and fragile against a huge world. Show it 4 times in a row, standing still: facing down (toward camera), facing up (back), facing left, facing right.
```
Save this one and **attach it to every player prompt below**.

---

## 2. The player (attach the turnaround)

All prompts below start with: *"Same candle-golem as the attached reference, 32x40 pixels."*

**Idle (4 directions)**
```
Idle breathing animation, 4 frames per direction, the flame gently flickering. 4 rows: down, up, left, right.
```

**Walk (one direction per request; do down, up, left, right)**
```
Walk cycle facing DOWN, 6 frames in a single row, small bouncy steps, wax drips swaying, flame leaning slightly opposite to movement.
```

**Dim state ("trimming the wick")**
```
Dim stealth state: the flame is a tiny tight blue-white ember, the body hunched and crouched low, the wax darker and less lit. Idle 4 frames + sneaking walk 6 frames, facing down.
```

**Weak state (low wax)**
```
Low-health state: the golem has visibly melted down to 2/3 of its height, wax pooling at its feet, the flame small and shaky. Idle 4 frames facing down.
```

**Attack (amber flame)**
```
Melee attack facing RIGHT: the golem swings a sword-shaped arc of amber fire from its hand. 5 frames: wind-up, swing start, full arc of fire, follow-through, recover.
```

**Dash**
```
Dash facing RIGHT: 3 frames with squash-and-stretch, the body stretched horizontally, leaving a short trail of hot wax drops behind.
```

**Hurt**
```
Hurt reaction facing down: 2 frames, body flinching back, wax chunks flying off, the flame flaring out sideways.
```

**Death (extinguished)**
```
Death animation facing down, 12 frames in two rows of 6: the flame sputters and goes out with a thin smoke wisp, then the golem slowly collapses and melts into a flat puddle of wax with the black wick lying on top.
```

**Flame swap**
```
Flame swap: 6 frames, the golem raises its head, the flame bursts upward in a bright flare and changes color from amber to blue.
```

**Rest at a brazier**
```
Sitting animation: the golem sits down cross-legged facing up, toward a brazier off-screen, 4 frames of calm idle breathing while sitting.
```

---

## 3. The five flames

**Flame icons (HUD, 16x16 each)**
```
Five 16x16 flame icons in a single row, each with a different SILHOUETTE so they read without color: AMBER round flame, BLUE teardrop flame, GREEN leaf-shaped flame, WHITE four-point star flame, CRIMSON jagged spiky flame. Glowing bright core, dark 1-pixel outline.
```

**Head flames on the player (one row per color)**
```
The flame on top of the candle-golem's head in 5 color variants, 4-frame flicker loop each, one row per color: amber, blue, green, white, crimson. Same 16x16 cell size for every frame.
```

**Attack per flame (one request each)**
- **Amber:** `Wide melee arc of warm amber fire, 5 frames, 48x32, sweeping left to right.`
- **Blue:** `Ranged projectile: a blue teardrop fireball 16x16 with a frosty trail, 4-frame flying loop, plus a 5-frame impact burst of blue sparks and ice-like shards.`
- **Green:** `Poison flame attack: a green fire burst that leaves a bubbling green-flame puddle on the ground. Burst 5 frames 48x48, puddle loop 4 frames 32x16.`
- **White:** `Holy anti-shadow attack: a blinding white star-shaped flash expanding in a ring, 6 frames 64x64, with a thin bone-white outline, very bright.`
- **Crimson:** `Aggressive crimson flame slash, jagged and violent, with flying ember sparks, 5 frames 48x32.`

---

## 4. Environments (one section per region)

For each region, do these requests **in order**:
- **A. Concept scene** (SCENE STYLE): your mood reference for the region.
- **B. Ground tiles**
- **C. Wall and cliff tiles**
- **D. Props**
- **E. Hazard**
- **F. Enemies**

For tiles, always add this line:
```
Tileset on a strict 16x16 pixel grid, seamless tiles that repeat without visible seams, include variations, edges and inner/outer corners, laid out as a clean tile sheet.
```

### Region 1: Champs de Suif (Tallow Fields)
**A. Concept**
```
Top-down view of a vast plain made of hardened pale wax, gentle hills of melted candle drips, fields of short wax grass, a few half-melted giant candles as ruins, faint ambient light, small glowing insects. Calm, lonely, beginning of a journey. A tiny candle-golem with an amber flame in the middle.
```
**B. Ground tiles**
```
Cream hardened wax ground: plain tiles, cracked tiles, smooth puddle tiles, small wax-grass tufts (2-frame sway), transition tiles from wax to dark dirt.
```
**C. Walls**
```
Cliff and wall tiles made of thick layered melted wax, drips running down the face, top edge, side edges, inner and outer corners.
```
**D. Props**
```
Props: 3 half-melted giant candle ruins (different sizes, 32x48 to 48x64), small wax rocks, a dead wick-tree, wax mushrooms, a broken wooden fence.
```
**E. Hazard**
```
Wax geyser: a hole in the wax ground, 32x32. Animation: 6 frames of warning bubbling (glowing orange), then 6 frames of a boiling wax eruption shooting up, then 4 frames calming down.
```
**F. Enemies**
```
Dripling: slow 24x24 melee creature, a hunched blob of dripping pale wax with two dark empty eye holes and stubby arms. Idle 4 frames, crawl 6 frames, attack (lunge) 5 frames, death (melts) 6 frames.
```
```
Waxfly ("Cirelle"): tiny 8x8 harmless glowing insect made of wax with a tiny warm light. 4-frame flying loop.
```

### Region 2: Bois-Mèche (Wickwood)
**A. Concept**
```
Top-down view of a pitch-black forest of giant trees whose trunks are made of twisted black candle wicks, a thick canopy blocking almost all light, only a few pale beams reaching the ground, hanging climbable vines made of wick string, dark flowers with pale glowing cores. Oppressive and silent.
```
**B. Ground tiles**
```
Dark forest soil with roots, dead leaves, moss, small stones, a dirt path; transition tiles between path and soil.
```
**C. Walls**
```
Forest edge walls made of huge black wick-tree trunks and tangled roots, plus climbable hanging wick-vine tiles (vertical, repeatable), corners and edges.
```
**D. Props**
```
Props: giant black wick-tree tops seen from above (canopy, 64x64, semi-transparent darker shape), fallen logs, thorn bushes, pale glowing mushrooms, an old stone shrine.
```
**E. Hazard**
```
Light-Eater Bloom: a 32x32 dark flower with a pale glowing core. Idle pulse 4 frames, then a "feeding" animation 6 frames where it opens wide and sucks in light particles.
```
**F. Enemies**
```
Husk: 32x32 hollow creature made of dead bark and hardened wax, hanging upside down from a branch in ambush pose. Hanging idle 4 frames, drop-down attack 6 frames, walk 6 frames, death 6 frames (crumbles into bark chips).
```
```
Lurker: 24x24 low crouching shadow creature with long thin limbs and small white pinpoint eyes. Frozen pose (1 frame), slow crawl 6 frames, fast pounce 5 frames, death 6 frames (dissolves into black smoke).
```

### Region 3: Fonderies de Suie (Soot Foundries)
**A. Concept**
```
Top-down view of a huge abandoned candle foundry: rusted metal floors, conveyor belts carrying wax blocks, giant hydraulic presses, flickering furnaces glowing orange, soot everywhere, steam vents, chains. Industrial and hostile.
```
**B. Ground tiles**
```
Rusted metal grating floor, riveted steel plates, soot-covered stone, hazard stripes on the floor; conveyor belt tiles with 4-frame animation in all 4 directions, plus belt ends.
```
**C. Walls**
```
Industrial walls: dark brick with metal pipes, furnace walls with glowing slits, corners and edges.
```
**D. Props**
```
Props: furnace 48x48 (4-frame flicker glow), stacked wax blocks, molds, metal barrels, chains, cauldron of molten wax, broken machinery.
```
**E. Hazards**
```
Hydraulic press seen from above, 32x32: idle raised, 4-frame warning (shaking, red light), 3-frame slam down, 4-frame rise back up. Separate: steam vent in the floor, 6-frame burst of white steam.
```
**F. Enemies**
```
Slagborn: 48x48 heavy armored golem made of rusted metal and cooled black slag, glowing orange cracks, slow and massive. Idle 4 frames, slow walk 6 frames, overhead smash attack 7 frames, death 8 frames (collapses into slag).
```
```
Bellows: 32x32 ranged machine creature with an accordion leather body and a metal nozzle. Idle 4 frames, attack 6 frames (inflates then blows). Projectile: 8x8 black soot cloud 4-frame loop, plus 5-frame soot hit splash.
```

### Region 4: Cathédrale de Verre (Glass Cathedral)
**A. Concept**
```
Top-down view of a ruined gothic cathedral where everything is made of black mirror glass: shining black floors reflecting light, crystal pillars, a giant broken rose window on the floor made of colored glass panels, thin beams of light bouncing between mirrors. Beautiful, cold and dangerous.
```
**B. Ground tiles**
```
Mirror-black glass floor with subtle pale reflections, cracked glass tiles, broken glass hole tiles (falling into darkness), crystal edge tiles, stained glass floor tiles (5 colors: amber, blue, green, white, crimson).
```
**C. Walls**
```
Gothic cathedral walls of black glass and dark stone, pointed arches, crystal pillars, corners and edges.
```
**D. Props**
```
Props: crystal pedestal holding a rotatable mirror (4 rotation directions), broken pews, crystal shards, tall candelabras, a large stained glass rose panel 64x64.
```
**E. Hazard**
```
Light laser beam: a straight beam segment 16x16 that tiles horizontally and vertically, 4-frame shimmer, plus beam start and beam end impact pieces. Bright white with a warm core.
```
**F. Enemy**
```
Reflection: a mirror-black copy of the candle-golem (same 32x40 size and shape as the attached player reference), pure glossy black with pale white edge highlights, its flame is a black flame with a white outline. Glitchy idle 4 frames, walk 6 frames, attack 5 frames, shattering death 8 frames.
```

### Region 5: Creux Profond (The Deep Hollow)
**A. Concept**
```
Top-down view of total darkness. The only visible thing is a small circle of warm light around a tiny candle-golem, revealing a strange ground of pale bones, ash and faint carved tiles. At the edge of the light, shapeless black silhouettes with faint white outlines drift closer. A trail of glowing wax drops behind the golem.
```
**B. Ground tiles**
```
Very dark ground tiles that are almost pure black, with only faint pale outlines, cracks, carved runes and scattered small bones visible. Designed to be seen only inside a circle of light.
```
**C. Walls**
```
Cave walls of pure black rock with faint pale edge outlines only, corners and edges.
```
**D. Props**
```
Props: glowing wax drop on the ground 8x8 (4-frame soft glow), old extinguished candles, bone piles, a dead giant hand made of stone, a faint glowing wall rune.
```
**F. Enemy**
```
The Unlit: shapeless pure black silhouette 32x32, like a hole cut in the world shaped like something that was once lit (a vague person or animal), with a faint white 1-pixel outline. Drifting animation 6 frames, attack 5 frames (stretches toward the viewer), fade death 6 frames.
```

### Region 6: Chambre du Foyer (Hearth Chamber)
**A. Concept**
```
Top-down view of a colossal cathedral made of grey ash, ash drifts on the floor, giant ruined pillars, and in the center the Great Hearth: a huge stone fireplace burning with "negative light", a flame made of pure black with a white outline that darkens everything around it. Final, solemn, epic.
```
**B. Ground tiles**
```
Grey ash ground, deep ash drifts (slowing zones), scattered cinders, ancient carved stone floor plates, transition tiles between ash and stone.
```
**C. Walls**
```
Huge ash-stone cathedral walls with collapsed arches, corners and edges.
```
**D. Props**
```
The Great Hearth 96x96: a giant stone fireplace with a black negative flame, 6-frame flicker. Also: broken giant pillars, ash piles, a throne of melted candles.
```

---

## 5. Boss: the Hollow King

**Reference first**
```
Character reference for the final boss "The Hollow King", 128x160 pixels: a tall gaunt crowned figure made of pure black negative space with a faint white outline, a flame-shaped hole in its chest showing the background through it, a tattered cloak that dissolves into black smoke at the bottom, a crown of melted black candles. Facing down, standing still.
```
Then attach it and request one row at a time:
- `Idle, 6 frames, the cloak drifting, the crown flickering with black flames.`
- `Slam attack, 6 frames: raises both arms, slams the ground, shockwave of ash.`
- `Sweep attack, 6 frames: a wide horizontal swipe with a long arm of black smoke.`
- `Phase 2 variant: the crown is cracked and half of it is missing, the chest hole is bigger and leaks white light. Idle 6 frames.`
- `Phase 3 variant: the crown is shattered, the body is breaking apart into floating black fragments, desperate and violent. Idle 6 frames.`
- `Death, 10 frames: the chest hole fills with warm light and the black body cracks and fades away into ash.`

---

## 6. Objects and world items

**Brazier (checkpoint)**
```
Iron brazier 32x32 on a small stone base. Unlit version (cold, grey ash inside). Lit version with a 4-frame warm fire loop. A 6-frame lighting-up animation between them.
```

**Wax pool (refill)**
```
Wax pool in the ground 32x32: full (glowing warm melted wax, 4-frame shimmer) and empty (dry cracked basin).
```

**Wax pickups and wax shards (currency)**
```
Small wax drops dropped by enemies: 3 sizes (8x8, 12x12, 16x16), soft glow, 4-frame bob. Wax shard currency: a translucent amber crystal shard 16x16, 4-frame sparkle.
```

**The 6 Ember Relics (16x16 icons + 32x32 world version)**
```
Six glowing relic items, one per cell, each a different shape: (1) a warm tallow seed, (2) a hook made of braided wick, (3) a small leather bellows lung, (4) a prism shard of black glass, (5) a lantern shaped like an eye, (6) an ancient hearth key. Each with a soft amber glow and dark outline.
```
(These six are suggestions. Rename them to match whatever each relic ends up doing.)

**Landmarks and doors**
```
Great Bell hanging from a wooden frame 48x64, 4-frame ringing animation with sound-wave rings.
```
```
The Broken Wick: a huge broken candle stump landmark 64x160, ancient, covered in drips, the wick snapped and burnt.
```
```
Locked iron gate door 48x32 in a stone wall: closed, 4-frame opening, open.
```
```
Breakable objects: wooden crates, clay pots, candle racks. Each intact plus a 4-frame breaking animation.
```

---

## 7. Interface (NO text — add text in Unity)

**Main menu background**
Use SCENE STYLE, and attach `Main menu.jpg` as reference:
```
Main menu background, 16:9: a dark gothic hall, stained glass window, banners, torches on the walls, and on the right a huge old wax golem sitting on a throne covered in lit candles. Leave the center and left side empty and dark for the logo and menu. No text, no logo, no buttons.
```

**Logo**
```
Game logo for the word "WICKFALL" in heavy gothic blackletter pixel letters, bone white with dark outlines, small dripping wax on the letters, a lit candle on each side. Transparent background.
```
(Logos are the one place you need text. Check the spelling, and redraw the letters in Aseprite if needed.)

**Buttons and panels**
```
UI kit, EMPTY with no text: a rectangular button frame 96x24 in 3 states (normal: dark charcoal with grey stone border; highlighted: warm amber border glow; pressed: darker and pushed in). A large 9-slice panel frame in bone white on charcoal with small ornate corners. A small square inventory slot 24x24 (empty and selected). A scrollbar.
```

**HUD**
```
HUD wax bar drawn as a vertical candle that melts down, 8 states from full (tall candle, big flame) to almost empty (tiny stub, tiny flame), same width, aligned at the bottom.
```
```
Radial flame wheel: a circular dark stone ring 96x96 with 5 empty slots around it and a highlighted slot glow, for selecting flames. No icons inside.
```
```
Small HUD icons 16x16: dash charge (full and empty), wax shard counter icon, relic slot (empty and filled), pact icon (a black crown).
```

**Interaction prompt and dialog box**
```
Small floating interaction marker 16x16 above objects (glowing ember diamond, 4-frame bob). A dialog box frame 9-slice in charcoal with bone white border, empty.
```

---

## 8. Visual effects (8 frames each, one request per line)

- `Ember trail particles behind a moving flame, small orange sparks fading out, 8 frames 16x16.`
- `Wax drip hitting the ground and splashing, 8 frames 16x16.`
- `Extinguish burst: radial puff of dark smoke and dying sparks, 8 frames 48x48.`
- `Flare burst upward when swapping flames, 8 frames 32x48, warm and bright.`
- `Hit spark when an attack connects, white and amber star burst, 6 frames 24x24.`
- `Enemy death dissolve into black smoke, 8 frames 32x32.`
- `Brazier lighting-up burst with rising sparks, 8 frames 48x48.`
- `Ash particles slowly drifting, 8 frames 8x8 loop.`
- `Relic pickup: rising golden light pillar, 8 frames 32x64.`
- `Soot hit splash, black and grey burst, 8 frames 24x24.`

---

## 9. Suggested order of work

1. Style sheet (1.1), player turnaround (1.2), SPRITE STYLE check.
2. Player idle and walk, plus Region 1 ground tiles: enough to build a first playable room.
3. Flame icons, HUD candle bar, brazier, wax drops.
4. Region 1 enemies and geyser.
5. Then one region at a time, and the boss last.
