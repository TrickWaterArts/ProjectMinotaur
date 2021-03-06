﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazeHandler : MonoBehaviour {

	public static IAlgorithm MAZE_ALGORITHM = new EllersMaze();

	public int chunkSize = 16;
	public int chunksX = 4;
	public int chunksY = 4;
	public float pathWidth = 15.0f;
	public float pathSpread = 30.0f;
	public float pathHeight = 25.0f;
	public float distanceVariability = 7.4f;
	public string chunkPrefabName = "MazeRenderedChunk";
	public GameObject[] nearnessObjects;
	public float updateInterval = 1.0f;
	public float chunkUnloadDistancePadding = 100.0f;
	public bool addLights;

	public bool loadDebug = true;
	public Text infoText;

	private GameObject chunkPrefab;
	private Maze maze;
	private float loadTimer;
	public bool Rendering { private set; get; }
	public bool Generated { private set; get; }

	private Dictionary<MazePos, MazeRenderedChunk> loadedChunks;

	void Start() {
		if (chunkPrefabName == null) {
			Debug.LogError("Couldn't load chunk prefab, name is null.");
			return;
		}
		chunkPrefabName = chunkPrefabName.Trim();
		if (chunkPrefabName.Length == 0) {
			Debug.LogError("Couldn't load chunk prefab, name is empty.");
			return;
		}
		chunkPrefab = Resources.Load<GameObject>(chunkPrefabName);
		if (chunkPrefab == null) {
			Debug.LogError("Couldn't load chunk prefab, file was missing or corrupted.");
			return;
		}
		Debug.Log("Loaded chunk prefab.");
		loadedChunks = new Dictionary<MazePos, MazeRenderedChunk>();
	}

	public void Generate(System.Action<float> progress, LoadingStepComplete done, bool newMaze, bool trueGen) {
		Rendering = true;
		loadedChunks = new Dictionary<MazePos, MazeRenderedChunk>();
		if (newMaze) {
			maze = new Maze(MAZE_ALGORITHM, chunkSize, chunksX, chunksY, distanceVariability);
		}
		GenerateMaze(progress, done, trueGen);
	}

	public void Load(System.Action<float> progress, LoadingStepComplete done, Maze maze) {
		this.maze = maze;
		Generate(progress, done, false, false);
	}

	void Update() {
		if (Generated) {
			if (infoText != null) {
				infoText.text = "Project Minotaur 0.0.1" + ((GameStateHandler.Instance.devReleaseMode) ? "-alpha" : "");
				infoText.text += "\n\nLoaded chunks: " + loadedChunks.Count;
				infoText.text += "\nGenerated chunks: " + chunksX * chunksY;
				infoText.text += "\nNodes per chunk: " + chunkSize + "x" + chunkSize;
				infoText.text += "\nGenerated nodes: " + maze.GetSizeX() * maze.GetSizeY();
				infoText.text += "\nSize: " + maze.GetSizeX() + "x" + maze.GetSizeY();
				infoText.text += "\nChunks: " + chunksX + "x" + chunksY;
				infoText.text += "\nChunk loader check in " + (updateInterval - loadTimer).ToString("0.00") + "s";
				infoText.text += "\nPath width: " + pathWidth;
				infoText.text += "\nPath height: " + pathHeight;
				infoText.text += "\nNode interval: " + pathSpread;
				if (GameStateHandler.Instance.devReleaseMode && GameStateHandler.Instance.State.Equals(GameState.INGAME)) {
					infoText.text += "\n\nMovement: WASD and Mouse";
					infoText.text += "\n  Press F to toggle flight";
					infoText.text += "\n  Tap spacebar to jump or move up";
					infoText.text += "\n  Hold leftcontrol to move down while flying";
					infoText.text += "\n  Hold leftshift to speed up movement";
				}
			}
			loadTimer += Time.deltaTime;
			if (loadTimer >= updateInterval) {
				loadTimer = 0;
				foreach (GameObject obj in nearnessObjects) {
					if (loadDebug) {
						Debug.DrawLine(obj.transform.position, obj.transform.position + Vector3.up * 200.0f, Color.red, updateInterval / 2.0f, true);
					}
					StartCoroutine(RenderMazeAroundObject(obj));
				}
			}
		}
	}

	public MazePos GetWorldPosAsNode(Vector3 pos) {
		int x = Mathf.FloorToInt(pos.x);
		int y = Mathf.FloorToInt(pos.z);
		return new MazePos(x, y);
	}

	public IEnumerator RenderMazeAroundObject(GameObject obj) {
		if (Rendering || obj == null || !obj.activeSelf || !obj.activeInHierarchy || !Generated) {
			yield break;
		}
		Rendering = true;
		List<MazePos> toDestroy = new List<MazePos>();
		List<MazePos> keys = new List<MazePos>(loadedChunks.Keys);
		foreach (MazePos pos in keys) {
			if (!loadedChunks.ContainsKey(pos)) {
				continue;
			}
			if (!ShouldBeLoadedByObj(obj, pos) && !toDestroy.Contains(pos)) {
				toDestroy.Add(pos);
				if (loadDebug) {
					Vector3 center = GetCenterWorldPosOfChunk(pos);
					Debug.DrawLine(center, center + Vector3.up * 200.0f, Color.magenta, updateInterval / 2.0f, true);
				}
			}
			yield return null;
		}
		foreach (MazePos pos in toDestroy) {
			RemoveChunk(pos);
		}
		for (int x = 0; x < chunksX; x++) {
			for (int y = 0; y < chunksY; y++) {
				if (loadedChunks.ContainsKey(new MazePos(x, y)) || !ShouldBeLoadedByObj(obj, new MazePos(x, y))) {
					continue;
				}
				RenderChunk(new MazePos(x, y));
				if (loadDebug) {
					Vector3 center = GetCenterWorldPosOfChunk(new MazePos(x, y));
					Debug.DrawLine(center, center + Vector3.up * 200.0f, Color.green, updateInterval / 2.0f, true);
				}
				yield return null;
			}
		}
		Rendering = false;
	}

	private float GetDistSqrd(Vector3 fromP, Vector3 toP) {
		return Mathf.Pow(fromP.x - toP.x, 2) + Mathf.Pow(fromP.y - toP.y, 2) + Mathf.Pow(fromP.z - toP.z, 2);
	}

	public float GetRequiredUnloadDistance() {
		return (GetChunkWorldSize() * 3.0f / 4.0f) + chunkUnloadDistancePadding;
	}

	private bool ShouldBeLoadedByObj(GameObject obj, MazePos pos) {
		Vector3 center = GetCenterWorldPosOfChunk(pos);
		float dist = GetDistSqrd(center, obj.transform.position);
		if (dist >= Mathf.Pow(GetRequiredUnloadDistance(), 2)) {
			return false;
		}
		return true;
	}

	public void Clear() {
		Generated = false;
		maze.Destroy();
		List<MazePos> chunks = new List<MazePos>(loadedChunks.Keys);
		foreach (MazePos pos in chunks) {
			RemoveChunk(pos);
		}
		loadedChunks.Clear();
	}

	private void RemoveChunk(MazePos pos) {
		if (!loadedChunks.ContainsKey(pos)) {
			return;
		}
		MazeRenderedChunk chunk = loadedChunks[pos];
		chunk.destroyed = true;
		loadedChunks.Remove(pos);
		Destroy(chunk.gameObject);
	}

	private void RenderChunk(MazePos pos) {
		if (!Generated) {
			return;
		}
		GameObject instance = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
		instance.transform.name = "Chunk: " + pos;
		instance.transform.parent = transform;
		MazeRenderedChunk chunkObj = instance.GetComponent<MazeRenderedChunk>();
		if (chunkObj == null) {
			Debug.LogError("Failed to get maze chunk on chunk: " + pos);
			return;
		}
		chunkObj.addLights = addLights;
		if (!loadedChunks.ContainsKey(pos)) {
			loadedChunks.Add(pos, chunkObj);
		} else {
			loadedChunks[pos] = chunkObj;
		}
		StartCoroutine(chunkObj.Render(this, maze.GetChunk(pos.GetX(), pos.GetY())));
	}

	public Maze GetMaze() {
		return maze;
	}

	private System.Action<float> progress;
	private LoadingStepComplete done;
	private void GenerateMaze(System.Action<float> progress, LoadingStepComplete done, bool trueGen) {
		this.progress = progress;
		this.done = done;
		
		PMEventSystem.GetEventSystem().RemoveListener<EventMazeGenerationBegin>(MazeBegin);
		PMEventSystem.GetEventSystem().RemoveListener<EventMazeGenerationFinish>(MazeFinish);
		PMEventSystem.GetEventSystem().RemoveListener<EventMazeGenerationUpdate>(MazeUpdate);
		PMEventSystem.GetEventSystem().RemoveListener<ItemSpawnEvent>(ItemSpawn);

		PMEventSystem.GetEventSystem().AddListener<EventMazeGenerationBegin>(MazeBegin);
		PMEventSystem.GetEventSystem().AddListener<EventMazeGenerationFinish>(MazeFinish);
		PMEventSystem.GetEventSystem().AddListener<EventMazeGenerationUpdate>(MazeUpdate);
		PMEventSystem.GetEventSystem().AddListener<ItemSpawnEvent>(ItemSpawn);

		maze.Generate(this, new MazePos(chunksX * chunkSize / 2, chunksY * chunkSize / 2), trueGen);
	}

	private void MazeBegin<T>(T e) where T : EventMazeGenerationBegin {
		Rendering = true;
		e.IsCancellable();
		Generated = false;
		if (infoText != null) {
			infoText.text = "Generating maze... ";
		}
	}

	private void MazeFinish<T>(T e) where T : EventMazeGenerationFinish {
		e.ToString();
		Generated = true;
		if (infoText != null) {
			infoText.text = "Finished generating.";
		}
		done.Invoke();
		Rendering = false;
	}

	private void MazeUpdate<T>(T e) where T : EventMazeGenerationUpdate {
		if (infoText != null) {
			progress.Invoke(e.GetProgress());
			infoText.text = "Generating maze: " + (e.GetProgress() * 100.0f).ToString("00.00") + "%";
		}
	}

	private void ItemSpawn<T>(T e) where T : ItemSpawnEvent {
		if (infoText != null && e.Item != null && !e.Item.Stack.IsEmpty) {
			infoText.text = "Item: " + e.Item.transform.name + " at " + e.Item.transform.position;
		}
	}

	// Will return the top(in the world, bottom) left corner of the node at the specified y position
	public Vector3 GetWorldPosOfNode(MazePos pos, float y) {
		MazeNode atNode = maze.GetNode(pos.GetX(), pos.GetY());
		if (atNode == null) {
			return Vector3.zero;
		}
		pos = atNode.GetGlobalPos();
		return new Vector3(pos.GetX() * (pathWidth + pathSpread), y, pos.GetY() * (pathWidth + pathSpread)) + atNode.GetWorldOffset();
	}

	// Will return the floored node (if the pos is in a hallway, it will return the node with a lower x and y/z value)
	// Example:
	// K y y K
	// x x x z
	// x x x z
	// K x x K
	// Each letter that is not K belongs to the K that is below it and/or left of it.
	//   So: If the passed position falls on any of the x's, then the node position returned will be the lower left K.
	public MazePos GetMazeNodePosFromWorld(Vector3 pos) {
		float x = pos.x / (pathWidth + pathSpread);
		float y = pos.z / (pathWidth + pathSpread);
		return new MazePos(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
	}

	public Vector2 GetMazeWorldSize() {
		float w = (chunksX * GetChunkWorldSize()) - pathSpread;
		float h = (chunksY * GetChunkWorldSize()) - pathSpread;
		return new Vector2(w, h);
	}

	public float GetChunkWorldSize() {
		return chunkSize * (pathSpread + pathWidth);
	}

	// Provided with the chunk coords of the chunk
	public Vector3 GetChunkWorldPos(MazePos chunkPos) {
		float between = GetChunkWorldSize();
		return new Vector3(chunkPos.GetX() * between, 0.0f, chunkPos.GetY() * between);
	}

	public Vector3 GetCenterWorldPosOfChunk(MazePos chunkPos) {
		Vector3 chunk = GetChunkWorldPos(chunkPos);
		float sizeHalf = GetChunkWorldSize() / 2.0f;
		chunk.x += sizeHalf;
		chunk.z += sizeHalf;
		return chunk;
	}

}

public class MazeRenderEvent : IPMEvent {

	private readonly Maze maze;

	public MazeRenderEvent(Maze maze) {
		this.maze = maze;
	}

	public string GetName() {
		return GetType().Name;
	}

	public bool IsCancellable() {
		return false;
	}

	public bool IsCancelled() {
		return false;
	}

	public void Cancel() {
	}

	public Maze GetMaze() {
		return maze;
	}

}

public class EventMazeRenderChunkBegin : MazeRenderEvent {

	private readonly MazeChunk chunk;

	public EventMazeRenderChunkBegin(Maze maze, MazeChunk chunk) : base(maze) {
		this.chunk = chunk;
	}

	public MazeChunk GetChunk() {
		return chunk;
	}

}

public class EventMazeRenderChunkFinish : MazeRenderEvent {

	private readonly MazeChunk chunk;

	public EventMazeRenderChunkFinish(Maze maze, MazeChunk chunk) : base(maze) {
		this.chunk = chunk;
	}

	public MazeChunk GetChunk() {
		return chunk;
	}

}