using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "GlyphMappingDatabase", menuName = "Scriptable Objects/GlyphMappingDatabase")]
public class GlyphMappingDatabase : ScriptableObject {
    public TMP_SpriteAsset spriteAsset;
    public GlyphMapping[] mappings;
}
