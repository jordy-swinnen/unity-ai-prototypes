-- Template: one pixel-art icon, drawn from a character map.
-- Copy to Tools/aseprite/<Prototype>/<name>.lua and run:  Tools/aseprite/run.sh <Prototype> <name>.lua
-- Checked on Aseprite 1.3.18.6. A new Sprite already has one layer, one frame and one empty cel,
-- so sprite.cels[1].image = image works.

local out_dir = app.params["out_dir"]
local src_dir = app.params["src_dir"]
assert(out_dir and src_dir, "run this through Tools/aseprite/run.sh, which sets out_dir and src_dir")

local rgba = app.pixelColor.rgba

-- 1. Named palette: every colour has a name, so a diff shows what changed and why.
local colors = {
  clear = rgba(0, 0, 0, 0),
  ink   = rgba(40, 32, 48, 255),
  steel = rgba(216, 221, 227, 255),
  gold  = rgba(201, 160, 72, 255),
}

-- 2. Legend: one character per colour. "." is always transparent.
local legend = {
  ["."] = colors.clear,
  ["#"] = colors.ink,
  ["w"] = colors.steel,
  ["g"] = colors.gold,
}

-- 3. The drawing. Every row has the same length; the map is square (a sword, 12x12).
local rows = {
  "..........##",
  ".........#w#",
  "........#w#.",
  ".......#w#..",
  "......#w#...",
  ".#...#w#....",
  ".##.#w#.....",
  "..##w#......",
  "..#g##......",
  ".#g#.##.....",
  "#g#...#.....",
  "##..........",
}

local size = #rows
local sprite = Sprite(size, size)
local image = Image(size, size)

for y, row in ipairs(rows) do
  -- Aseprite does not complain about a nil colour (drawPixel just draws nothing), so check here.
  assert(#row == size, string.format("row %d has %d characters, expected %d", y, #row, size))
  for x = 1, #row do
    local ch = row:sub(x, x)
    local color = legend[ch]
    assert(color ~= nil, string.format("unknown character %q at row %d, column %d", ch, y, x))
    image:drawPixel(x - 1, y - 1, color)
  end
end

sprite.cels[1].image = image

-- 4. Save the source (.aseprite, Git LFS) and a PNG copy for Unity.
-- run.sh checks every "wrote" line: print one for each file, with the path as written.
sprite:saveAs(src_dir .. "/intent_attack.aseprite")
print("wrote " .. src_dir .. "/intent_attack.aseprite")
sprite:saveCopyAs(out_dir .. "/intent_attack.png")
print("wrote " .. out_dir .. "/intent_attack.png")
