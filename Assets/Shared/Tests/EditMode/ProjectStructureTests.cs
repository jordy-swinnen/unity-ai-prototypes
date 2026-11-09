using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Shared.Tests
{
    /// <summary>
    /// Keeps the project's structure as AGENTS.md and docs/project-structure.md describe it, so that it stays
    /// easy to work in as more prototypes are added. Each rule is from Unity's "organizing your project" guide
    /// or from this repository's own rules. A failure names the file to fix.
    /// </summary>
    public class ProjectStructureTests
    {
        // Folders and files directly under Assets that are allowed. The last four are leftovers from the URP
        // project template. Remove them with the person's approval, then delete them from this list.
        static readonly string[] AssetsRoot =
        {
            "Prototypes",   // one folder per game
            "Shared",       // code and assets used by two or more games
            "ThirdParty",   // anything we did not make (Asset Store, downloads), kept apart from our own
            "Sandbox",      // personal experiments, ignored by git
            "Settings",     // the render pipeline assets (URP)
            "Scenes", "TutorialInfo", "Readme.asset", "InputSystem_Actions.inputactions", // template leftovers
        };

        // Names that already break the no-spaces rule and are waiting to be renamed on their own branch.
        static readonly string[] SpacesAllowedUnder =
        {
            "Assets/Prototypes/NeonPong/Materials/", // renamed on feat/neon-pong (PR 1), not here
        };

        static string Root
        {
            get
            {
                var dir = Directory.GetCurrentDirectory();
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "Assets")), "the tests must run from the project folder");
                return dir;
            }
        }

        static string Rel(string path) => path.Substring(Root.Length + 1).Replace('\\', '/');

        static string[] Prototypes() => Directory.GetDirectories(Path.Combine(Root, "Assets/Prototypes")).Select(Path.GetFileName).OrderBy(n => n).ToArray();

        static string[] SourceFiles(string folder) =>
            Directory.GetFiles(Path.Combine(Root, folder), "*.cs", SearchOption.AllDirectories);

        static string Show(System.Collections.Generic.IEnumerable<string> items) => string.Join("\n  ", items);

        [Test]
        public void Nothing_unexpected_sits_at_the_top_of_Assets()
        {
            var found = Directory.GetFileSystemEntries(Path.Combine(Root, "Assets"))
                .Select(Path.GetFileName).Where(n => !n.EndsWith(".meta")).ToArray();
            var stray = found.Where(n => !AssetsRoot.Contains(n)).ToArray();
            Assert.IsEmpty(stray, "Put game content in Assets/Prototypes/<Name>, shared content in Assets/Shared and anything we did not make in Assets/ThirdParty:\n  " + Show(stray));
        }

        [Test]
        public void Asset_and_folder_names_have_no_spaces()
        {
            var bad = Directory.GetFileSystemEntries(Path.Combine(Root, "Assets"), "*", SearchOption.AllDirectories)
                .Select(Rel)
                .Where(p => Path.GetFileName(p).Contains(' ') && !SpacesAllowedUnder.Any(p.StartsWith))
                .ToArray();
            Assert.IsEmpty(bad, "Unity's command line tools trip on spaces. Use PascalCase (MedievalChessTileLight, not 'MedievalChess Tile Light'):\n  " + Show(bad));
        }

        [Test]
        public void There_are_no_empty_folders()
        {
            var empty = Directory.GetDirectories(Path.Combine(Root, "Assets"), "*", SearchOption.AllDirectories)
                .Where(d => Directory.GetFileSystemEntries(d).All(e => e.EndsWith(".meta") && Directory.Exists(e.Substring(0, e.Length - 5)) == false))
                .Select(Rel).ToArray();
            Assert.IsEmpty(empty, "Git does not keep empty folders, and the stray .meta files then confuse Unity. Remove them, or put something in:\n  " + Show(empty));
        }

        [Test]
        public void Every_prototype_is_a_PascalCase_folder_with_its_own_assemblies()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (var name in Prototypes())
            {
                if (!Regex.IsMatch(name, "^[A-Z][A-Za-z0-9]*$")) problems.Add($"{name}: the folder name must be PascalCase with no spaces or dashes");
                var asmdefs = Directory.GetFiles(Path.Combine(Root, "Assets/Prototypes", name), "*.asmdef", SearchOption.AllDirectories)
                    .Select(Path.GetFileNameWithoutExtension).ToArray();
                if (!asmdefs.Contains(name)) problems.Add($"{name}: missing the runtime assembly {name}.asmdef (under Scripts/)");
                foreach (var a in asmdefs)
                    if (a != name && !a.StartsWith(name + ".")) problems.Add($"{name}: assembly '{a}' must be named {name} or {name}.<Part>");
            }
            Assert.IsEmpty(problems, Show(problems));
        }

        [Test]
        public void Code_uses_the_namespace_that_matches_its_folder()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (var name in Prototypes())
            {
                foreach (var file in SourceFiles("Assets/Prototypes/" + name))
                {
                    var match = Regex.Match(File.ReadAllText(file), @"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
                    string ns = match.Success ? match.Groups[1].Value : "(none)";
                    if (ns != "Prototypes." + name && !ns.StartsWith("Prototypes." + name + "."))
                        problems.Add($"{Rel(file)}: namespace {ns}, expected Prototypes.{name}[.Part]");
                }
            }
            foreach (var file in SourceFiles("Assets/Shared"))
            {
                var match = Regex.Match(File.ReadAllText(file), @"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
                string ns = match.Success ? match.Groups[1].Value : "(none)";
                if (ns != "Shared" && !ns.StartsWith("Shared.")) problems.Add($"{Rel(file)}: namespace {ns}, expected Shared[.Part]");
            }
            Assert.IsEmpty(problems, Show(problems));
        }

        [Test]
        public void Prototypes_never_reference_each_other_and_Shared_never_references_a_prototype()
        {
            var prototypes = Prototypes();
            var problems = new System.Collections.Generic.List<string>();

            void Check(string asmdefPath, string owner)
            {
                var text = File.ReadAllText(asmdefPath);
                var refs = Regex.Matches(Regex.Match(text, "\"references\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline).Groups[1].Value, "\"([^\"]+)\"")
                    .Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
                foreach (var r in refs)
                {
                    var other = prototypes.FirstOrDefault(p => r == p || r.StartsWith(p + "."));
                    if (other != null && other != owner)
                        problems.Add($"{Rel(asmdefPath)} references {r}: {(owner == "Shared" ? "Shared" : owner)} must not depend on the {other} prototype. Move what is shared into Assets/Shared.");
                }
            }

            foreach (var name in prototypes)
                foreach (var f in Directory.GetFiles(Path.Combine(Root, "Assets/Prototypes", name), "*.asmdef", SearchOption.AllDirectories)) Check(f, name);
            foreach (var f in Directory.GetFiles(Path.Combine(Root, "Assets/Shared"), "*.asmdef", SearchOption.AllDirectories)) Check(f, "Shared");
            Assert.IsEmpty(problems, Show(problems));
        }

        [Test]
        public void Generated_art_sits_in_its_place_and_every_asset_has_a_meta_file()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (var name in Prototypes())
            {
                string proto = Path.Combine(Root, "Assets/Prototypes", name);
                foreach (var f in Directory.GetFiles(proto, "*.glb", SearchOption.AllDirectories))
                    if (!Rel(f).Contains("/Art/Exports/")) problems.Add($"{Rel(f)}: .glb files belong in Art/Exports");
                foreach (var f in Directory.GetFiles(proto, "*.png", SearchOption.AllDirectories))
                    if (!Rel(f).Contains("/Art/Textures/")) problems.Add($"{Rel(f)}: .png files belong in Art/Textures");
                foreach (var f in Directory.GetFiles(proto, "*.shader", SearchOption.AllDirectories))
                    if (!Rel(f).Contains("/Shaders/")) problems.Add($"{Rel(f)}: .shader files belong in Shaders");
            }
            foreach (var f in Directory.GetFileSystemEntries(Path.Combine(Root, "Assets"), "*", SearchOption.AllDirectories)
                         .Where(p => !p.EndsWith(".meta") && !Path.GetFileName(p).StartsWith(".") && !p.EndsWith("~")))
                if (!File.Exists(f + ".meta")) problems.Add($"{Rel(f)}: no .meta file (commit the .meta files Unity makes)");
            Assert.IsEmpty(problems, Show(problems));
        }
    }
}
