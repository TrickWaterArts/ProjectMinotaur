﻿using UnityEngine;

public class Player : Entity {

	public float armRotation;
	public GameObject playerArm;

	private Vector3 rotationOffset;
	private PlayerMove player;
	private Camera cam;
	private IWeapon weapon;

	public override void Start() {
		base.Start();

		if (playerArm != null) {
			rotationOffset = new Vector3(playerArm.transform.rotation.x, playerArm.transform.rotation.y, playerArm.transform.rotation.z);
		}
		player = GetComponent<PlayerMove>();
		if (player == null) {
			Debug.LogError("Player not found on PlayerCombat object.");
			gameObject.SetActive(false);
		}

		cam = GetComponentInChildren<Camera>();
		if (cam == null) {
			Debug.LogError("Camera not found on Player object.");
			gameObject.SetActive(false);
		}

		weapon = new WeaponFist();
	}

	void Update() {
		if (weapon != null) {
			if (Input.GetMouseButtonDown(0)) {
				weapon.OnPrimary(this);
			} else if (Input.GetMouseButtonDown(1)) {
				weapon.OnSecondary(this);
			} else if (Input.GetMouseButtonDown(2)) {
				weapon.OnTertiary(this);
			}
			weapon.OnUpdate(this);
		}
	}

	public PlayerMove GetPlayer() {
		return player;
	}

	public Camera GetCamera() {
		return cam;
	}

	// Controls arm rotation
	private void HandleArmRotation() {
		if (player.Moving) {
			if (Mathf.Approximately(playerArm.transform.rotation.x, armRotation)) {
				rotationOffset = rotationOffset + new Vector3(armRotation, 0, 0);
			}
		}
	}

	// Damages player from attack
	public override void OnAttack(Entity attacker, int damage) {
		TakeHealth(damage);
		print("Damaged by " + attacker);
	}

}