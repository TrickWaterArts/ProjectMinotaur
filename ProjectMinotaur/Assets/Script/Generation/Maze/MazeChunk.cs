﻿using System.Collections.Generic;
using UnityEngine;

public class MazeChunk {

	private readonly int chunkSize;
	private readonly MazePos pos;
	private bool initialized;
	protected MazeNode[] nodes;

	public MazeChunk(int x, int y, int chunkSize) {
		pos = new MazePos(x, y);
		this.chunkSize = chunkSize;
		nodes = new MazeNode[chunkSize * chunkSize];
	}

	public MazeChunk(int x, int y, int chunkSize, MazeNode[] nodes) : this(x, y, chunkSize) {
		this.nodes = nodes;
	}

	public string GetString() {
		string output = pos.GetX() + "x" + pos.GetY() + "x";
		foreach (MazeNode node in nodes) {
			output += node.GetPosition().GetX() + "y" + node.GetPosition().GetY() + "y" + node.GetWorldOffset().x + "," + node.GetWorldOffset().z + "y" + node.GetWallData() + "^";
		}
		return output.Substring(0, output.Length - 1);
	}

	public MazeNode GetNode(int x, int y) {
		if (!InChunk(x, y)) {
			return null;
		}
		return nodes[chunkSize * y + x];
	}

	public void AddNode(int x, int y, float variability) {
		if (!InChunk(x, y)) {
			return;
		}
		if (GetNode(x, y) != null) {
			return;
		}
		Vector3 off = new Vector3(Util.NextRand((int) (-100.0f * variability), (int) (100.0f * variability)) / 100.0f, 0.0f, Util.NextRand((int) (-100.0f * variability), (int) (100.0f * variability)) / 100.0f);
		nodes[chunkSize * y + x] = new MazeNode(x, y, pos.GetX() * chunkSize + x, pos.GetY() * chunkSize + y, off);
	}

	// Prepopulates the chunk with empty nodes.
	public void InitializeNodes(float variability) {
		for (int x = 0; x < chunkSize; x++) {
			for (int y = 0; y < chunkSize; y++) {
				AddNode(x, y, variability);
			}
		}
		initialized = true;
	}

	public bool InChunk(int x, int y) {
		return x >= 0 && x < chunkSize && y >= 0 && y < chunkSize;
	}

	public MazePos GetGlobalPosition(int x, int y) {
		return new MazePos(chunkSize * pos.GetX() + x, chunkSize * pos.GetY() + y);
	}

	public bool IsInitialized() {
		return initialized;
	}

	public MazePos GetPosition() {
		return pos;
	}

	public int GetSize() {
		return chunkSize;
	}

}