# Basketball

A first-person basketball prototype focused on responsive movement, satisfying
ball physics, and expressive arcade-style shooting.

The project combines physics-based basketball interactions with animated arms,
charged throws, mid-air slow motion, momentum transfer, ball attraction, and
visual feedback for successful shots.

## Gameplay

Explore the completed basketball court, collect loose balls, charge throws, and
score by sending the ball through the hoop from above. The hoop checker uses two
trigger zones to distinguish a valid top-to-bottom basket from a reverse
bottom-to-top pass.

## Controls

| Input | Action |
| --- | --- |
| `WASD` | Move |
| Mouse | Look |
| `Space` | Jump |
| Hold `Space` in the air | Activate slow motion |
| Left click near a targeted ball | Pick up ball |
| Hold left click with a ball | Charge throw |
| Release left click | Throw ball |
| Hold right click while targeting a ball | Magnetically attract ball |
| `Escape` | Unlock cursor and Pause |

## Gameplay Feel Improvements

1. **Complete surroundings**

   A finished court environment provides clear boundaries and a complete play
   space.

2. **No-bounce hoop physics**

   The hoop uses a non-bouncy physics material for predictable rim contacts.

3. **Bouncy ball physics**

   Basketballs use a highly elastic physics material for energetic rebounds.

4. **Medium-bounce environment**

   The floor and surrounding objects provide controlled, moderate ball bounce.

5. **Animated first-person arms**

   Pickup, reach, charge, calling, and shooting states are represented through
   first-person arm animations.

6. **Hold-to-charge throwing**

   Holding left click charges a throw up to a configurable maximum duration.
   Charge increases forward velocity more strongly than upward velocity.

7. **Ball trail effects**

   Fast-moving basketballs enable a Trail Renderer, which turns off as the ball
   slows and clears when picked up.

8. **Basket particle effects**

   A successful top-to-bottom score triggers particles at the hoop.

9. **Magnetic ball attraction**

   Holding right click pulls a targeted loose ball toward the player and
   automatically picks it up once it enters reach.

10. **Charge FOV feedback**

    Charging smoothly narrows the Cinemachine field of view, then eases it back
    after release.

11. **Mid-air throw floatiness**

    Gravity is temporarily reduced while the player is descending and waiting
    for the ball to leave the hand.

12. **Jump buffer**

    Jump input is remembered briefly, reducing missed jumps between input and
    physics updates.

13. **Player momentum transfer**

    A configurable portion of the player's running, jumping, or falling velocity
    is inherited by the thrown ball.

14. **Mid-air slow motion**

    Holding jump while airborne smoothly slows time. Releasing jump or landing
    smoothly restores normal speed.

15. **SFX**

   random sound effect volumes, collision strength modifier, and other sound effect modifiers (mute collisions with other balls and player).

16. **Backspin**

   Ball has added backspin rotation on throw, making it more realistic and easier to get successful shots.

## Additional Systems

- Forgiving screen-space ball targeting around the crosshair
- Automatic ball attachment to an animated hand anchor
- Camera-angle influence on throw height
- Delayed ball release synchronized with the shooting animation
- Per-ball scoring and reverse-pass detection
- Animator trigger resets to prevent state desynchronization
- Debug gizmos for pickup and magnet ranges

## Technology

- Unity `6000.0.58f2`
- Universal Render Pipeline `17.0.4`
- Cinemachine `3.1.7`
- Unity Input System package with legacy input handling enabled

## Running the Project

1. Open the repository with Unity `6000.0.58f2` or a compatible Unity 6
   version.
2. Allow Unity to restore the packages listed in `Packages/manifest.json`.
3. Open `Assets/Scenes/Gameplay.unity`.
4. Enter Play Mode.

## Main Scripts

- `PlayerController.cs` handles movement, jumping, jump buffering, airborne
  slow motion, and throw hang time.
- `PlayerBallHandler.cs` handles targeting, pickup, magnetic attraction,
  charging, animation state, camera feedback, and throwing.
- `Ball.cs` handles held/free physics states, attraction, trails, and release.
- `HoopPassChecker.cs` validates scoring direction and plays score feedback.
- `HoopPassTrigger.cs` identifies the upper and lower hoop trigger zones.
