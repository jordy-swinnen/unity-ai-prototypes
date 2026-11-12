-- PixelInvaders player cannon (idle, tilt, recoil, shatter) and projectiles: 13 frames (16x16 each).
-- Run:  Tools/aseprite/run.sh PixelInvaders cannon_and_shots.lua

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

local cannon_idle = {
  ".......kk.......", -- 1
  "......kwwk......", -- 2
  "......kcck......", -- 3
  ".....kcccck.....", -- 4
  ".....kcwwck.....", -- 5
  "....kcccccck....", -- 6
  "..kkcccccccckk..", -- 7
  ".kccccwwwwcccck.", -- 8
  "kcccccccccccccbb", -- 9
  "kccbbbbbbbbbbcck", -- 10
  "kcbssssssssssbck", -- 11
  "kcsshhhhhhhhssck", -- 12
  ".kkkkkkyykkkkkk.", -- 13
  "......kaak......", -- 14
  ".......kk.......", -- 15
  "................", -- 16
}
-- Fix row 9 symmetry
cannon_idle[9] = "kcccccccccccccck"

local cannon_left = {
  "......kk........", -- 1
  ".....kwwk.......", -- 2
  ".....kcck.......", -- 3
  "....kcccck......", -- 4
  "....kcwwck......", -- 5
  "...kcccccck.....", -- 6
  ".kkcccccccckk...", -- 7
  "kccccwwwwcccck..", -- 8
  "kcccccccccccccck", -- 9
  "kccbbbbbbbbbbcck", -- 10
  "kcbssssssssssbck", -- 11
  ".kcsshhhhhhhhssk", -- 12
  "..kkkkkyykkkkkk.", -- 13
  "......kaak......", -- 14
  ".......kk.......", -- 15
  "................", -- 16
}

local cannon_right = {
  "........kk......", -- 1
  ".......kwwk.....", -- 2
  ".......kcck.....", -- 3
  "......kcccck....", -- 4
  "......kcwwck....", -- 5
  ".....kcccccck...", -- 6
  "...kkcccccccckk.", -- 7
  "..kccccwwwwcccck", -- 8
  "kcccccccccccccck", -- 9
  "kccbbbbbbbbbbcck", -- 10
  "kcbssssssssssbck", -- 11
  "ksshhhhhhhhssck.", -- 12
  ".kkkkkkyykkkkk..", -- 13
  "......kaak......", -- 14
  ".......kk.......", -- 15
  "................", -- 16
}

local cannon_recoil = {
  ".....kywwyk.....", -- 1
  "......kwwk......", -- 2
  ".....kcccck.....", -- 3
  ".....kcwwck.....", -- 4
  "....kcccccck....", -- 5
  "..kkcccccccckk..", -- 6
  ".kccccwwwwcccck.", -- 7
  "kcccccccccccccck", -- 8
  "kccbbbbbbbbbbcck", -- 9
  "kcbssssssssssbck", -- 10
  "kcsshhhhhhhhssck", -- 11
  ".kkkkkkyykkkkkk.", -- 12
  ".....kyaayk.....", -- 13
  "......kaak......", -- 14
  ".......kk.......", -- 15
  "................", -- 16
}

local shatter_1 = {
  "......kyyk......",
  "....kwyyyywk....",
  "...kywwwwwwyk...",
  "..kccwwwwwwcck..",
  ".kcccwwyywwccck.",
  ".kcbbyyyyyybbck.",
  "..kssryyyrsssk..",
  "..k..kywwyk..k..",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local shatter_2 = {
  "..kc........ck..",
  "....ky....yk....",
  ".kw..krrrrk..wk.",
  "...k.rywwyr.k...",
  "..kc.ywwwwy.ck..",
  "....k.ryyr.k....",
  ".kb..k....k..bk.",
  "...ka......ak...",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local shatter_3 = {
  ".c............c.",
  "....y......y....",
  "......r..r......",
  "..w....yy....w..",
  "......r..r......",
  "...b........b...",
  ".....a....a.....",
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

local shatter_4 = {
  "................",
  "...a........a...",
  ".......ee.......",
  "....e......e....",
  ".......ee.......",
  "..s..........s..",
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

local shot_player = {
  ".......kk.......",
  "......kwwk......",
  "......kwwk......",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  "......kcck......",
  "......kbbk......",
  ".......kk.......",
  "................",
  "................",
  "................",
}

local shot_twin = {
  ".......kk.......",
  "......kyyk......",
  "......kwwk......",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcyyck.....",
  ".....kcwwck.....",
  ".....kcwwck.....",
  ".....kcccck.....",
  "......kcck......",
  "......kbbk......",
  ".......kk.......",
  "................",
  "................",
  "................",
  "................",
}

local muzzle_flash = {
  ".......kk.......",
  "......kwwk......",
  "....kkywwykk....",
  "...kywwwwwwyk...",
  "....kkywwykk....",
  "......kcck......",
  ".......kk.......",
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

local shot_alien_a = {
  "......krrk......",
  ".....krwwrk.....",
  "......krwk......",
  ".......krwk.....",
  "......krwk......",
  ".....krwk.......",
  "......krwk......",
  ".......krwk.....",
  "......kryrk.....",
  ".....kywwyk.....",
  "......kyyk......",
  ".......kk.......",
  "................",
  "................",
  "................",
  "................",
}

local shot_alien_b = {
  "......kvvk......",
  ".....kvwwvk.....",
  ".......kvwk.....",
  "......kvwk......",
  ".....kvwk.......",
  "......kvwk......",
  ".......kvwk.....",
  "......kvwk......",
  "......kvyvk.....",
  ".....kywwyk.....",
  "......kyyk......",
  ".......kk.......",
  "................",
  "................",
  "................",
  "................",
}

local frames = {
  cannon_idle, cannon_left, cannon_right, cannon_recoil,
  shatter_1, shatter_2, shatter_3, shatter_4,
  shot_player, shot_twin, muzzle_flash, shot_alien_a, shot_alien_b,
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
  sprite.frames[index].duration = 0.12
end

local function tag(name, from, to)
  local t = sprite:newTag()
  t.name = name
  t.fromFrame = sprite.frames[from]
  t.toFrame = sprite.frames[to]
end

tag("cannon", 1, 4)
tag("cannon_shatter", 5, 8)
tag("shots", 9, 13)

local ase_path = src_dir .. "/cannon_and_shots.aseprite"
local png_path = out_dir .. "/cannon_and_shots_sheet.png"
local json_path = out_dir .. "/cannon_and_shots_sheet.json"

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
