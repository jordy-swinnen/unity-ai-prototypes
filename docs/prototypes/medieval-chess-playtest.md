# Medieval Chess: play-test sheet

For the "needs a person" criteria in `medieval-chess.md`. About 30 minutes. Open `Assets/Prototypes/MedievalChess/Scenes/MedievalChess.unity` in Unity and press Play. Headphones help. Write what you find in the last column and send it back, or tick a row if it is fine. A row you mark "no" becomes a task or an open question.

I (the agent) checked numbers, tests and renders. I could not play, listen or judge timing, so every row below is yours.

## 1. First look (2 minutes)
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Press Play, read the start screen | It is clear what to choose. The title and buttons read well. | | |
| Press BEGIN | The camera walks in from the hall to the board with a drum and a horn. It feels like arriving, and not like a glitch. | | |
| Look at the whole board | Medieval and heavy, not plain blocks. Warm torchlight at the edges, readable squares in the middle. | | |
| Look at each piece type, both sides | You can tell pawn, knight, bishop, rook, queen and king apart at a glance, and bone from cinnabar, including red pieces on the dark squares. Say which pair is hardest. | | |

## 2. Moving (5 minutes)
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Hover the mouse over squares | A gold ring follows it with a quiet tick. | | |
| Click a piece, then a gold ring | The move plays. No mis-click on a neighbouring square, even for tall pieces and from the far side (try Cinnabar). | | |
| Click an enemy piece you cannot take | A dull knock, nothing else happens. | | |
| Walk a pawn, leap a knight, slide a rook | Heavy, slow, with a thud and dust at each step. Not bouncy, not a toy. | | |
| Castle, and push a pawn to the last rank | Castling is a stride and a hop. Promotion shows four choices and the right piece appears with an anvil ring. | | |

## 3. Fights (8 minutes)
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Take a piece with each type: pawn, knight, bishop, rook, queen, king | The attacker steps up, swings, the blow lands with a drum and a clank, the victim falls apart in embers, the attacker takes the square. It reads as a kill. | | |
| Watch the camera on a capture | It pushes in and shakes on the blow, then returns. You never lose track of the position. | | |
| Judge the length (about 1.8 s at Full) | Not too long, not too silly. Say if it wears out by the tenth capture. | | |
| Switch to **Quick** and **Off** on the start screen | Quick is fast and still satisfying. Off is fast enough for a long game. | | |
| Let the computer capture your piece | The same fight, and then it is clearly your turn again. | | |

## 4. The three opponents (10 minutes)
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Play a few moves against **Squire** | Beatable. It misses a two-move trap now and then. It waits about a second, so you can follow its move. | | |
| Play against **Knight** | A real game. It takes free pieces and sees simple tactics. Say if it feels too weak (a fourth ply is one number). | | |
| Play against **Marshal** | Hard. It plays strongly, and never makes you wait much more than 3 seconds. | | |
| Watch the top bar while the computer thinks | "The Marshal is thinking..." shows, the game does not freeze. | | |

## 5. Endings (5 minutes)
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Give check (or be checked) | The king's square pulses red, a low horn sounds, a small shake. It is clear who is in check. | | |
| Play on to a checkmate. Squire blunders most, so it is the quickest. There is no shortcut to a set position in the game itself | Slow-motion fall, a fanfare, a warm wash, the result panel. Dramatic, not annoying. | | |
| Reach a draw, or press Resign | A draw: a bell, torches dim, the camera draws back. Resign: the king kneels and a low horn sounds. | | |
| Press Play again | A new game starts at once with the same opponent. | | |

## 6. Sound and comfort
| What to do | What "good" means | Fine? | Notes |
|---|---|---|---|
| Listen across a whole game | The sounds fit stone, wood and iron, and no sound is too loud or too quiet next to the others. Name any that is. | | |
| Look for anything uncomfortable | No flicker that tires the eyes, no shake that is too much. (A value of `effectStrength` below 1 on `Settings/MedievalChessSettings.asset` softens them. 0 turns the visual effects off.) | | |

## Decisions I need from you
- **Font.** The built-in font is plain. A medieval font needs a free licence recorded in the repository. Adding a font file needs your approval.
- **Missing feel rows.** Banner hover, a sway on an illegal click, a lift on a picked piece, a thinking candle, pieces lowering weapons in a draw, a fade between games. Which, if any, are worth adding?
- **Effect strength.** Today it is only a value on the settings asset. Do you want a control on the start screen?
- **The placeholder crate** (`Assets/Art/Exports/crate.glb`, `Tools/blender/make_crate.py`). `AGENTS.md` says to remove it once a prototype has real assets, and two now do. Deleting needs your yes.
- **GitHub Actions and the Unity secrets**, so the tests can run on pull requests.
