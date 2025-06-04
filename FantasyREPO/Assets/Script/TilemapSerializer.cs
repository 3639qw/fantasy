using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

public class TilemapSerializer : MonoBehaviour
{
    
    [Header("저장할 타일맵")]
    public string saveFileName = "tilemap_save.json"; // 저장 파일명
    private Dictionary<string, TileBase> tileDict; // Resources디렉토리에 있는 타일전체를 저장할 Dic

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Z))
            SaveTilemapToJson();
        
        if(Input.GetKeyDown(KeyCode.X))
            LoadTilemapFromJson();
        
    }


    // 저장
    protected internal void SaveTilemapToJson()
    {
        var bounds = this.GetComponent<Farming>().farmLand.cellBounds;
        var seedBounds = this.GetComponent<Farming>().seedLand.cellBounds;
        TileSaveDataList dataList = new();

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = this.GetComponent<Farming>().farmLand.GetTile(pos);
            if (tile != null)
            {
                dataList.tiles.Add(new TileSaveData
                {
                    tileName = tile.name,
                    x = pos.x,
                    y = pos.y,
                    z = pos.z
                });
            }
        }

        foreach (var pos in seedBounds.allPositionsWithin)
        {
            TileBase tile = this.GetComponent<Farming>().seedLand.GetTile(pos);
            if (tile != null)
            {
                dataList.seed.Add(new TileSaveData
                {
                    tileName = tile.name,
                    x = pos.x,
                    y = pos.y,
                    z = pos.z
                });
            }
        }

        string json = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Tilemap saved to: {SavePath}");
    }

    // 복원
    protected internal void LoadTilemapFromJson()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No tilemap save file found.");
            return;
        }

        Farming farming = this.GetComponent<Farming>();
        if (farming == null)
        {
            Debug.LogError("Farming 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        if (farming.farmLand == null)
        {
            Debug.LogError("farmLand가 할당되어 있지 않습니다. 할당 후 시도하세요.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        TileSaveDataList dataList = JsonUtility.FromJson<TileSaveDataList>(json);

        farming.farmLand.ClearAllTiles();
        farming.seedLand.ClearAllTiles();

        if (tileDict == null)
        {
            Debug.LogWarning("tileDict가 비어있음. LoadAllTiles() 호출 필요.");
            LoadAllTiles();
        }

        foreach (var tileData in dataList.tiles)
        {
            Vector3Int pos = new(tileData.x, tileData.y, tileData.z);
            if (tileDict.TryGetValue(tileData.tileName, out TileBase tile))
            {
                farming.farmLand.SetTile(pos, tile);
            }
            else
            {
                Debug.LogWarning($"❌ Dictionary에서 타일 '{tileData.tileName}' 을(를) 찾을 수 없음.");
            }
        }

        foreach (var tileData in dataList.seed)
        {
            Vector3Int pos = new(tileData.x, tileData.y, tileData.z);
            if (tileDict.TryGetValue(tileData.tileName, out TileBase tile))
            {
                farming.seedLand.SetTile(pos, tile);
            }
            else
            {
                Debug.LogWarning($"❌ Dictionary에서 타일 '{tileData.tileName}' 을(를) 찾을 수 없음.");
            }
            
        }

        Debug.Log("Tilemap loaded from JSON.");
    }

    
    // 전체 타일을 로딩
    protected internal void LoadAllTiles()
    {
        tileDict = new Dictionary<string, TileBase>();

        // Resources 폴더 내부의 모든 TileBase 자산 불러오기
        TileBase[] allTiles = Resources.LoadAll<TileBase>("Tileset");

        foreach (TileBase tile in allTiles)
        {
            if (!tileDict.ContainsKey(tile.name))
            {
                tileDict.Add(tile.name, tile);
            }
            else
            {
                Debug.LogWarning($"⚠️ 중복된 타일 이름 발견: {tile.name}");
            }
        }

        Debug.Log($"✅ {tileDict.Count}개의 타일 로드됨.");
    }
    
    
    
    [System.Serializable]
    public class TileSaveData
    {
        public string tileName;
        public int x, y, z;
    }

    [System.Serializable]
    public class TileSaveDataList
    {
        public List<TileSaveData> tiles = new();
        public List<TileSaveData> seed = new();
    }
}
