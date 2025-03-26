using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using InfimaGames.LowPolyShooterPack;

[RequireComponent(typeof(CharacterKinematics))]
public sealed class Character : CharacterBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    private InventoryBehaviour inventory;

    [Header("Cameras")]

    [Tooltip("Normal Camera.")]
    [SerializeField]
    private Camera cameraWorld;

    [Header("Animation")]

    [Tooltip("Determines how smooth the locomotion blendspace is.")]
    [SerializeField]
    private float dampTimeLocomotion = 0.15f;

    [Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
    [SerializeField]
    private float dampTimeAiming = 0.3f;

    [Header("Animation Procedural")]
    [SerializeField] private Animator characterAnimator;

    private bool aiming;
    private bool running;
    private bool holstered;
    private float lastShotTime;
    private int layerOverlay;
    private int layerHolster;
    private int layerActions;
    private CharacterKinematics characterKinematics;
    private WeaponBehaviour equippedWeapon;
    private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
    private ScopeBehaviour equippedWeaponScope;
    private MagazineBehaviour equippedWeaponMagazine;
    private bool reloading;
    private bool inspecting;
    private bool holstering;
    private Vector2 axisLook;
    private Vector2 axisMovement;
    private bool holdingButtonAim;
    private bool holdingButtonRun;
    private bool holdingButtonFire;
    private bool tutorialTextVisible;
    private bool cursorLocked;
    private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");
    private static readonly int HashMovement = Animator.StringToHash("Movement");
    protected override void Awake()
    {
        cursorLocked = true;
        UpdateCursorState();
        characterKinematics = GetComponent<CharacterKinematics>();
        inventory.Init();
        RefreshWeaponSetup();
    }
    protected override void Start()
    {
        layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
        layerActions = characterAnimator.GetLayerIndex("Layer Actions");
        layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
    }
    protected override void Update()
    {
        aiming = holdingButtonAim && CanAim();
        running = holdingButtonRun && CanRun();
        if (holdingButtonFire)
        {
            if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
            {
                if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                    Fire();
            }
        }
        UpdateAnimator();
    }
    protected override void LateUpdate()
    {
        if (equippedWeapon == null)
            return;
        if (equippedWeaponScope == null)
            return;
        if (characterKinematics != null)
        {
            characterKinematics.Compute();
        }
    }
    public override Camera GetCameraWorld() => cameraWorld;
    public override InventoryBehaviour GetInventory() => inventory;
    public override bool IsCrosshairVisible() => !aiming && !holstered;
    public override bool IsRunning() => running;
    public override bool IsAiming() => aiming;
    public override bool IsCursorLocked() => cursorLocked;
    public override bool IsTutorialTextVisible() => tutorialTextVisible;
    public override Vector2 GetInputMovement() => axisMovement;
    public override Vector2 GetInputLook() => axisLook;
    private void UpdateAnimator()
    {
        characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);
        characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);
        const string boolNameAim = "Aim";
        characterAnimator.SetBool(boolNameAim, aiming);
        const string boolNameRun = "Running";
        characterAnimator.SetBool(boolNameRun, running);
    }
    private void Inspect()
    {
        inspecting = true;
        characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
    }
    private void Fire()
    {
        lastShotTime = Time.time;
        equippedWeapon.Fire();
        const string stateName = "Fire";
        characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);
    }
    private void PlayReloadAnimation()
    {
        string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
        characterAnimator.Play(stateName, layerActions, 0.0f);
        reloading = true;
        equippedWeapon.Reload();
    }
    private IEnumerator Equip(int index = 0)
    {
        if (!holstered)
        {
            SetHolstered(holstering = true);
            yield return new WaitUntil(() => holstering == false);
        }
        SetHolstered(false);
        characterAnimator.Play("Unholster", layerHolster, 0);

        inventory.Equip(index);
        RefreshWeaponSetup();
    }
    private void RefreshWeaponSetup()
    {
        if ((equippedWeapon = inventory.GetEquipped()) == null)
            return;
        characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();
        weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
        if (weaponAttachmentManager == null)
            return;
        equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
        equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
    }
    private void FireEmpty()
    {
        /*
         * Save Time. Even though we're not actually firing, we still need this for the fire rate between
         * empty shots.
         */
        lastShotTime = Time.time;
        characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
    }
    private void UpdateCursorState()
    {
        Cursor.visible = !cursorLocked;
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }
    private void SetHolstered(bool value = true)
    {
        holstered = value;
        const string boolName = "Holstered";
        characterAnimator.SetBool(boolName, holstered);
    }
    private bool CanPlayAnimationFire()
    {
        if (holstered || holstering)
            return false;
        if (reloading)
            return false;
        if (inspecting)
            return false;
        return true;
    }
    private bool CanPlayAnimationReload()
    {
        //No reloading!
        if (reloading)
            return false;

        //Block while inspecting.
        if (inspecting)
            return false;

        //Return.
        return true;
    }

    /// <summary>
    /// Returns true if the character is able to holster their weapon.
    /// </summary>
    /// <returns></returns>
    private bool CanPlayAnimationHolster()
    {
        //Block.
        if (reloading)
            return false;

        //Block.
        if (inspecting)
            return false;

        //Return.
        return true;
    }

    /// <summary>
    /// Returns true if the Character can change their Weapon.
    /// </summary>
    /// <returns></returns>
    private bool CanChangeWeapon()
    {
        //Block.
        if (holstering)
            return false;

        //Block.
        if (reloading)
            return false;

        //Block.
        if (inspecting)
            return false;

        //Return.
        return true;
    }

    /// <summary>
    /// Returns true if the Character can play the Inspect animation.
    /// </summary>
    private bool CanPlayAnimationInspect()
    {
        //Block.
        if (holstered || holstering)
            return false;

        //Block.
        if (reloading)
            return false;

        //Block.
        if (inspecting)
            return false;

        //Return.
        return true;
    }

    /// <summary>
    /// Returns true if the Character can Aim.
    /// </summary>
    /// <returns></returns>
    private bool CanAim()
    {
        //Block.
        if (holstered || inspecting)
            return false;

        //Block.
        if (reloading || holstering)
            return false;

        //Return.
        return true;
    }

    /// <summary>
    /// Returns true if the character can run.
    /// </summary>
    /// <returns></returns>
    private bool CanRun()
    {
        //Block.
        if (inspecting)
            return false;

        //Block.
        if (reloading || aiming)
            return false;

        //While trying to fire, we don't want to run. We do this just in case we do fire.
        if (holdingButtonFire && equippedWeapon.HasAmmunition())
            return false;

        //This blocks running backwards, or while fully moving sideways.
        if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
            return false;

        //Return.
        return true;
    }
    #region INPUT

    /// <summary>
    /// Fire.
    /// </summary>
    public void OnTryFire(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Switch.
        switch (context)
        {
            //Started.
            case { phase: InputActionPhase.Started }:
                //Hold.
                holdingButtonFire = true;
                break;
            //Performed.
            case { phase: InputActionPhase.Performed }:
                //Ignore if we're not allowed to actually fire.
                if (!CanPlayAnimationFire())
                    break;

                //Check.
                if (equippedWeapon.HasAmmunition())
                {
                    //Check.
                    if (equippedWeapon.IsAutomatic())
                        break;

                    //Has fire rate passed.
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                }
                //Fire Empty.
                else
                    FireEmpty();
                break;
            //Canceled.
            case { phase: InputActionPhase.Canceled }:
                //Stop Hold.
                holdingButtonFire = false;
                break;
        }
    }
    /// <summary>
    /// Reload.
    /// </summary>
    public void OnTryPlayReload(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Block.
        if (!CanPlayAnimationReload())
            return;

        //Switch.
        switch (context)
        {
            //Performed.
            case { phase: InputActionPhase.Performed }:
                //Play Animation.
                PlayReloadAnimation();
                break;
        }
    }

    /// <summary>
    /// Inspect.
    /// </summary>
    public void OnTryInspect(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Block.
        if (!CanPlayAnimationInspect())
            return;

        //Switch.
        switch (context)
        {
            //Performed.
            case { phase: InputActionPhase.Performed }:
                //Play Animation.
                Inspect();
                break;
        }
    }
    /// <summary>
    /// Aiming.
    /// </summary>
    public void OnTryAiming(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Switch.
        switch (context.phase)
        {
            case InputActionPhase.Started:
                //Started.
                holdingButtonAim = true;
                break;
            case InputActionPhase.Canceled:
                //Canceled.
                holdingButtonAim = false;
                break;
        }
    }

    /// <summary>
    /// Holster.
    /// </summary>
    public void OnTryHolster(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Switch.
        switch (context.phase)
        {
            //Performed.
            case InputActionPhase.Performed:
                //Check.
                if (CanPlayAnimationHolster())
                {
                    //Set.
                    SetHolstered(!holstered);
                    //Holstering.
                    holstering = true;
                }
                break;
        }
    }
    /// <summary>
    /// Run. 
    /// </summary>
    public void OnTryRun(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Switch.
        switch (context.phase)
        {
            //Started.
            case InputActionPhase.Started:
                //Start.
                holdingButtonRun = true;
                break;
            //Canceled.
            case InputActionPhase.Canceled:
                //Stop.
                holdingButtonRun = false;
                break;
        }
    }
    /// <summary>
    /// Next Inventory Weapon.
    /// </summary>
    public void OnTryInventoryNext(InputAction.CallbackContext context)
    {
        //Block while the cursor is unlocked.
        if (!cursorLocked)
            return;

        //Null Check.
        if (inventory == null)
            return;

        //Switch.
        switch (context)
        {
            //Performed.
            case { phase: InputActionPhase.Performed }:
                //Get the index increment direction for our inventory using the scroll wheel direction. If we're not
                //actually using one, then just increment by one.
                float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;

                //Get the next index to switch to.
                int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetLastIndex();
                //Get the current weapon's index.
                int indexCurrent = inventory.GetEquippedIndex();

                //Make sure we're allowed to change, and also that we're not using the same index, otherwise weird things happen!
                if (CanChangeWeapon() && (indexCurrent != indexNext))
                    StartCoroutine(nameof(Equip), indexNext);
                break;
        }
    }

    public void OnLockCursor(InputAction.CallbackContext context)
    {
        //Switch.
        switch (context)
        {
            //Performed.
            case { phase: InputActionPhase.Performed }:
                //Toggle the cursor locked value.
                cursorLocked = !cursorLocked;
                //Update the cursor's state.
                UpdateCursorState();
                break;
        }
    }

    /// <summary>
    /// Movement.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        //Read.
        axisMovement = cursorLocked ? context.ReadValue<Vector2>() : default;
    }
    /// <summary>
    /// Look.
    /// </summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        //Read.
        axisLook = cursorLocked ? context.ReadValue<Vector2>() : default;
    }

    /// <summary>
    /// Called in order to update the tutorial text value.
    /// </summary>
    public void OnUpdateTutorial(InputAction.CallbackContext context)
    {
        //Switch.
        tutorialTextVisible = context switch
        {
            //Started. Show the tutorial.
            { phase: InputActionPhase.Started } => true,
            //Canceled. Hide the tutorial.
            { phase: InputActionPhase.Canceled } => false,
            //Default.
            _ => tutorialTextVisible
        };
    }

    #endregion

    #region ANIMATION EVENTS

    public override void EjectCasing()
    {
        //Notify the weapon.
        if (equippedWeapon != null)
            equippedWeapon.EjectCasing();
    }
    public override void FillAmmunition(int amount)
    {
        //Notify the weapon to fill the ammunition by the amount.
        if (equippedWeapon != null)
            equippedWeapon.FillAmmunition(amount);
    }

    public override void SetActiveMagazine(int active)
    {
        //Set magazine gameObject active.
        equippedWeaponMagazine.gameObject.SetActive(active != 0);
    }

    public override void AnimationEndedReload()
    {
        //Stop reloading!
        reloading = false;
    }

    public override void AnimationEndedInspect()
    {
        //Stop Inspecting.
        inspecting = false;
    }
    public override void AnimationEndedHolster()
    {
        //Stop Holstering.
        holstering = false;
    }

    #endregion
}


