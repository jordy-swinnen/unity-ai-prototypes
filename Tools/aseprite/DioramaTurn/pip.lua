-- Pip, the DioramaTurn character: a first three-frame sheet (idle, walk A, walk B) that proves
-- the sprite path end to end. The final 14-frame Pip comes with the polish stage.
-- Run:  Tools/aseprite/run.sh DioramaTurn pip.lua
-- Palette: the spec's shared 16-colour set (Pip cream, scarf, ink, shadow violet).
-- The drawing is one base character map plus small per-frame patches, so a diff shows what changed.

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba

local colors = {
  clear = rgba(0, 0, 0, 0),
  ink   = rgba(59, 42, 77, 255),    -- #3B2A4D outlines and eyes
  cream = rgba(255, 241, 214, 255), -- #FFF1D6 Pip's body
  scarf = rgba(255, 122, 89, 255),  -- #FF7A59 the scarf
  violet = rgba(91, 75, 138, 255),  -- #5B4B8A cheeks and shade
}

local legend = {
  ["."] = colors.clear,
  ["k"] = colors.ink,
  ["c"] = colors.cream,
  ["s"] = colors.scarf,
  ["v"] = colors.violet,
}

-- 32 x 32. Rows are y, top to bottom. Big round head, scarf collar, small body, two feet.
-- Every row is exactly 32 characters (the asserts below check it).
local base = {
  "................................", -- 1
  "................................", -- 2
  "............kkkkkkkk............", -- 3  head top
  "..........kcccccccccck..........", -- 4
  ".........kcccccccccccck.........", -- 5
  "........kcvccccccccccvck........", -- 6  cheeks
  "........kcccccccccccccck........", -- 7
  "........kcccccccccccccck........", -- 8
  ".......kccckkcccccckkccck.......", -- 9  eyes
  ".......kccckkcccccckkccck.......", -- 10 eyes
  ".......kcccccccccccccccck.......", -- 11
  ".......kcccccccccccccccck.......", -- 12
  ".......kcccccccccccccccck.......", -- 13
  ".......kcccccccccccccccck.......", -- 14
  "........kcccccccccccccck........", -- 15
  ".........kcccccccccccck.........", -- 16
  "..........kssssssssssk..........", -- 17 scarf collar
  "...........kssssssssk...........", -- 18
  "...........kssssssssk...........", -- 19
  "...........kcccccssck...........", -- 20 body, knot hangs right
  "...........kcccccccck...........", -- 21
  "...........kcccccccck...........", -- 22
  "...........kcccccccck...........", -- 23
  "...........kcccccccck...........", -- 24
  "...........kcccccccck...........", -- 25
  "...........kcccccccck...........", -- 26
  "...........kkkkkkkkkk...........", -- 27 body bottom
  "............kck..kck............", -- 28 feet together
  "............kck..kck............", -- 29
  "............kkk..kkk............", -- 30 soles
  "................................", -- 31
  "................................", -- 32
}

-- Frame 2 (walk A): feet apart. Frame 3 (walk B): feet passing, close together.
local patches = {
  [2] = {
    [28] = "..........kck.....kck...........",
    [29] = "..........kck.....kck...........",
    [30] = "..........kkk.....kkk...........",
  },
  [3] = {
    [28] = ".............kckkck.............",
    [29] = ".............kckkck.............",
    [30] = ".............kkkkkk.............",
  },
}

local size = 32
local frames = { base, base, base }

local sprite = Sprite(size, size)
local frame_durations = { 0.5, 0.11, 0.11 }

for index, rows in ipairs(frames) do
  local patched = {}
  for y, row in ipairs(rows) do
    patched[y] = (patches[index] and patches[index][y]) or row
  end
  assert(#patched == size, string.format("frame %d has %d rows, expected %d", index, #patched, size))
  local image = Image(size, size)
  for y, row in ipairs(patched) do
    assert(#row == size, string.format("frame %d row %d has %d characters, expected %d", index, y, #row, size))
    for x = 1, #row do
      local ch = row:sub(x, x)
      local color = legend[ch]
      assert(color ~= nil, string.format("unknown character %q in frame %d at row %d, column %d", ch, index, y, x))
      image:drawPixel(x - 1, y - 1, color)
    end
  end
  if index > 1 then
    sprite:newEmptyFrame(index)
  end
  if index == 1 then
    sprite.cels[1].image = image
  else
    sprite:newCel(sprite.layers[1], index, image, Point(0, 0))
  end
  sprite.frames[index].duration = frame_durations[index]
end

-- Tags, so the game can find its animations in the sheet's JSON (this is also the tag-export spike).
local idle = sprite:newTag()
idle.name = "idle"
idle.fromFrame = sprite.frames[1]
idle.toFrame = sprite.frames[1]
local walk = sprite:newTag()
walk.name = "walk"
walk.fromFrame = sprite.frames[2]
walk.toFrame = sprite.frames[3]

sprite:saveAs(src_dir .. "/pip.aseprite")
print("wrote " .. src_dir .. "/pip.aseprite")

app.sprite = sprite
app.command.ExportSpriteSheet{
  ui = false,
  askOverwrite = false,
  type = SpriteSheetType.HORIZONTAL,
  textureFilename = out_dir .. "/pip_sheet.png",
  dataFilename = out_dir .. "/pip_sheet.json",
  dataFormat = SpriteSheetDataFormat.JSON_ARRAY,
}
print("wrote " .. out_dir .. "/pip_sheet.png")
print("wrote " .. out_dir .. "/pip_sheet.json")
