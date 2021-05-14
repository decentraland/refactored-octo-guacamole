using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LandWithAccess
{
    public string id => rawData.id;
    public LandType type => rawData.type;
    public LandRole role => rawData.role;
    public int size => rawData.size;
    public string name => rawData.name;
    public string owner => rawData.owner;

    public List<DeployedScene> scenes;
    public Vector2Int[] parcels;
    public Vector2Int baseCoords;
    public Land rawData;

    public LandWithAccess(Land land)
    {
        rawData = land;
        parcels = land.parcels.Select(parcel => new Vector2Int(parcel.x, parcel.y)).ToArray();
        baseCoords = land.type == LandType.PARCEL ? new Vector2Int(land.x, land.y) : parcels[0];
    }
}