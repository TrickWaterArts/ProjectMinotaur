﻿using System;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerate : MonoBehaviour {

	public int width = 10;
	public int height = 10;
	public bool IsBuilt { private set; get; }

	private MazeCell sideWall;
	private MazeCell[] cells;

	void Start() {
		Generate();
	}

	public void Generate() {
		print("Generating maze...");
		long startTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

		InitVariables();
		Stack<CellPosition> stack = new Stack<CellPosition>();
		int[] keys = new int[] { 0, 1, 2, 3 }; // Up, down, left, right
		int seed = 103737;
		IsBuilt = CarveTo(0, 0, stack, keys, ref seed);

		// Generate rooms and such.

		long endTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		print("Generated maze in " + (endTimeMs - startTimeMs) + "ms.");
	}

	private bool CarveTo(int x, int y, Stack<CellPosition> stack, int[] keys, ref int seed) {
		MazeCell cell = GetCell(x, y);

		if (cell.init) {
			stack.Pop();
			CellPosition next = stack.Pop();
			GetCell(next.x, next.y).init = false;
			if (stack.Count > 0) {
				return CarveTo(next.x, next.y, stack, keys, ref seed);
			}
			return true;
		}

		cell.init = true;
		stack.Push(new CellPosition(x, y));

		MazeCell[] neighbors = GetNeighbors(x, y);
		Shuffle(ref keys, ref seed);
		int check = 0;
		int rand = 0;

		while (check ++ < keys.Length) {
			rand = keys[check - 1];

			switch (rand) {
				case 0:
					if (!neighbors[0].init) {
						ClearWalls(x, y, MazeCell.UP);
						ClearWalls(x, y - 1, MazeCell.DOWN);
						y --;
						check = keys.Length;
					}
					break;
				case 1:
					if (!neighbors[1].init) {
						ClearWalls(x, y, MazeCell.DOWN);
						ClearWalls(x, y + 1, MazeCell.UP);
						y ++;
						check = keys.Length;
					}
					break;
				case 2:
					if (!neighbors[2].init) {
						ClearWalls(x, y, MazeCell.LEFT);
						ClearWalls(x - 1, y, MazeCell.RIGHT);
						x --;
						check = keys.Length;
					}
					break;
				case 3:
					if (!neighbors[3].init) {
						ClearWalls(x, y, MazeCell.RIGHT);
						ClearWalls(x + 1, y, MazeCell.LEFT);
						x ++;
						check = keys.Length;
					}
					break;
			}
		}

		return CarveTo(x, y, stack, keys, ref seed);
	}

	private void Shuffle(ref int[] array, ref int seed) {
		//BetterRandom r = new BetterRandom();
		for (int i = array.Length; i > 0; i --) {
			int j = BetterRandom.Between(0, i - 1, seed);
			seed ++;
			//int j = r.Next(i - 1);
			int k = array[j];
			array[j] = array[i - 1];
			array[i - 1] = k;
		}
	}

	private void InitVariables() {
		sideWall = new MazeCell();
		sideWall.init = true;
		sideWall.walls = 0x1111;
		cells = new MazeCell[width * height];
		for (int i = 0; i < cells.Length; i ++) {
			cells[i] = new MazeCell();
		}
	}

	public MazeCell[] GetNeighbors(int x, int y) {
		MazeCell[] neighbors = new MazeCell[4];
		neighbors[0] = ((y > 0) ? (GetCell(x, y - 1)) : (sideWall));
		neighbors[1] = ((y < height - 1) ? (GetCell(x, y + 1)) : (sideWall));
		neighbors[2] = ((x > 0) ? (GetCell(x - 1, y)) : (sideWall));
		neighbors[3] = ((x < width - 1) ? (GetCell(x + 1, y)) : (sideWall));
		return neighbors;
	}

	public MazeCell GetCell(int x, int y) {
		int i = y * width + x;
		if (x < 0 || x > width - 1 || y < 0 || y > height - 1 || i < 0 || i >= cells.Length) {
			return null;
		}
		return cells[i];
	}

	public void SetWalls(int x, int y, int walls) {
		MazeCell cell = GetCell(x, y);
		if (cell != null) {
			cell.walls = walls;
		}
	}

	public void ClearWalls(int x, int y, int walls) {
		MazeCell cell = GetCell(x, y);
		if (cell != null) {
			cell.walls ^= walls;
		}
	}

}