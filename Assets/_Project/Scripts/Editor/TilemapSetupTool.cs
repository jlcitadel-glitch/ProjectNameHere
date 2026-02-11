using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// One-time editor tool to set up Sunny Land Forest tilemap infrastructure.
/// Creates tile assets from the tileset sprite sheet, builds a tile palette,
/// and adds a properly configured Grid/Tilemap to the active scene.
///
/// Usage: Tools > Sunny Land Forest > Setup Tilemap
/// Safe to delete after use.
/// </summary>
public static class TilemapSetupTool
{
    private const string TilesetPath = "Assets/Sunny Land Forest Assets/Artwork/Environment/tileset.png";
    private const string TileAssetFolder = "Assets/_Project/Art/Tiles/SunnyLandForest";
    private const string PaletteFolder = "Assets/_Project/Art/Tiles";
    private const string PaletteName = "SunnyLandForest";

    [MenuItem("Tools/Sunny Land Forest/Setup Tilemap (Full)")]
    public static void SetupAll()
    {
        CreateTileAssets();
        CreateTilePalette();
        CreateSceneTilemap();
        Debug.Log("[TilemapSetupTool] Full setup complete. Open Window > 2D > Tile Palette to start painting.");
    }

    [MenuItem("Tools/Sunny Land Forest/1 - Create Tile Assets")]
    public static void CreateTileAssets()
    {
        // Load all sprites from the tileset
        var sprites = AssetDatabase.LoadAllAssetsAtPath(TilesetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name, new NaturalStringComparer())
            .ToArray();

        if (sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Error",
                $"No sprites found at:\n{TilesetPath}\n\nMake sure the tileset is sliced (Sprite Mode: Multiple).",
                "OK");
            return;
        }

        // Create output folder
        EnsureFolderExists(TileAssetFolder);

        int created = 0;
        int skipped = 0;

        foreach (var sprite in sprites)
        {
            string tilePath = $"{TileAssetFolder}/{sprite.name}.asset";

            if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
            {
                skipped++;
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Sprite;

            AssetDatabase.CreateAsset(tile, tilePath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TilemapSetupTool] Tile assets: {created} created, {skipped} already existed. Total sprites: {sprites.Length}");
    }

    [MenuItem("Tools/Sunny Land Forest/2 - Create Tile Palette")]
    public static void CreateTilePalette()
    {
        string palettePath = $"{PaletteFolder}/{PaletteName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null)
        {
            if (!EditorUtility.DisplayDialog("Palette Exists",
                $"Palette already exists at:\n{palettePath}\n\nDelete and recreate it?",
                "Recreate", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(palettePath);
        }

        EnsureFolderExists(PaletteFolder);

        // Load tile assets
        var tiles = AssetDatabase.FindAssets("t:Tile", new[] { TileAssetFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(t => t != null)
            .OrderBy(t => t.name, new NaturalStringComparer())
            .ToArray();

        if (tiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "No tile assets found. Run step 1 (Create Tile Assets) first.",
                "OK");
            return;
        }

        // Step 1: Create empty palette prefab with GridPalette embedded from the start
        var paletteGo = new GameObject(PaletteName);
        var grid = paletteGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        var tilemapGo = new GameObject("Layer1");
        tilemapGo.transform.SetParent(paletteGo.transform);
        tilemapGo.AddComponent<Tilemap>();
        tilemapGo.AddComponent<TilemapRenderer>();

        // Save empty prefab first
        PrefabUtility.SaveAsPrefabAsset(paletteGo, palettePath);
        Object.DestroyImmediate(paletteGo);

        // Embed GridPalette sub-asset so Unity recognizes this as a valid palette
        var gridPalette = ScriptableObject.CreateInstance<GridPalette>();
        gridPalette.name = "GridPalette";
        gridPalette.cellSizing = GridPalette.CellSizing.Automatic;
        AssetDatabase.AddObjectToAsset(gridPalette, palettePath);
        AssetDatabase.SaveAssets();

        // Step 2: Open the saved prefab and populate tiles
        var prefabRoot = PrefabUtility.LoadPrefabContents(palettePath);
        var tilemap = prefabRoot.GetComponentInChildren<Tilemap>();

        int columns = 20; // matches tileset's 320px / 16px layout
        for (int i = 0; i < tiles.Length; i++)
        {
            int x = i % columns;
            int y = -(i / columns);
            tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
        }
        tilemap.CompressBounds();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, palettePath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TilemapSetupTool] Tile palette created at {palettePath} with {tiles.Length} tiles.");
    }

    [MenuItem("Tools/Sunny Land Forest/3 - Create Scene Tilemap")]
    public static void CreateSceneTilemap()
    {
        // Check if a Grid already exists in the scene
        var existingGrid = Object.FindFirstObjectByType<Grid>();
        if (existingGrid != null)
        {
            if (!EditorUtility.DisplayDialog("Grid Already Exists",
                $"A Grid object '{existingGrid.name}' already exists in the scene.\n\nCreate another one?",
                "Yes, Create New", "Cancel"))
            {
                return;
            }
        }

        // Create Grid parent
        var gridGo = new GameObject("Grid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        Undo.RegisterCreatedObjectUndo(gridGo, "Create Tilemap Grid");

        // Create Ground tilemap child
        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridGo.transform);

        var tilemap = groundGo.AddComponent<Tilemap>();
        var renderer = groundGo.AddComponent<TilemapRenderer>();

        // Set sorting layer to "Ground" if it exists
        var sortingLayers = SortingLayer.layers.Select(l => l.name).ToArray();
        if (sortingLayers.Contains("Ground"))
        {
            renderer.sortingLayerName = "Ground";
        }

        // Add physics: TilemapCollider2D with CompositeCollider2D
        var tilemapCollider = groundGo.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var rb = groundGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        groundGo.AddComponent<CompositeCollider2D>();

        // Set layer to "Ground" (layer 7)
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            groundGo.layer = groundLayer;
        }
        else
        {
            Debug.LogWarning("[TilemapSetupTool] 'Ground' layer not found. Set the layer manually.");
        }

        // Select the new tilemap so user can start painting immediately
        Selection.activeGameObject = groundGo;

        Debug.Log("[TilemapSetupTool] Scene tilemap created: Grid > Ground (with TilemapCollider2D + CompositeCollider2D, Static Rigidbody2D, Ground layer).");
        Debug.Log("[TilemapSetupTool] Open Window > 2D > Tile Palette, select the SunnyLandForest palette, and start painting!");
    }

    // --- Helpers ---

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    /// <summary>
    /// Sorts strings with natural number ordering (tileset_2 before tileset_10).
    /// </summary>
    private class NaturalStringComparer : System.Collections.Generic.IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == null || b == null) return string.Compare(a, b);

            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                {
                    long na = 0, nb = 0;
                    while (ia < a.Length && char.IsDigit(a[ia]))
                        na = na * 10 + (a[ia++] - '0');
                    while (ib < b.Length && char.IsDigit(b[ib]))
                        nb = nb * 10 + (b[ib++] - '0');
                    if (na != nb) return na.CompareTo(nb);
                }
                else
                {
                    if (a[ia] != b[ib]) return a[ia].CompareTo(b[ib]);
                    ia++;
                    ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
