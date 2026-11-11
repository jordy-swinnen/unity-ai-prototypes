-- Template: a small animation as a sprite sheet (PNG) plus its data (JSON, json-array format).
-- Copy to Tools/aseprite/<Prototype>/<name>.lua and run:  Tools/aseprite/run.sh <Prototype> <name>.lua
-- Checked on Aseprite 1.3.18.6. The same export from a saved file on the command line is:
--   "$ASEPRITE_PATH" --batch <src>.aseprite --sheet <out>.png --data <out>.json --format json-array
-- Unity imports the PNG as one Single sprite (see the importer in Assets/Shared/Editor). Cutting it into frames
-- from the JSON ("frames", each with x, y, w, h and duration in ms) is the prototype's own editor code.

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba

local colors = {
  clear = rgba(0, 0, 0, 0),
  ink   = rgba(40, 32, 48, 255),
  flame = rgba(232, 120, 48, 255),
  glow  = rgba(248, 208, 96, 255),
}

local legend = {
  ["."] = colors.clear,
  ["#"] = colors.ink,
  ["o"] = colors.flame,
  ["y"] = colors.glow,
}

-- A torch flame, two frames of 8x8. All frames must have the same size.
local frames = {
  {
    "...o....",
    "..ooo...",
    ".oyyyo..",
    ".oyyyo..",
    "..ooo...",
    "...#....",
    "...#....",
    "...#....",
  },
  {
    "....o...",
    "..ooo...",
    "..oyyo..",
    ".oyyyo..",
    "..ooo...",
    "...#....",
    "...#....",
    "...#....",
  },
}

local size = #frames[1]
local sprite = Sprite(size, size)

for index, rows in ipairs(frames) do
  assert(#rows == size, string.format("frame %d has %d rows, expected %d", index, #rows, size))
  local image = Image(size, size)
  for y, row in ipairs(rows) do
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
  sprite.frames[index].duration = 0.12 -- seconds
end

sprite:saveAs(src_dir .. "/torch.aseprite")
print("wrote " .. src_dir .. "/torch.aseprite")

app.sprite = sprite
app.command.ExportSpriteSheet{
  ui = false,
  askOverwrite = false,
  type = SpriteSheetType.HORIZONTAL,
  textureFilename = out_dir .. "/torch_sheet.png",
  dataFilename = out_dir .. "/torch_sheet.json",
  dataFormat = SpriteSheetDataFormat.JSON_ARRAY,
}
print("wrote " .. out_dir .. "/torch_sheet.png")
print("wrote " .. out_dir .. "/torch_sheet.json")
