-- PixelInvaders bunker arch preview and HUD badges (life cannon, crown, R/T/S badges): 6 frames (16x16 each).
-- Run:  Tools/aseprite/run.sh PixelInvaders bunker_and_hud.lua

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

local bunker_icon = {
  "................",
  "...kkkkkkkkkk...",
  "..kggggggggggk..",
  ".kggggggggggggk.",
  "kggggggggggggggk",
  "kggggggggggggggk",
  "kggggggggggggggk",
  "kggmmmmmmmmmmggk",
  "kggmkkkkkkkkmmgk",
  "kggmk......kmmgk",
  "kggmk......kmmgk",
  "kkkkk......kkkkk",
  "................",
  "................",
  "................",
  "................",
}

local life_icon = {
  ".......kk.......",
  "......kwwk......",
  "......kcck......",
  ".....kcccck.....",
  "...kkcccccckk...",
  "..kccccwwcccck..",
  ".kcccccccccccck.",
  ".kccbbbbbbbbcck.",
  "..kkkkkkkkkkkk..",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local crown_icon = {
  "................",
  "..kk...kk...kk..",
  ".kyyk.kyyk.kyyk.",
  ".kyyykkyykkyyyk.",
  "..kyyyyyyyyyyk..",
  "..kyrryyyyrryk..",
  "..kaaaaaaaaaak..",
  "...kkkkkkkkkk...",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local badge_rapid = {
  "..kkkkkkkkkkkk..",
  ".kaaaaaaaaaaaak.",
  "kayyyyyyyyyyyyak",
  "kayykwwwwk..yyak",
  "kayykwwkkwwkyyak",
  "kayykwwwwk..yyak",
  "kayykwwkwwk.yyak",
  "kayykwwk.wwkyyak",
  "kayyyyyyyyyyyyak",
  ".kaaaaaaaaaaaak.",
  "..kkkkkkkkkkkk..",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local badge_twin = {
  "..kkkkkkkkkkkk..",
  ".kbbbbbbbbbbbbk.",
  "kbccccccccccccbk",
  "kbcckwwwwwwkccbk",
  "kbcc..kwwk..ccbk",
  "kbcc..kwwk..ccbk",
  "kbcc..kwwk..ccbk",
  "kbcc..kwwk..ccbk",
  "kbccccccccccccbk",
  ".kbbbbbbbbbbbbk.",
  "..kkkkkkkkkkkk..",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local badge_shield = {
  "..kkkkkkkkkkkk..",
  ".kmmmmmmmmmmmmk.",
  "kmggggggggggggmk",
  "kmgg.kwwwwk.ggmk",
  "kmggkwwkkkk.ggmk",
  "kmgg.kwwwwk.ggmk",
  "kmgg.kkkkwwkggmk",
  "kmgg.kwwwwk.ggmk",
  "kmggggggggggggmk",
  ".kmmmmmmmmmmmmk.",
  "..kkkkkkkkkkkk..",
  "................",
  "................",
  "................",
  "................",
  "................",
}

local frames = {
  bunker_icon, life_icon, crown_icon,
  badge_rapid, badge_twin, badge_shield,
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

tag("bunker_icon", 1, 1)
tag("life_icon", 2, 2)
tag("crown_icon", 3, 3)
tag("badge_rapid", 4, 4)
tag("badge_twin", 5, 5)
tag("badge_shield", 6, 6)

local ase_path = src_dir .. "/bunker_and_hud.aseprite"
local png_path = out_dir .. "/bunker_and_hud_sheet.png"
local json_path = out_dir .. "/bunker_and_hud_sheet.json"

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
