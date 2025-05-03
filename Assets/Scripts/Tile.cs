using UnityEngine;
using UnityEngine.EventSystems;  // ← makes IPointerDownHandler & PointerEventData available
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class Tile : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public int x, y;

    public Image icon;
    public Button button;

    // swipe detection
    private Vector2 _pointerDownPos;
    private const float MinSwipeDist = 30f;

    // shortcut neighbors
    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1] : null;
    public Tile Right => x < Board.Instance.Width - 1 ? Board.Instance.Tiles[x + 1, y] : null;
    public Tile Bottom => y < Board.Instance.Height - 1 ? Board.Instance.Tiles[x, y + 1] : null;
    public Tile[] Neighbors => new[] { Left, Top, Right, Bottom };

    // the item backing this tile
    private Item _item;
    public Item Item
    {
        get => _item;
        set
        {
            if (_item == value) return;
            _item = value;
            icon.sprite = _item.sprite;
        }
    }

    void Start()
    {
        // click-to-select still works
        button.onClick.AddListener(() => Board.Instance.Select(this));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownPos = eventData.pressPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _pointerDownPos;
        if (delta.magnitude < MinSwipeDist)
            return;

        // choose direction
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            // horizontal
            if (delta.x > 0 && Right != null) Board.Instance.Select(this, Right);
            if (delta.x < 0 && Left != null) Board.Instance.Select(this, Left);
        }
        else
        {
            // vertical
            if (delta.y > 0 && Top != null) Board.Instance.Select(this, Top);
            if (delta.y < 0 && Bottom != null) Board.Instance.Select(this, Bottom);
        }
    }

    public List<Tile> GetConnectedTiles(List<Tile> exclude = null)
    {
        var result = new List<Tile> { this };
        exclude = exclude ?? new List<Tile>();
        exclude.Add(this);

        foreach (var neighbor in Neighbors)
        {
            if (neighbor == null || exclude.Contains(neighbor) || neighbor.Item != Item)
                continue;

            result.AddRange(neighbor.GetConnectedTiles(exclude));
        }

        return result;
    }
}
