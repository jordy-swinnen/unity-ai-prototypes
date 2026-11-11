-- PixelInvaders alien ranks (Scout, Guard, Heavy) and UFO saucer: 12 frames (16x16 each).
-- Run:  Tools/aseprite/run.sh PixelInvaders invaders.lua

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba
local palette = {
  ["."] = rgba(0, 0, 0, 0),       -- 0 Transparent
  ["k"] = rgba(9, 12, 18, 255),   -- 1 Void black #090C12
  ["d"] = rgba(22, 30, 43, 255),  -- 2 Trench steel #161E2B
  ["s"] = rgba(44, 57, 75, 255),  -- 3 Hull slate #2C394B
  ["h"] = rgba(99, 118, 141, 255),-- 4 Highlight steel #63768D
  ["w"] = rgba(240, 246, 255, 255),-- 5 Phosphor white #F0F6FF
  ["a"] = rgba(217, 130, 43, 255),-- 6 Amber grid #D9822B
  ["y"] = rgba(255, 209, 71, 255),-- 7 Solar gold #FFD147
  ["c"] = rgba(44, 232, 245, 255),-- 8 Cyan turret #2CE8F5
  ["b"] = rgba(27, 118, 181, 255),-- 9 Deep cobalt #1B76B5
  ["g"] = rgba(56, 217, 115, 255),-- 10 Bunker emerald #38D973
  ["m"] = rgba(30, 130, 76, 255), -- 11 Bunker moss #1E824C
  ["v"] = rgba(184, 92, 255, 255),-- 12 Guard violet #B85CFF
  ["p"] = rgba(107, 47, 163, 255),-- 13 Guard plum #6B2FA3
  ["r"] = rgba(255, 71, 105, 255),-- 14 Scout coral #FF4769
  ["e"] = rgba(168, 29, 57, 255), -- 15 Crimson ember #A81D39
}

local function to_flash(rows)
  local out = {}
  for i, row in ipairs(rows) do
    local s = {}
    for x = 1, #row do
      local ch = row:sub(x, x)
      if ch == "." or ch == "k" then
        s[x] = ch
      else
        s[x] = "w"
      end
    end
    out[i] = table.concat(s)
  end
  return out
end

-- Scout (top rank, 30 pts): narrow diamond squid with twin antennae
local scout_a = {
  "....kk....kk....", -- 1
  "....kyk..kyk....", -- 2
  ".....krrrrk.....", -- 3
  "....krrrrrrk....", -- 4
  "...krwkrrrrkwrk.", -- 5
  "..krrwkrrrrkwrrk", -- 6
  "..krrrrrrrrrrrrk", -- 7
  "..krrerrrrrrerrk", -- 8
  "...keeeeeeeeeek.", -- 9
  "....krkkrkkrk...", -- 10
  "...krk.krk.krk..", -- 11
  "...kek.kek.kek..", -- 12
  "....kk..kk..kk..", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}
scout_a[5]  = "...krwkrrkwrk..."
scout_a[6]  = "..krrwkrrkwrrk.."
scout_a[7]  = "..krrrrrrrrrrk.."
scout_a[8]  = "..krrerrrrerrk.."
scout_a[9]  = "...keeeeeeeek..."
scout_a[10] = "...krk.kk.krk..."
scout_a[11] = "..krk..kk..krk.."
scout_a[12] = "..kek..kk..kek.."
scout_a[13] = "...kk......kk..."

local scout_b = {
  "...kk......kk...", -- 1
  "...kyk....kyk...", -- 2
  "....krrrrrrk....", -- 3
  "...krrrrrrrrk...", -- 4
  "...krwkrrkwrk...", -- 5
  "..krrwkrrkwrrk..", -- 6
  "..krrrrrrrrrrk..", -- 7
  "...krrerrrerrk..", -- 8
  "...keeeeeeeek...", -- 9
  "....krk..krk....", -- 10
  "...krk.kk.krk...", -- 11
  "...kek.kk.kek...", -- 12
  "....kk....kk....", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}
scout_b[8] = "..krrerrrrerrk.."

-- Guard (middle rank, 20 pts): winged crab with pincer claws
local guard_a = {
  "..kk........kk..", -- 1
  ".kvvk......kvvk.", -- 2
  ".kvvvkeddekvvvk.", -- 3
  "..kvvvvvvvvvvk..", -- 4
  ".kvvvykvvvkyvvk.", -- 5
  "kvvvvykvvvkyvvvk", -- 6
  "kvvvvvvvvvvvvvvk", -- 7
  "kvpkvvvvvvvvkpvk", -- 8
  "kpkkppppppppkkpk", -- 9
  ".kk.kvvkkvvk.kk.", -- 10
  "....kvk..kvk....", -- 11
  "...kpvk..kvpk...", -- 12
  "...kkk....kkk...", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}

local guard_b = {
  "....kk....kk....", -- 1
  "...kvvk..kvvk...", -- 2
  "..kvvvkedkvvvk..", -- 3
  ".kvvvvvvvvvvvvk.", -- 4
  ".kvvvykvvvkyvvk.", -- 5
  "kvvvvykvvvkyvvvk", -- 6
  "kvvvvvvvvvvvvvvk", -- 7
  ".kvppppppppppvk.", -- 8
  "kvk.kvvkkvvk.kvk", -- 9
  "kpk..kvkkvk..kpk", -- 10
  ".kk.kvk..kvk.kk.", -- 11
  "....kpk..kpk....", -- 12
  "....kk....kk....", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}

