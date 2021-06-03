using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using TMPro;

public class HexTile
{
    public struct WorkableHeightData
    {
		public Workable WorkableObject;
		public int Height;
	}


	public const float WATER_LEVEL = -3f;
	public const int SIDES = 6;
	public const float HEIGHT_STEP = 0.1f;
	public const float OUTER_RADIUS = 1f;
    public const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;
	public readonly static Vector3[] CORNERS = {
		new Vector3(0f, 0f, OUTER_RADIUS),
		new Vector3(INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS),
		new Vector3(INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS),
		new Vector3(0f, 0f, -OUTER_RADIUS),
		new Vector3(-INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS),
		new Vector3(-INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS),
		new Vector3(0f, 0f, OUTER_RADIUS),
	};

	public HexCoordinates Coordinates;
	
	public int GlobalIndex { get; set; }

	private List<HexTile> _neighbors;
	public IReadOnlyList<HexTile> Neighbors => _neighbors ??= HexBoardChunkHandler.Instance.GetTileNeighbors_Uncached(this);

	private Dictionary<int, IReadOnlyList<HexTile>> _neighborCache = new Dictionary<int, IReadOnlyList<HexTile>>();
	
	public int Height { get; private set; }
	public HexBoard ParentBoard { get; set; }
	public int MaterialIndex { get; set; } = -1;
	public bool HeightLocked { get; set; }
	public bool WorkArea { get; set; }
	public bool HasWorkables { get { return buildingReferences.Count > 0; } }
	public Building BuildingOnTile { get; set; }
	public bool IsStorageTile { get; set; } = false;

	public HexTile(int x, int y, int index) {
		Position = new Vector3(
			(x + y * 0.5f - y / 2) * (INNER_RADIUS * 2f),
			0,
			y * (OUTER_RADIUS * 1.5f));

		Coordinates = HexCoordinates.FromOffsetCoordinates(x, y);
		GlobalIndex = index;
	}

	public IReadOnlyList<HexTile> GetTileNeighborsInDistance(int distance) {
		if (distance <= 0) {
			return Neighbors;
		}

		if (!_neighborCache.TryGetValue(distance, out var neighbors)) {
			neighbors = _neighborCache[distance] = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance_Uncached(this, distance);
		}

		return neighbors;
	}

	public bool CantWalkThrough
    {
        get
        {
			return HasEnvironmentalItems;
		}
    }

	public bool HasEnvironmentalItems
    {
        get
        {
			return environmentalObjectsOnTile != null && environmentalObjectsOnTile.Count > 0;
        }
	}
	private List<WorkableHeightData> buildingReferences = new List<WorkableHeightData>();
	private List<Workable> environmentalObjectsOnTile = new List<Workable>();
	public Vector3 Position { get; set; }

	public void AddWorkableToTile(Workable workable, int height)
    {
		buildingReferences.Add(new WorkableHeightData()
		{
			WorkableObject = workable,
			Height = height
		});
		workable.OnWorkFinished += (success) =>
		{
			RemoveWorkableFromTile(workable);
		};
    }

	public void RemoveWorkableFromTile(Workable workable)
    {
        for (int i = 0; i < buildingReferences.Count; i++)
        {
			if(buildingReferences[i].WorkableObject == workable)
            {
				buildingReferences.RemoveAt(i);
				return;
            }
        }
    }

	public Workable GetWorkable(bool top = true)
    {
		if(!HasWorkables)
        {
			return null;
        }

		if(top)
		{
			buildingReferences.Sort((x, y) => { return y.Height.CompareTo(x.Height); });
		}
		else
        {
			buildingReferences.Sort((x, y) => { return x.Height.CompareTo(y.Height); });
		}

		return buildingReferences[0].WorkableObject;
    }

	public void RemoveEnvironmentalItem(Workable obj)
	{ 
		environmentalObjectsOnTile.Remove(obj); 
	}

	public void AddEnvironmentItem(Workable obj)
    {
		Workable envWorkable = obj;
		envWorkable.OnWorkFinished += (success) => { if(success) environmentalObjectsOnTile.Remove(obj); };
		envWorkable.TilesAssociated = new HashSet<HexTile>() { this };
		environmentalObjectsOnTile.Add(obj);
    }

	public Workable[] GetEnvironmentalItemsAsWorkable()
    {
		List<Workable> workables = new List<Workable>();
        for (int i = 0; i < environmentalObjectsOnTile.Count; i++)
        {
			Workable envWorkable = environmentalObjectsOnTile[i];

			if(envWorkable != null)
			{
				workables.Add(envWorkable);
			}
        }

		return workables.ToArray();
    }

	public void SetHeight(int height)
    {
		if(!HeightLocked)
		{
			Height = height;
			Coordinates.Height = height;
		}
		//Vector3 pos = transform.localPosition;
		//pos.y = Height * HEIGHT_STEP;
		//transform.localPosition = pos;

		//Material mat = textureReference.GetMaterial(Height);
		//HexMesh.SetMaterials(mat, mat);
    }
}
