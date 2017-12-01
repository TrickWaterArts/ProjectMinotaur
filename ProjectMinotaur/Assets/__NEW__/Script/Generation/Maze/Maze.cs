﻿using System.Collections.Generic;
using UnityEngine;

public class Maze {

	public readonly int chunkSize;
	public readonly int mazeChunkWidth;
	public readonly int mazeChunkHeight;

	protected readonly IAlgorithm mazeAlgorithm;
	protected List<MazeChunk> chunks;

	public Maze(IAlgorithm mazeAlgorithm, int chunkSize, int mazeChunkWidth, int mazeChunkHeight) {
		this.mazeAlgorithm = mazeAlgorithm;
		this.chunkSize = chunkSize;
		this.mazeChunkWidth = mazeChunkWidth;
		this.mazeChunkHeight = mazeChunkHeight;
		chunks = new List<MazeChunk>();
	}

	// Prepopulate the maze with chunks.
	private void InitializeChunks() {
		for (int x = 0; x < mazeChunkWidth; x++) {
			for (int y = 0; y < mazeChunkWidth; y++) {
				AddChunk(x, y);
				MazeChunk chunk = GetChunk(x, y);
				if (chunk != null) {
					chunk.InitializeNodes();
				}
			}
		}
	}

	public MazeChunk GetChunk(int x, int y) {
		if (!InMaze(x, y)) {
			return null;
		}
		foreach (MazeChunk chunk in chunks) {
			if (chunk.GetPosition().GetX() == x && chunk.GetPosition().GetY() == y) {
				return chunk;
			}
		}
		return null;
	}

	public void AddChunk(int x, int y) {
		if (!InMaze(x, y)) {
			return;
		}
		if (GetChunk(x, y) != null) {
			return;
		}
		MazeChunk chunk = new MazeChunk(this, x, y, chunkSize);
		chunks.Add(chunk);
		chunk.InitializeNodes();
	}

	public bool InMaze(int x, int y) {
		return x >= 0 && x < mazeChunkWidth && y >= 0 && y < mazeChunkHeight;
	}

	public MazeNode GetNode(int x, int y) {
		int chunkX = Mathf.FloorToInt((float) x / (float) chunkSize);
		int chunkY = Mathf.FloorToInt((float) y / (float) chunkSize);
		MazeChunk chunk = GetChunk(chunkX, chunkY);
		if (chunk == null) {
			return null;
		}
		int inChunkX = 0;
		int inChunkY = 0;
		if (x != 0) {
			inChunkX = x % chunkSize;
		}
		if (y != 0) {
			inChunkY = y % chunkSize;
		}
		MazeNode node = chunk.GetNode(inChunkX, inChunkY);
		return node;
	}

	public IAlgorithm GetMazeGenerationAlgorithm() {
		return mazeAlgorithm;
	}

	public int GetSizeX() {
		return mazeChunkWidth * chunkSize;
	}

	public int GetSizeY() {
		return mazeChunkHeight * chunkSize;
	}

	// -- Actual Generation -- //

	public void Generate(MonoBehaviour executor, MazePos startingPoint) {
		PMEventSystem.GetEventSystem().TriggerEvent<EventChunkPrePopulationBegin>(new EventChunkPrePopulationBegin(this));
		InitializeChunks();
		PMEventSystem.GetEventSystem().TriggerEvent<EventChunkPrePopulationFinish>(new EventChunkPrePopulationFinish(this));

		for (int x = 0; x < mazeChunkWidth; x++) {
			for (int y = 0; y < mazeChunkWidth; y++) {
				MazeChunk chunk = GetChunk(x, y);
				PMEventSystem.GetEventSystem().TriggerEvent<EventChunkGenerationBegin>(new EventChunkGenerationBegin(this, chunk, x, y));
				if (chunk == null) {
					AddChunk(x, y);
				}
				if (chunk == null) {
					Debug.LogError("Chunk was null after creation: " + x + ", " + y + ". This is a major error.");
					return;
				}
				if (!chunk.IsInitialized()) {
					chunk.InitializeNodes();
				}
				PMEventSystem.GetEventSystem().TriggerEvent<EventChunkGenerationFinish>(new EventChunkGenerationFinish(this, chunk, x, y));
			}
		}

		Debug.Log("Generating maze using: " + mazeAlgorithm.GetName());
		executor.StartCoroutine(mazeAlgorithm.Generate(this, startingPoint));
	}

}