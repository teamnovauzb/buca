"""Bake the existing Cormorant lining figures for TMP, which uses cmap glyphs.

Requires fonttools. Run from any directory; the original font is kept intact.
The derivative remains under the adjacent SIL Open Font License (OFL.txt).
"""
from pathlib import Path

from fontTools.ttLib import TTFont


def main():
    folder = Path(__file__).resolve().parents[1] / "Assets/TwilightMap/Artwork/Fonts"
    font = TTFont(folder / "CormorantGaramond-Bold.ttf")
    substitutions = {}
    gsub = font["GSUB"].table
    for record in gsub.FeatureList.FeatureRecord:
        if record.FeatureTag == "lnum":
            for index in record.Feature.LookupListIndex:
                for table in gsub.LookupList.Lookup[index].SubTable:
                    substitutions.update(table.mapping)
    digits = {code: substitutions[font.getBestCmap()[code]] for code in range(48, 58)}
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap.update(digits)
    names = {
        1: "Buca Plaque", 2: "Bold", 3: "BucaPlaque-Bold-Lining-1.0",
        4: "Buca Plaque Bold", 6: "BucaPlaque-Bold",
        16: "Buca Plaque", 17: "Bold",
        18: "Buca Plaque Bold", 21: "Buca Plaque", 22: "Bold",
    }
    for record in font["name"].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    output = folder / "BucaPlaque-Bold.ttf"
    font.save(output)
    print(f"Saved {output.name}: 0–9 use the font's matching lining figures.")


if __name__ == "__main__":
    main()
