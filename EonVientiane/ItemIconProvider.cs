using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace EonVientiane;

public interface IItemIconRenderer
{
    void Draw(SpriteBatch spriteBatch, Rectangle destination, Color tint, float timeSeconds);
}

public class ItemIconProvider
{
    private static readonly string[] _supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
    private readonly ContentManager _content;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IItemIconRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);

    public Func<float> TimeProvider { get; set; } = () => 0f;

    public ItemIconProvider(ContentManager content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public void RegisterRenderer(string key, IItemIconRenderer renderer)
    {
        if (string.IsNullOrWhiteSpace(key) || renderer == null)
        {
            return;
        }

        _renderers[key] = renderer;
    }

    public bool TryDrawIcon(SpriteBatch spriteBatch, Item item, Rectangle destination, Color? tint = null)
    {
        if (spriteBatch == null || item == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(item.IconRendererKey) && _renderers.TryGetValue(item.IconRendererKey, out var renderer))
        {
            renderer.Draw(spriteBatch, destination, tint ?? Color.White, TimeProvider());
            return true;
        }

        var texture = GetIconTexture(item);
        if (texture == null)
        {
            return false;
        }

        spriteBatch.Draw(texture, destination, tint ?? Color.White);
        return true;
    }

    private Texture2D GetIconTexture(Item item)
    {
        var assetName = item.IconAsset?.Trim();
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        if (_missingAssets.Contains(assetName))
        {
            return null;
        }

        if (_textureCache.TryGetValue(assetName, out var cachedTexture))
        {
            return cachedTexture;
        }

        if (!AssetFileExists(assetName))
        {
            _missingAssets.Add(assetName);
            return null;
        }

        try
        {
            var texture = _content.Load<Texture2D>(assetName);
            _textureCache[assetName] = texture;
            return texture;
        }
        catch
        {
            _missingAssets.Add(assetName);
            return null;
        }
    }

    private bool AssetFileExists(string assetName)
    {
        foreach (var baseDir in GetSearchRoots())
        {
            var root = Path.Combine(baseDir, _content.RootDirectory ?? "Content");
            if (!Directory.Exists(root))
            {
                continue;
            }

            var assetWithExtension = Path.Combine(root, assetName);
            if (File.Exists(assetWithExtension))
            {
                return true;
            }

            foreach (var ext in _supportedExtensions)
            {
                if (File.Exists(assetWithExtension + ext))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return AppContext.BaseDirectory;
        yield return Environment.CurrentDirectory;
    }
}
