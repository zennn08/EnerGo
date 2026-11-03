using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    // The UI Toolkit theme must carry every design-system token under the same name and value,
    // so docs/design-system and Assets/UI can be compared one to one.
    public class ThemeTokensTests
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string DesignSystem => Path.Combine(Root, "docs", "design-system");

        [Test]
        public void EveryTokenIsAUssVariableWithTheSameValue()
        {
            string json = File.ReadAllText(Path.Combine(DesignSystem, "tokens.json"));
            string uss = File.ReadAllText(Path.Combine(Root, "Assets", "UI", "Theme.uss")).ToLowerInvariant();
            var tokens = Regex.Matches(json, "\"name\":\\s*\"([^\"]+)\",\\s*\"value\":\\s*\"([^\"]+)\"");
            Assert.That(tokens.Count, Is.GreaterThan(60), "tokens.json parsed");
            foreach (Match t in tokens)
            {
                // The shadow family has no USS counterpart (no box-shadow); Components.uss draws it as offsets.
                if (t.Groups[1].Value.StartsWith("bayang-")) continue;
                string decl = $"--{t.Groups[1].Value}: {t.Groups[2].Value}".ToLowerInvariant();
                Assert.That(uss, Does.Contain(decl), decl);
            }
        }

        [Test]
        public void EveryDesignIconIsCopied()
        {
            string src = Path.Combine(DesignSystem, "assets", "Ikon");
            string dst = Path.Combine(Root, "Assets", "UI", "Icons");
            var icons = Directory.GetFiles(src, "*.svg");
            Assert.That(icons.Length, Is.GreaterThan(20));
            foreach (string icon in icons)
            {
                string copy = Path.Combine(dst, Path.GetFileName(icon));
                Assert.IsTrue(File.Exists(copy), copy);
                Assert.IsTrue(File.ReadAllBytes(icon).SequenceEqual(File.ReadAllBytes(copy)), copy);
            }
        }
    }
}
