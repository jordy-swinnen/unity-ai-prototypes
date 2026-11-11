-- PixelInvaders power-up capsules (Rapid Fire R, Twin Shot T, Shield Repair S) and explosions: 15 frames (16x16 each).
-- Run:  Tools/aseprite/run.sh PixelInvaders powerups_and_fx.lua

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba
local palette = {
  ["."] = rgba(0, 0, 0, 0),
  ["k"] = rgba(9, 12, 18, 255),
  ["d"] = rgba(22, 30, 43, 255),
  ["s"] = rgba(44, 57, 75, 255),
  ["h"] = rgba(99, 118, 141, 255),
  ["w"] = rgba(240, 246, 255, 255),
  ["a"] = rgba(217, 130, 43, 255),
  ["y"] = rgba(255, 209, 71, 255),
  ["c"] = rgba(44, 232, 245, 255),
  ["b"] = rgba(27, 118, 181, 255),
  ["g"] = rgba(56, 217, 115, 255),
  ["m"] = rgba(30, 130, 76, 255),
  ["v"] = rgba(184, 92, 255, 255),
  ["p"] = rgba(107, 47, 163, 255),
  ["r"] = rgba(255, 71, 105, 255),
  ["e"] = rgba(168, 29, 57, 255),
}

-- Capsule helper: replaces 'M' (main), 'H' (highlight), 'G' (glyph) in a 16x16 pill template
local function make_capsule(glyph_rows, main_ch, hi_ch, rim_ch)
  local base = {
    "....kkkkkkkk....", -- 1
    "..kkRRRRRRRRkk..", -- 2
    ".kRHHHHHHHHHHRk.", -- 3
    "kRMMMMMMMMMMMMRk", -- 4
    "kRMM........MMRk", -- 5
    "kRMM........MMRk", -- 6
    "kRMM........MMRk", -- 7
    "kRMM........MMRk", -- 8
    "kRMM........MMRk", -- 9
    "kRMM........MMRk", -- 10
    "kRMM........MMRk", -- 11
    "kRMMMMMMMMMMMMRk", -- 12
    ".kRMMMMMMMMMMRk.", -- 13
    "..kkRRRRRRRRkk..", -- 14
    "....kkkkkkkk....", -- 15
    "................", -- 16
  }
  local out = {}
  for r = 1, 16 do
    local row = base[r]
    if r >= 5 and r <= 11 then
      local g = glyph_rows[r - 4]
      row = row:sub(1, 4) .. g .. row:sub(13, 16)
    end
    row = row:gsub("M", main_ch):gsub("H", hi_ch):gsub("R", rim_ch)
    out[r] = row
  end
  return out
end

local glyph_R = {
  "kwwwwk..",
  "kwwkkwwk",
  "kwwkkwwk",
  "kwwwwvk.",
  "kwwkwwk.",
  "kwwk.wwk",
  "kkkk..kk",
}
glyph_R[4] = "kwwwwkk."

local glyph_T = {
  "kwwwwwwk",
  "kwwwwwwk",
  "..kwwk..",
  "..kwwk..",
  "..kwwk..",
  "..kwwk..",
  "..kkkk..",
}

local glyph_S = {
  ".kwwwwk.",
  "kwwkkkk.",
  "kwwwwk..",
  ".kkkkwwk",
  "....kwwk",
  "kwwwwk..",
  ".kkkk...",
}

local cap_rapid_a  = make_capsule(glyph_R, "a", "y", "s")
local cap_rapid_b  = make_capsule(glyph_R, "a", "w", "y")
local cap_twin_a   = make_capsule(glyph_T, "b", "c", "s")
local cap_twin_b   = make_capsule(glyph_T, "b", "w", "c")
local cap_shield_a = make_capsule(glyph_S, "m", "g", "s")
local cap_shield_b = make_capsule(glyph_S, "m", "w", "g")

