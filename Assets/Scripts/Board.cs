using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using DG.Tweening;
using UnityEngine.SubsystemsImplementation;
using DG.Tweening.Core;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    public Row[] rows;
    public Tile[,] Tiles { get; private set; }

    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new();
    private Coroutine resetCoroutine;
    private const float TweenDuration = 0.25f;

    [SerializeField] private AudioClip popTiles, usePowerup;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button level1Next, level2Next;

    #region Unity Lifecycle

    void Awake() => Instance = this;

    void Start()
    {
        // Initialize tile grid based on row data
        Tiles = new Tile[rows.Max(r => r.tiles.Length), rows.Length];

        // Fill grid with random items
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = rows[y].tiles[x];
                tile.x = x;
                tile.y = y;
                tile.Item = GetRandomItem();
                Tiles[x, y] = tile;
            }
        }

        // Prevent pre-existing matches on start
        RemoveInitialMatches();
    }

    void Update()
    {
        // Handle level progress-based UI logic
        var score = SaveManager.Instance.Load();

        if (SceneManager.GetActiveScene().name == "Level 1")
            level1Next.gameObject.SetActive(score >= 350);
        else if (SceneManager.GetActiveScene().name == "Level 2")
            level2Next.gameObject.SetActive(score >= 700);
    }

    #endregion

    #region Tile Selection and Swapping

    public async void Select(Tile tile)
    {
        // Only allow selection of neighbors or first tile
        if (!_selection.Contains(tile))
        {
            if (_selection.Count > 0)
            {
                if (Array.IndexOf(_selection[0].Neighbors, tile) != -1)
                    _selection.Add(tile);
            }
            else
            {
                _selection.Add(tile);
            }
        }

        if (_selection.Count < 2) return;

        Debug.Log($"Selected tiles at ({_selection[0].x}, {_selection[0].y}) and ({_selection[1].x}, {_selection[1].y})");

        await Swap(_selection[0], _selection[1]);

        // Check for special tile effect
        if (IsSpecial(_selection[0]) || IsSpecial(_selection[1]))
        {
            var specialTile = IsSpecial(_selection[0]) ? _selection[0] : _selection[1];
            await PopSquare(specialTile.x, specialTile.y);
            _selection.Clear();
            return;
        }

        // Handle normal pop or revert swap
        if (CanPop())
            await Pop();
        else
            await Swap(_selection[0], _selection[1]);

        _selection.Clear();
    }

    public async void Select(Tile a, Tile b)
    {
        // Direct selection method (e.g. by AI or touch drag)
        _selection.Clear();
        _selection.Add(a);
        _selection.Add(b);

        await Swap(a, b);

        if (IsSpecial(a) || IsSpecial(b))
        {
            var special = IsSpecial(a) ? a : b;
            await PopSquare(special.x, special.y);
            return;
        }

        if (CanPop())
            await Pop();
        else
            await Swap(a, b);

        // Cascading pops
        while (CanPop())
            await Pop();
    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        // Tween-based icon swapping
        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        var icon1Transform = icon1.transform;
        var icon2Transform = icon2.transform;

        var sequence = DOTween.Sequence();
        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration))
                .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        // Swap item data
        (tile2.Item, tile1.Item) = (tile1.Item, tile2.Item);
    }

    #endregion

    #region Matching and Popping

    private bool CanPop()
    {
        // Check if any tile has a match
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (Tiles[x, y].GetConnectedTiles().Skip(1).Count() >= 2)
                    return true;

        return false;
    }

    private async Task Pop()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = Tiles[x, y];
                var connectedTiles = tile.GetConnectedTiles();

                if (connectedTiles.Skip(1).Count() < 2) continue;

                ComboManager.Instance.ComboIndex++;

                // Deflate animation
                var deflateSequence = DOTween.Sequence();
                foreach (var t in connectedTiles)
                    deflateSequence.Join(t.icon.transform.DOScale(Vector3.zero, TweenDuration));

                audioSource.PlayOneShot(popTiles);
                await deflateSequence.Play().AsyncWaitForCompletion();

                ScoreCounter.Instance.Score += tile.Item.value * connectedTiles.Count;

                // Refill and animate inflate
                var inflateSequence = DOTween.Sequence();
                int i = 0;
                foreach (var t in connectedTiles)
                {
                    t.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                    if (connectedTiles.Skip(1).Count() >= 4 && i == 0)
                        t.Item = ItemDatabase.SpecialItems[0];

                    inflateSequence.Join(t.icon.transform.DOScale(Vector3.one, TweenDuration));
                    i++;
                }

                await inflateSequence.Play().AsyncWaitForCompletion();

                // Restart pop loop
                x = y = 0;
            }
        }

        ComboManager.Instance.ComboIndex = -3;

        // Handle dead board
        if (!HasPossibleMove())
        {
            Debug.Log("No possible moves! Shuffling board...");
            ShuffleBoard();
        }
    }

    private async Task PopSquare(int centerX, int centerY)
    {
        var toPop = new List<Tile>();

        // Gather 3x3 grid around center
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                if (x >= 0 && x < Width && y >= 0 && y < Height)
                    toPop.Add(Tiles[x, y]);
            }
        }

        var deflate = DOTween.Sequence();
        foreach (var t in toPop)
            deflate.Join(t.icon.transform.DOScale(Vector3.zero, TweenDuration));

        audioSource.PlayOneShot(usePowerup);
        await deflate.Play().AsyncWaitForCompletion();

        foreach (var t in toPop)
            ScoreCounter.Instance.Score += t.Item.value;

        var inflate = DOTween.Sequence();
        foreach (var t in toPop)
        {
            t.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
            inflate.Join(t.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await inflate.Play().AsyncWaitForCompletion();

        if (CanPop()) await Pop();
    }

    #endregion

    #region Board Management

    private void RemoveInitialMatches()
    {
        bool foundMatch;

        do
        {
            foundMatch = false;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var tile = Tiles[x, y];
                    var connected = tile.GetConnectedTiles();

                    if (connected.Count >= 3)
                    {
                        foundMatch = true;

                        // Replace until not matched
                        do
                        {
                            tile.Item = GetRandomItem();
                            connected = tile.GetConnectedTiles();
                        }
                        while (connected.Count >= 3);
                    }
                }
            }

        } while (foundMatch);
    }

    private void ShuffleBoard()
    {
        List<Item> allItems = new();

        // Collect all tile items
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                allItems.Add(Tiles[x, y].Item);

        // Randomize and reassign
        do
        {
            allItems = allItems.OrderBy(_ => UnityEngine.Random.value).ToList();

            int index = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Tiles[x, y].Item = allItems[index++];

        } while (!HasPossibleMove());
    }

    private bool HasPossibleMove()
    {
        // Check if any swap can form a match
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tile tile = Tiles[x, y];

                foreach (Tile neighbor in tile.Neighbors)
                {
                    if (neighbor == null) continue;

                    (tile.Item, neighbor.Item) = (neighbor.Item, tile.Item);

                    bool wouldMatch = tile.GetConnectedTiles().Count >= 3 || neighbor.GetConnectedTiles().Count >= 3;

                    (tile.Item, neighbor.Item) = (neighbor.Item, tile.Item);

                    if (wouldMatch) return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Helpers

    private bool IsSpecial(Tile t) => ItemDatabase.SpecialItems.Contains(t.Item);

    private Item GetRandomItem() => ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

    IEnumerator DisappearAfterSometime()
    {
        yield return new WaitForSeconds(3f);
        ComboManager.Instance.ComboIndex = -3;
    }

    #endregion

    #region Scene Controls

    public void BackToMainMenu() => SceneManager.LoadScene("Main Menu");

    public void NextLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    #endregion
}
