-- Pip, the DioramaTurn character: 14 frames (idle 2, walk 4, hop 3, cheer 3, shake 2).
-- Run:  Tools/aseprite/run.sh DioramaTurn pip.lua
-- One base character map plus per-frame patches and small shifts, so a diff shows what changed.

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba
local colors = {
  clear = rgba(0, 0, 0, 0),
  ink   = rgba(59, 42, 77, 255),    -- #3B2A4D
  cream = rgba(255, 241, 214, 255), -- #FFF1D6
  scarf = rgba(255, 122, 89, 255),  -- #FF7A59
  violet = rgba(91, 75, 138, 255),  -- #5B4B8A
}
local legend = {
  ["."] = colors.clear,
  ["k"] = colors.ink,
  ["c"] = colors.cream,
  ["s"] = colors.scarf,
  ["v"] = colors.violet,
}

local base = {
  "................................", -- 1
  "................................", -- 2
  "............kkkkkkkk............", -- 3
  "..........kcccccccccck..........", -- 4
  ".........kcccccccccccck.........", -- 5
  "........kcvccccccccccvck........", -- 6
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
  "..........kssssssssssk..........", -- 17 scarf
  "...........kssssssssk...........", -- 18
  "...........kssssssssk...........", -- 19
  "...........kcccccssck...........", -- 20 knot
  "...........kcccccccck...........", -- 21
  "...........kcccccccck...........", -- 22
  "...........kcccccccck...........", -- 23
  "...........kcccccccck...........", -- 24
  "...........kcccccccck...........", -- 25
  "...........kcccccccck...........", -- 26
  "...........kkkkkkkkkk...........", -- 27
  "............kck..kck............", -- 28
  "............kck..kck............", -- 29
  "............kkk..kkk............", -- 30
  "................................", -- 31
  "................................", -- 32
}

local EYES_CLOSED = base[11]
local FEET_APART = "..........kck.....kck..........."
local FEET_APART_SOLE = "..........kkk.....kkk..........."
local FEET_PASSING = ".............kckkck............."
local FEET_PASSING_SOLE = ".............kkkkkk............."

local function shift_row(row, dx)
  if dx == 0 then return row end
  if dx > 0 then return string.rep(".", dx) .. row:sub(1, #row - dx) end
  return row:sub(1 - dx) .. string.rep(".", -dx)
end

local function shift_up(rows, amount)
  local out = {}
  for i = 1, #rows do
    out[i] = rows[i + amount] or string.rep(".", #rows[1])
  end
  return out
end

-- overrides: absolute row replacements. up: lift the body inside the frame. head: shift head rows.
local function frame(overrides, up, head)
  local rows = {}
  for i, row in ipairs(base) do rows[i] = row end
  if head and head ~= 0 then
    for i = 3, 20 do rows[i] = shift_row(rows[i], head) end
  end
  if up and up > 0 then rows = shift_up(rows, up) end
  if overrides then
    for i, row in pairs(overrides) do rows[i] = row end
  end
  return rows
end

local closed = { [9] = EYES_CLOSED, [10] = EYES_CLOSED }
local apart = { [28] = FEET_APART, [29] = FEET_APART, [30] = FEET_APART_SOLE }
local passing = { [28] = FEET_PASSING, [29] = FEET_PASSING, [30] = FEET_PASSING_SOLE }

local function merge(a, b)
  local out = {}
  if a then for k, v in pairs(a) do out[k] = v end end
  if b then for k, v in pairs(b) do out[k] = v end end
  return out
end

local frames = {
  frame(),                    -- 1 idle A
  frame(closed),              -- 2 idle B (blink)
  frame(apart),               -- 3 walk A feet apart
  frame(),                    -- 4 walk B feet together
  frame(apart),               -- 5 walk C feet apart
  frame(passing),             -- 6 walk D feet passing
  frame(merge(closed, apart)),-- 7 hop A crouch
  frame(nil, 1),              -- 8 hop B air
  frame(merge(closed, apart)),-- 9 hop C landing
  frame(closed),              -- 10 cheer A
  frame(closed, 1),           -- 11 cheer B bounce
  frame(closed),              -- 12 cheer C
  frame(nil, 0, -1),          -- 13 shake A head left
  frame(nil, 0, 1),           -- 14 shake B head right
}

local durations = {
  0.45, 0.45,
  0.11, 0.11, 0.11, 0.11,
  0.12, 0.12, 0.12,
  0.14, 0.14, 0.14,
  0.1, 0.1,
}

local sprite = Sprite(32, 32)
for index, rows in ipairs(frames) do
  assert(#rows == 32, string.format("frame %d has %d rows", index, #rows))
  local image = Image(32, 32)
  for y, row in ipairs(rows) do
    assert(#row == 32, string.format("frame %d row %d has %d characters", index, y, #row))
    for x = 1, #row do
      local ch = row:sub(x, x)
      local color = legend[ch]
      assert(color ~= nil, string.format("unknown character %q in frame %d at row %d, column %d", ch, index, y, x))
      image:drawPixel(x - 1, y - 1, color)
    end
  end
  if index > 1 then sprite:newEmptyFrame(index) end
  if index == 1 then sprite.cels[1].image = image else sprite:newCel(sprite.layers[1], index, image, Point(0, 0)) end
  sprite.frames[index].duration = durations[index]
end

local function tag(name, from, to)
  local t = sprite:newTag()
  t.name = name
  t.fromFrame = sprite.frames[from]
  t.toFrame = sprite.frames[to]
end

tag("idle", 1, 2)
tag("walk", 3, 6)
tag("hop", 7, 9)
tag("cheer", 10, 12)
tag("shake", 13, 14)

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
