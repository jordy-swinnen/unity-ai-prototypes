-- DioramaTurn pieces: the plank bridge and the goal star (4 twinkle frames).
-- Run:  Tools/aseprite/run.sh DioramaTurn pieces.lua
-- Palette: the spec's shared colours (plank wood, goal gold, goal pink, ink).

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba
local colors = {
  clear = rgba(0, 0, 0, 0),
  ink   = rgba(59, 42, 77, 255),    -- #3B2A4D
  plank = rgba(232, 182, 122, 255), -- #E8B67A
  gold  = rgba(255, 217, 59, 255),  -- #FFD93B
  pink  = rgba(255, 154, 200, 255), -- #FF9AC8
}
local legend = {
  ["."] = colors.clear,
  ["k"] = colors.ink,
  ["w"] = colors.plank,
  ["y"] = colors.gold,
  ["p"] = colors.pink,
}

local function draw(size, rows, path)
  local sprite = Sprite(size, size)
  local image = Image(size, size)
  for y, row in ipairs(rows) do
    assert(#row == size, string.format("%s row %d has %d characters, expected %d", path, y, #row, size))
    for x = 1, #row do
      local ch = row:sub(x, x)
      local color = legend[ch]
      assert(color ~= nil, string.format("unknown character %q in %s at row %d, column %d", ch, path, y, x))
      image:drawPixel(x - 1, y - 1, color)
    end
  end
  sprite.cels[1].image = image
  return sprite
end

local function map(size, rows)
  assert(#rows == size, string.format("map has %d rows, expected %d", #rows, size))
  return rows
end

-- A plank the width of a cell: 28 x 12 pixels, centred.
local plank = map(32, {
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "...kkkkkkkkkkkkkkkkkkkkkkkkkk...",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
  "...kkkkkkkkkkkkkkkkkkkkkkkkkk...",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
  "................................",
})

local sprite = draw(32, plank, "plank")
sprite:saveAs(src_dir .. "/plank.aseprite")
print("wrote " .. src_dir .. "/plank.aseprite")
sprite:saveCopyAs(out_dir .. "/plank.png")
print("wrote " .. out_dir .. "/plank.png")

-- The goal star: a five-point star with ink outline, four twinkle frames.
local star = map(32, {
  "................................",
  "................................",
  "................................",
  "................................",
  "...............yy...............",
  "..............kyyk..............",
  "..............kyyk..............",
  ".............kyyyyk.............",
  ".............kyyyyk.............",
  "............kyyyyyyk............",
  ".........kyyyyyyyyyyyyk.........",
  "....kyyyyyyyyyyyyyyyyyyyyk.....",
  "....kyyyyyyyyyyyyyyyyyyyyk.....",
  ".....kyyyyyyyyyyyyyyyyyyk......",
  "......kyyyyyyyyyyyyyyyyk.......",
  ".......kyyyyyyyyyyyyyyk........",
  "........kyyyyyyyyyyyyk.........",
  ".........kyyyyyyyyyyk..........",
  "..........kyyyyyyyyk...........",
  "..........kyyyyyyyyk...........",
  "..........kyyyyyyyyk...........",
  ".........kyyyyyyyyyyk..........",
  ".........kyyyyy..yyyyk.........",
  "........kyyyy....yyyyk.........",
  "........kyyy......yyyk.........",
  ".......kyy..........yyk........",
  ".......ky............yk........",
  "......kp..............pk.......",
  "................................",
  "................................",
  "................................",
  "................................",
})

local function sparkle(base, cells)
  local rows = {}
  for i, row in ipairs(base) do rows[i] = row end
  for _, c in ipairs(cells) do
    local y, x = c[1], c[2]
    rows[y] = rows[y]:sub(1, x - 1) .. "y" .. rows[y]:sub(x + 1)
  end
  return rows
end

local star_frames = {
  star,
  sparkle(star, { { 2, 6 }, { 2, 27 }, { 8, 3 }, { 8, 30 } }),
  sparkle(star, { { 1, 15 }, { 1, 17 }, { 4, 10 }, { 4, 22 } }),
  sparkle(star, { { 3, 7 }, { 3, 26 }, { 9, 2 }, { 9, 31 } }),
}

local sheet = Sprite(32, 32)
for index, rows in ipairs(star_frames) do
  local image = Image(32, 32)
  for y, row in ipairs(rows) do
    for x = 1, #row do
      local color = legend[row:sub(x, x)]
      assert(color ~= nil, string.format("star frame %d row %d column %d has an unknown character", index, y, x))
      image:drawPixel(x - 1, y - 1, color)
    end
  end
  if index > 1 then sheet:newEmptyFrame(index) end
  if index == 1 then sheet.cels[1].image = image else sheet:newCel(sheet.layers[1], index, image, Point(0, 0)) end
  sheet.frames[index].duration = 0.18
end

local twinkle = sheet:newTag()
twinkle.name = "twinkle"
twinkle.fromFrame = sheet.frames[1]
twinkle.toFrame = sheet.frames[4]

sheet:saveAs(src_dir .. "/star.aseprite")
print("wrote " .. src_dir .. "/star.aseprite")

app.sprite = sheet
app.command.ExportSpriteSheet{
  ui = false,
  askOverwrite = false,
  type = SpriteSheetType.HORIZONTAL,
  textureFilename = out_dir .. "/star_sheet.png",
  dataFilename = out_dir .. "/star_sheet.json",
  dataFormat = SpriteSheetDataFormat.JSON_ARRAY,
}
print("wrote " .. out_dir .. "/star_sheet.png")
print("wrote " .. out_dir .. "/star_sheet.json")