-- Heavy (bottom rank, 10 pts): wide armored octopus
local heavy_a = {
  "....kkkkkkkk....", -- 1
  "..kkggggggggkk..", -- 2
  ".kggggggggggggk.", -- 3
  "kggggggggggggggk", -- 4
  "kggwwkggggkwwggk", -- 5
  "kggwwkggggkwwggk", -- 6
  "kggggggggggggggk", -- 7
  "kggmmmmmmmmmmggk", -- 8
  ".kggggggggggggk.", -- 9
  "..kgkkggggkkgk..", -- 10
  ".kgk.kgkkgk.kgk.", -- 11
  "kgk..kmkkmk..kgk", -- 12
  "kmk...kkkk...kmk", -- 13
  ".kk..........kk.", -- 14
  "................", -- 15
  "................", -- 16
}

local heavy_b = {
  "....kkkkkkkk....", -- 1
  "..kkggggggggkk..", -- 2
  ".kggggggggggggk.", -- 3
  "kggggggggggggggk", -- 4
  "kggwwkggggkwwggk", -- 5
  "kggwwkggggkwwggk", -- 6
  "kggggggggggggggk", -- 7
  "kggmmmmmmmmmmggk", -- 8
  ".kggggggggggggk.", -- 9
  "..kgk.kggk.kgk..", -- 10
  "..kggk.kk.kggk..", -- 11
  "...kmgk..kgmk...", -- 12
  "...kkkk..kkkk...", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}

-- UFO saucer (bonus lane): wide dome saucer with rotating beacon lights
local ufo_a = {
  "................", -- 1
  ".....kkkkkk.....", -- 2
  "...kkcccccckk...", -- 3
  "..kccwwcccccck..", -- 4
  "..kbbbbbbbbbbk..", -- 5
  ".krrrrrrrrrrrrk.", -- 6
  "krryrrcrryrrcrrk", -- 7
  "krrrrrrrrrrrrrrk", -- 8
  "keeeeeeeeeeeeeek", -- 9
  ".kkkkrrkkrrkkkk.", -- 10
  "....kyk..kyk....", -- 11
  "....kkk..kkk....", -- 12
  "................", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}

local ufo_b = {
  "................", -- 1
  ".....kkkkkk.....", -- 2
  "...kkcccccckk...", -- 3
  "..kccccccwwcck..", -- 4
  "..kbbbbbbbbbbk..", -- 5
  ".krrrrrrrrrrrrk.", -- 6
  "krrcrryrrcrryrrk", -- 7
  "krrrrrrrrrrrrrrk", -- 8
  "keeeeeeeeeeeeeek", -- 9
  ".kkkkrrkkrrkkkk.", -- 10
  "....kck..kck....", -- 11
  "....kkk..kkk....", -- 12
  "................", -- 13
  "................", -- 14
  "................", -- 15
  "................", -- 16
}

local frames = {
  scout_a, scout_b, to_flash(scout_a),
  guard_a, guard_b, to_flash(guard_a),
  heavy_a, heavy_b, to_flash(heavy_a),
  ufo_a,   ufo_b,   to_flash(ufo_a),
}

local sprite = Sprite(16, 16)
for index, rows in ipairs(frames) do
  assert(#rows == 16, string.format("frame %d has %d rows, expected 16", index, #rows))
  local image = Image(16, 16)
  for y, row in ipairs(rows) do
    assert(#row == 16, string.format("frame %d row %d has %d chars, expected 16", index, y, #row))
    for x = 1, #row do
      local ch = row:sub(x, x)
      local color = palette[ch]
      assert(color ~= nil, string.format("unknown char %q in frame %d at (%d,%d)", ch, index, x, y))
      image:drawPixel(x - 1, y - 1, color)
    end
  end
  if index > 1 then sprite:newEmptyFrame(index) end
  if index == 1 then
    sprite.cels[1].image = image
  else
    sprite:newCel(sprite.layers[1], index, image, Point(0, 0))
  end
  sprite.frames[index].duration = 0.25
end

local function tag(name, from, to)
  local t = sprite:newTag()
  t.name = name
  t.fromFrame = sprite.frames[from]
  t.toFrame = sprite.frames[to]
end

tag("scout", 1, 3)
tag("guard", 4, 6)
tag("heavy", 7, 9)
tag("ufo", 10, 12)

local ase_path = src_dir .. "/invaders.aseprite"
local png_path = out_dir .. "/invaders_sheet.png"
local json_path = out_dir .. "/invaders_sheet.json"

sprite:saveAs(ase_path)
print("wrote " .. ase_path)

app.sprite = sprite
app.command.ExportSpriteSheet{
  ui = false,
  askOverwrite = false,
  type = SpriteSheetType.HORIZONTAL,
  textureFilename = png_path,
  dataFilename = json_path,
  dataFormat = SpriteSheetDataFormat.JSON_ARRAY,
  listTags = true,
}
print("wrote " .. png_path)
print("wrote " .. json_path)