-- Alien explosion (3 frames)
local alien_burst_1 = {
  ".......kk.......",
  "....kk.yy.kk....",
  "...kyy.ww.yyk...",
  "..kywwwwwwwwyk..",
  ".k.ywwwwwwwwy.k.",
  "..kwwwwwwwwwwk..",
  ".kywwwwwwwwwwyk.",
  "kkywwwwwwwwwwykk",
  ".kywwwwwwwwwwyk.",
  "..kwwwwwwwwwwk..",
  ".k.ywwwwwwwwy.k.",
  "..kywwwwwwwwyk..",
  "...kyy.ww.yyk...",
  "....kk.yy.kk....",
  ".......kk.......",
  "................",
}

local alien_burst_2 = {
  "..rr........rr..",
  ".ryyr..yy..ryyr.",
  ".ryy..ywwy..yyr.",
  ".....ywwwwy.....",
  "..y.yww..wwy.y..",
  ".yww........wwy.",
  ".yww........wwy.",
  "..y.yww..wwy.y..",
  ".....ywwwwy.....",
  ".ryy..ywwy..yyr.",
  ".ryyr..yy..ryyr.",
  "..rr........rr..",
  "................",
  "................",
  "................",
  "................",
}

local alien_burst_3 = {
  ".e............e.",
  "....r......r....",
  "..a..........a..",
  "......y..y......",
  "................",
  ".r............r.",
  "................",
  "......y..y......",
  "..a..........a..",
  "....r......r....",
  ".e............e.",
  "................",
  "................",
  "................",
  "................",
  "................",
}

-- UFO big explosion (4 frames)
local ufo_burst_1 = {
  "....kkyyyykk....",
  "..kkywwwwwwykk..",
  ".kywwwwwwwwwwyk.",
  "kywwwwwwwwwwwwyk",
  "kywwwwwwwwwwwwyk",
  "kywwwwwwwwwwwwyk",
  ".kywwwwwwwwwwyk.",
  "..kkywwwwwwykk..",
  "....kkyyyykk....",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local ufo_burst_2 = {
  "..rr..yyyy..rr..",
  ".rwwryyyyyyrwwr.",
  "rwwwywwwwwwywwwr",
  ".rwywwccccwwywr.",
  ".rywwccccccwwyr.",
  ".rwywwccccwwywr.",
  "rwwwywwwwwwywwwr",
  ".rwwryyyyyyrwwr.",
  "..rr..yyyy..rr..",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local ufo_burst_3 = {
  ".r....y..y....r.",
  "..r..y....y..r..",
  "...ec......ce...",
  ".y..c......c..y.",
  "....c......c....",
  ".y..c......c..y.",
  "...ec......ce...",
  "..r..y....y..r..",
  ".r....y..y....r.",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local ufo_burst_4 = {
  "e..............e",
  ".....a....a.....",
  "..e..........e..",
  ".......bb.......",
  "................",
  ".......bb.......",
  "..e..........e..",
  ".....a....a.....",
  "e..............e",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local spark_1 = {
  "................",
  ".......yy.......",
  ".....yywwyy.....",
  "....ywwwwwwy....",
  ".....yywwyy.....",
  ".......yy.......",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local spark_2 = {
  ".......a........",
  "....y.....y.....",
  ".......w........",
  "...a..w.w..a....",
  ".......w........",
  "....y.....y.....",
  ".......a........",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local frames = {
  cap_rapid_a, cap_rapid_b,
  cap_twin_a, cap_twin_b,
  cap_shield_a, cap_shield_b,
  alien_burst_1, alien_burst_2, alien_burst_3,
  ufo_burst_1, ufo_burst_2, ufo_burst_3, ufo_burst_4,
  spark_1, spark_2,
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
  sprite.frames[index].duration = 0.10
end

local function tag(name, from, to)
  local t = sprite:newTag()
  t.name = name
  t.fromFrame = sprite.frames[from]
  t.toFrame = sprite.frames[to]
end

tag("capsule_rapid", 1, 2)
tag("capsule_twin", 3, 4)
tag("capsule_shield", 5, 6)
tag("alien_burst", 7, 9)
tag("ufo_burst", 10, 13)
tag("spark", 14, 15)

local ase_path = src_dir .. "/powerups_and_fx.aseprite"
local png_path = out_dir .. "/powerups_and_fx_sheet.png"
local json_path = out_dir .. "/powerups_and_fx_sheet.json"

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
