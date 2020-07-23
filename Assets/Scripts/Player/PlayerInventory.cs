﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class WeaponSlot
{
    public GameObject Weapon;
    public float[] Data;
}

[RequireComponent(typeof(PlayerController))]
public class PlayerInventory : MonoBehaviour
{

    // inspector stuff
    [SerializeField]
    public List<WeaponSlot> weapons;
    public bool holstered = true;
    public float interactDistance = 2f;
    public float scrollScale = 10f;
    public LayerMask interactMask = ~0;
    public float maxUseAngle = 30f;

    // auto-assigned
    private PlayerController _playerController;
    private Camera _camera;
    private PlayerHealth _health;
    private PlayerInput _input;
    private InputAction m_Fire1;
    
    // math things
    private int _currentWeaponIndex;
    private float _scrollBuildup;
    private Weapon _currentWeapon;
    [HideInInspector]
    public float lastSwitch;
    
    private Useable _thingToUse;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _health = GetComponent<PlayerHealth>();
        // initialize each weaponslot floats dict if not already
        if (weapons == null)
        {
            weapons = new List<WeaponSlot>();
        }

        _input = GetComponent<PlayerInput>();
        m_Fire1 = _input.actions["Fire1"];
    }

    void Start()
    {
        _health.onDeathDelegate += OnDeath;
        _camera = _playerController.cam;
        lastSwitch = Time.time;
        if (!holstered)
        {
            SwitchToWeapon(0);
        }
    }

    public void OnWeaponSwitch(InputValue val) {
        if (weapons.Count == 0) return;
        float scrollInput = val.Get<float>();
        if (scrollInput == 0) return;
        if (holstered) {
            SwitchToWeapon(_currentWeaponIndex);
        } else {
            SwitchWeapons( (int)Mathf.Sign(scrollInput) );
        }
    }

    public void OnWeaponSlot(InputValue val) {
        float num = val.Get<float>();
        if (num == 0) return;
        SwitchToWeapon( (int)num - 1 );
    }

    public void OnFire1(InputValue val) {
        if (holstered && weapons.Count > 0) {
            SwitchToWeapon(_currentWeaponIndex);
        }
    }

    public void OnUse() {
        if (_thingToUse)
        {
            _thingToUse.Use(this);
        }
    }

    void Update()
    {

        if (_health.Dead) return;

        // highlight useable element
        CheckUseables();

    }

    void CheckUseables()
    {
        _thingToUse = null;
        // break if list is empty
        if (Useable.useables.Count == 0)
        {
            return;
        }
        // limit use angle? idk
        float closestAngle = maxUseAngle;
        foreach (Useable useable in Useable.useables)
        {
            useable.highlighted = false;
            Vector3 vFromCam = useable.transform.position - _camera.transform.position;
            Quaternion rotFromCamera =
                Quaternion.FromToRotation(vFromCam, _camera.transform.forward);
            float useableAngle = Quaternion.Angle(rotFromCamera, Quaternion.identity);
            if (useableAngle < closestAngle)
            {
                // make sure we have los
                if (Physics.Raycast(_camera.transform.position, vFromCam, out RaycastHit hit, interactDistance, interactMask)) {
                    if (hit.transform == useable.transform) {
                        _thingToUse = useable;
                        closestAngle = useableAngle;
                    }
                }
            }
        }
        
        if (_thingToUse) _thingToUse.highlighted = true;
    }

    // shift weapon slot
    void SwitchWeapons(int amt)
    {
        if (amt == 0) return;
        if (weapons.Count == 0) return;
        SwitchToWeapon((_currentWeaponIndex + weapons.Count + amt) % (weapons.Count));
    }

    // actually do the switch
    void SwitchToWeapon(int weaponIndex)
    {
        // don't do anything if we're switching to the same weapon
        if (weaponIndex == _currentWeaponIndex && !holstered) return;
        if (weaponIndex >= weapons.Count) return;
        _currentWeaponIndex = weaponIndex;
        
        // destroy current weapon gameobject and create new one
        if (_currentWeapon != null) Destroy(_currentWeapon.gameObject);
        _currentWeapon = Instantiate(weapons[weaponIndex].Weapon).GetComponent<Weapon>();
        _currentWeapon.Equip(_playerController, this, _camera, weapons[weaponIndex]);
        lastSwitch = Time.time;

        holstered = false;
    }

    public void AddWeapon(WeaponSlot weaponSlot)
    {
        weapons.Add(weaponSlot);
        SwitchToWeapon(weapons.Count - 1);
    }

    private void OnDeath() {
        // delete all children
        foreach (Transform trans in _camera.transform) {
            Destroy(trans.gameObject);
        }
    }
}
