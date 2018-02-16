﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseListener : MonoBehaviour {

	public IMenu pauseMenu;
	public IMenu optionsMenu;

	private PlayerMove ply;

	void Update() {
		if (ply == null) {
			ply = FindObjectOfType<PlayerMove>();
			if (ply == null) {
				return;
			}
		}
		if (Input.GetButtonDown("Cancel") && !ply.locked) {
			TogglePause();
		}
	}

	public void TogglePause() {
		if (GameHandler.paused) {
			Unpause();
		} else {
			Pause();
		}
	}

	public void Pause() {
		GameHandler.paused = true;
		MenuSystem.GetInstance().ShowMenu(pauseMenu);
	}

	public void Unpause() {
		if (optionsMenu.IsShown()) {
			MenuSystem.GetInstance().ShowMenu(pauseMenu);
			return;
		}
		MenuSystem.GetInstance().HideMenus();
		GameHandler.paused = false;
	}

	public void OnOptionsClick() {
		MenuSystem.GetInstance().ShowMenu(optionsMenu);
	}

	public void OnQuitClick() {
		GameStateHandler.Instance.SetState(GameState.MENU);
	}

	public static PauseListener GetInstance() {
		return FindObjectOfType<PauseListener>();
	}

}