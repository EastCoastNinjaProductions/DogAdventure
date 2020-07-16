﻿using System;
using System.Collections.Generic;
using UnityEngine;

// todo: this still isn't serializable
[Serializable]
public class WeaponFloatData : Dictionary<String, float> { }

[Serializable]
public class WeaponSlot
{
    public GameObject Weapon;
    [SerializeField]
    public WeaponFloatData Floats;
}

[RequireComponent(typeof(PlayerController))]
public class PlayerInventory : MonoBehaviour
{

    [SerializeField]
    public List<WeaponSlot> weapons;
    public bool holstered = true;

    private PlayerController _playerController;
    private Camera _camera;

    public float maxUseAngle = 30f;
    
    private int _currentWeaponIndex;
    private Weapon _currentWeapon;
    [HideInInspector]
    public float lastSwitch;
    
    private List<Useable> _useables;
    private Useable _thingToUse;
    private bool _using;

    private void Awake()
    {
        _useables = new List<Useable>();
        // initialize each weaponslot floats dict if not already
        foreach (WeaponSlot weaponSlot in weapons)
        {
            if (weaponSlot.Floats == null)
            {
                weaponSlot.Floats = new WeaponFloatData();
            }
        }
    }

    void Start()
    {
        _playerController = GetComponent<PlayerController>();
        _camera = Camera.main;
        lastSwitch = Time.time;
        if (!holstered)
        {
            SwitchToWeapon(0);
        }
    }

    void Update()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        int scrollMeaning = scrollInput == 0 ? 0 : Mathf.RoundToInt(Mathf.Sign(scrollInput));
        if (holstered && weapons.Count != 0)
        {
            if (Input.GetMouseButtonDown(0) || scrollMeaning != 0)
            {
                SwitchToWeapon(_currentWeaponIndex);
            } 
        }
        else if (Input.GetAxis("Holster") > 0)
        {
            // holster the weapon
            holstered = true;
            if (_currentWeapon != null) Destroy(_currentWeapon.gameObject);
            _currentWeaponIndex = 0;
        } else if (scrollMeaning != 0)
        {
            SwitchWeapons( scrollMeaning );
        }

        // highlight useable element
        CheckUseables();
        if (Input.GetAxis("Use") > 0)
        {
            if (_thingToUse && !_using)
            {
                _thingToUse.Use(this);
                _using = true;
            }
        }
        else
        {
            _using = false;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        Useable useable = other.transform.GetComponent<Useable>();
        if (useable)
        {
            _useables.Add(useable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Useable useable = other.transform.GetComponent<Useable>();
        if (_useables.Contains(useable))
        {
            _useables.Remove(useable);
            useable.highlighted = false;
        }
    }

    void CheckUseables()
    {
        _thingToUse = null;
        // break if list is empty
        if (_useables.Count == 0)
        {
            return;
        }
        // limit use angle? idk
        float closestAngle = maxUseAngle;
        foreach (Useable useable in _useables)
        {
            // does it exist anymore??
            if (!useable)
            {
                _useables.Remove(useable);
                return;
            }
            useable.highlighted = false;
            Quaternion rotFromCamera =
                Quaternion.FromToRotation(useable.transform.position - _camera.transform.position, _camera.transform.forward);
            float useableAngle = Quaternion.Angle(rotFromCamera, Quaternion.identity);
            if (useableAngle < closestAngle)
            {
                _thingToUse = useable;
                closestAngle = useableAngle;
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
        if (weaponIndex == _currentWeaponIndex && !holstered) return;
        _currentWeaponIndex = weaponIndex;
        
        // destroy current weapon gameobject and create new one
        if (_currentWeapon != null) Destroy(_currentWeapon.gameObject);
        _currentWeapon = Instantiate(weapons[weaponIndex].Weapon).GetComponent<Weapon>();
        _currentWeapon.Equip(_playerController, this, weapons[weaponIndex].Floats);
        lastSwitch = Time.time;

        holstered = false;
    }

    public void AddWeapon(WeaponSlot weaponSlot)
    {
        weapons.Add(weaponSlot);
        SwitchToWeapon(weapons.Count - 1);
    }
}
