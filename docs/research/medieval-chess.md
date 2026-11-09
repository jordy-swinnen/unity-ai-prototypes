# Medieval Chess: research notes

Researched 9 November 2026 for `docs/prototypes/medieval-chess.md`, following the `game-spec` skill. General background is in `game-feel-and-look-development.md`.

Confidence tags: **[read]** read on the named page, **[secondary]** a search summary or a page I did not open, **[unverified]** not confirmed.

## Reference 1: the Lewis chessmen (real objects, 12th century)
Used for the shape language and the side colours. Not copied: the spec's pieces are original, simplified models.
- **[read]** Made of walrus ivory, with at least three of whale tooth, probably between 1150 and 1200, found on the Isle of Lewis in 1831. 79 chess pieces survive (8 kings, 8 queens, 16 bishops, 15 knights, 13 warders, 19 pawns). [Lewis chessmen](https://en.wikipedia.org/wiki/Lewis_chessmen), [Game pieces of the Lewis chessmen hoard](https://en.wikipedia.org/wiki/Game_pieces_of_the_Lewis_chessmen_hoard) (the second page was read verbatim only as far as the bishops; knight, warder and pawn details came through the fetch tool's summary).
- **[read]** Kings sit on a high-backed throne with a sheathed sword across the knees, with a crown of four trefoils. Queens sit with a hand cupped under the chin, wearing a veil and crown. Bishops hold a crozier and wear a mitre. Knights ride small ponies with spear and long kite shield. Warders (rooks) are standing warriors with helmet, sword and shield, and four of them are berserkers biting their shields. Pawns are plain, tombstone-like slabs. Sizes: most figures 7 to 10 cm.
- **[read]** The faces have bulging eyes and glum looks that modern viewers find comic. The article argues the makers meant strength, ferocity, or calm for the queens.
- **[read]** Some pieces were red when found (red stain, a study found traces of mercury consistent with cinnabar). The article suggests red and white may have told the two sides apart. The other source says none show colouring under close examination today.
- **What to take:** chunky, heavy, simple carved forms, strong silhouettes (throne, crozier, shield, pony), solemn faces, and the red-and-white side colours. **What not to copy:** the actual carved details of any piece. All models here are original.

## Reference 2: Battle Chess (1988)
Used for the idea of living pieces and for what to avoid.
- **[read]** Each capture is a short fight between the two pieces, shown in a close-up. The page counts 35 different battle animations (six piece types per colour, and no king takes or checks a king, so 35 pairings). Checkmate is also a fight. Examples: a rook turns into a rock monster and smashes a pawn, knight against knight echoes a film duel. [Battle Chess](https://en.wikipedia.org/wiki/Battle_Chess)
- **[read]** It could also be played in a 2D version without animations. The page does not say whether that was a toggle.
- **[read]** It used digitised sound effects for the battles. The page says one German magazine criticised the weak chess AI. It follows the full standard rules, which a review praised.
- **Not stated on the page:** how long a fight lasted, the camera, whether sound could be switched off, any difficulty levels or AI strength.
- **What to take:** the fight on every capture, and one distinct fight per attacker type. **What not to copy:** the characters, the animations, the film jokes, the sounds. **What to avoid:** forcing long fights on the player with no way to speed them up (spec: animation speed setting).

## Reference 3: heraldry (shapes and colour for readability)
- **[read]** Heraldry has two metals, or (gold) and argent (silver or white), and five colours: gules (red), azure (blue), vert (green), sable (black) and purpure (purple). The rule of tincture says metal should not sit on metal, and colour should not sit on colour, so a figure always stands out against its field. The page's reason is that arms should be easy to recognise. The page gives no hex values and says any shade is fine if it cannot be confused with another tincture. [Tincture (heraldry)](https://en.wikipedia.org/wiki/Tincture_(heraldry)), [Rule of tincture](https://en.wikipedia.org/wiki/Rule_of_tincture) (the first page was read verbatim for its first 39,000 of 172,000 characters; the rest was summarised).
- **What to take:** the contrast rule as a readability test (a light piece must not sit on a light square without an outline or shadow), and gold for highlights (legal moves). **What not to copy:** any real coat of arms. The banners and crests here are invented.

## Chess rules and engine facts
- **[read]** Perft counts for the initial position (`rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`): depth 1 = 20, 2 = 400, 3 = 8,902, 4 = 197,281, 5 = 4,865,609, 6 = 119,060,324. [Perft Results](https://www.chessprogramming.org/Perft_Results)
- **[read]** Kiwipete (`r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -`): depth 1 = 48, 2 = 2,039, 3 = 97,862, 4 = 4,085,603.
- **[read]** Position 3 (`8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1`): 14, 191, 2,812, 43,238. Position 4 (`r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1`): 6, 264, 9,467, 422,333. Position 5 (`rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8`): 44, 1,486, 62,379, 2,103,487. Position 6 (`r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10`): 46, 2,079, 89,890, 3,894,594. The page is a read-only recovery copy and the position 4 depth 4 row is garbled in its extra columns, but the node counts above are clear.
- **[read]** A well-known simple evaluation (Tomasz Michniewski's "Simplified Evaluation Function") uses piece values of pawn 100, knight 320, bishop 330, rook 500, queen 900 centipawns, plus piece-square tables that reward central knights and bishops, the seventh rank for rooks, a castled king in the middlegame and a central king in the endgame. The page admits it ignores pawn structure and passed pawns. [Simplified Evaluation Function](https://www.chessprogramming.org/Simplified_Evaluation_Function) The spec uses the ideas and the piece values, and writes its own tables, so nothing is copied.
- **[secondary]** Search (alpha-beta with iterative deepening, quiescence search, move ordering by captured value over attacker value, a transposition table) is the standard way to make a small engine. Chessprogramming wiki pages were seen in search results but not opened.
- **[unverified]** How strong a given search depth plays (Elo) in this implementation. The spec only requires that the three levels are ordered Squire, Knight, Marshal in a fixed test, and that a person finds them fair.
- **[unverified]** How long a depth 5 search takes in C# in the Unity Editor and in a build. The spec gives time caps, and the AI stops at the cap.

## Not found
- Any developer talk or postmortem on how Battle Chess's animations or camera were made.
- Timing for Battle Chess fights, or whether its sound could be turned off.
- A source for the strength (Elo) of a depth-limited alpha-beta engine in a game like this.
- A free-to-use medieval font to ship with the game. None was chosen. See the spec's open questions.
