﻿using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour {
	
	public WeaponDefinition WeaponType { private set; get; }
	public Player Holder { private set; get; }
	public ItemStack Stack { private set; get; }

	private ParticleSystem muzzleFlash;
	private float lastFire;
	private Vector3 swayInit;

	void Update() {
		if (GameHandler.paused) {
			return;
		}
		if (lastFire < WeaponType.resetTime) {
			lastFire += Time.deltaTime;
		}
		if (Holder != null) {
			float movX = Mathf.Clamp(-Input.GetAxis("Mouse X") * WeaponType.swayAmount, -WeaponType.swayMax, WeaponType.swayMax);
			float movY = Mathf.Clamp(-Input.GetAxis("Mouse Y") * WeaponType.swayAmount, -WeaponType.swayMax, WeaponType.swayMax);
			Vector3 final = new Vector3(movX, movY);
			transform.localPosition = Vector3.Lerp(transform.localPosition, final + swayInit, Time.deltaTime * WeaponType.swaySmooth);
		}
	}

	public void DrawFlash() {
		if (muzzleFlash != null) {
			muzzleFlash.Play(true);
			StartCoroutine(StopFlash());
		}
	}

	private IEnumerator StopFlash() {
		yield return new WaitForSeconds(WeaponType.resetTime / 2.0f);
		muzzleFlash.Stop();
	}

	public void AttemptFire() {
		if (lastFire < WeaponType.resetTime) {
			return;
		}
		if (GetCurrentClipAmmo() >= WeaponType.shotsPerPrimary) {
			lastFire = 0.0f;
			Holder.MovementMotor.DoRecoil(WeaponType.recoilTime, WeaponType.recoilX, WeaponType.recoilY, WeaponType.recoilSpeed);
			for (int i = 0; i < WeaponType.shotsPerPrimary; i++) {
				DoShot();
			}
		} else {
			DoReload();
		}
	}

	private void DoShot() {
		WeaponType.OnPrimary(this);
	}

	private void DoReload() {
		WeaponType.OnReload(this);
	}

	private void Init(int startingClips, WeaponDefinition type) {
		WeaponType = type;
		Stack = new ItemStack(type, 1);
		Stack.Data.Set("clip_count", startingClips - 1);
		Stack.Data.Set("current_clip_ammo", type.ammoPerClip);
		Stack.Data.Set("last_fire", 0);
	}

	public void SetPlayer(Player player) {
		if (player == null) {
			transform.parent = null;
			Holder = null;
			gameObject.SetLayer(0);
			return;
		}
		Holder = player;
		gameObject.SetLayer(8);
		transform.parent = player.HandRenderer.gameObject.transform;
		transform.localPosition = WeaponType.DisplayPositionOffset;
		transform.localRotation = Quaternion.Euler(WeaponType.DisplayRotationOffset);
		swayInit = transform.localPosition;
	}

	public Vector3 GetBarrelPosWorld() {
		return transform.TransformPoint(WeaponType.barrelPosition);
	}

	public int GetCurrentClipAmmo() {
		return Stack.Data.Get("current_clip_ammo", int.MinValue);
	}

	public int GetClipCount() {
		return Stack.Data.Get("clip_count", int.MinValue);
	}

	public float GetTimeSinceLastFire() {
		return Stack.Data.Get("last_fire", float.MinValue);
	}

	public void SetCurrentClipAmmo(int clip) {
		Stack.Data.Set("current_clip_ammo", clip);
	}

	public void SetClipCount(int clips) {
		Stack.Data.Set("clip_count", clips);
	}

	public void SetTimeSinceLastFire(float time) {
		Stack.Data.Set("last_fire", time);
	}

	public static Weapon Create(Player parentPlayer, WeaponDefinition def) {
		if (def == null) {
			return null;
		}
		GameObject tmp = new GameObject(def.DisplayName);
		tmp.transform.name = "Weapon: " + def.DisplayName;
		Weapon w = tmp.AddComponent<Weapon>();
		w.Init(3, def);
		if (def.drawTrail) {
			GameObject obj = Resources.Load<GameObject>("Weapon/MuzzleFlash");
			if (obj != null) {
				GameObject tmptmp = Instantiate(obj, Vector3.zero, Quaternion.identity);
				foreach (Transform trans in tmptmp.transform) {
					if (trans.parent.Equals(tmptmp.transform)) {
						w.muzzleFlash = trans.GetComponent<ParticleSystem>();
						break;
					}
				}
				if (w.muzzleFlash == null) {
					Debug.LogWarning("MuzzleFlash has no particle system.");
					Destroy(tmptmp);
				} else {
					w.muzzleFlash.Stop(true);
					tmptmp.transform.parent = w.gameObject.transform;
					tmptmp.transform.localPosition = def.barrelPosition;
				}
			} else {
				Debug.LogWarning("Failed to load MuzzleFlash prefab.");
			}
		}
		if (def.Model != null) {
			GameObject model = Instantiate(def.Model, Vector3.zero, Quaternion.identity);
			model.transform.parent = tmp.transform;
			model.transform.name = "Model";
			model.transform.localPosition = Vector3.zero;
		}
		w.SetPlayer(parentPlayer);
		return w;
	}

	public static Weapon Create(Player player, ItemStack stack) {
		if (stack == null || stack.Item == null || !(stack.Item is WeaponDefinition)) {
			return null;
		}
		Weapon wep = Create(player, stack.Item as WeaponDefinition);
		wep.Stack.Data.SetAll(stack.Data);
		return wep;
	}

}