using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI instance;
    public static void SetSigletone(PlayerUI _playerUI)
    {
        instance = _playerUI;
    }

    [Header("Objects interactions")]
    [SerializeField] private GameObject takingBar;

    [Header("Ammo info")]
    [SerializeField] private GameObject ammoTextsParent;
    [SerializeField] private TMP_Text magazineAmmoText;
    [SerializeField] private TMP_Text totalAmmoText;

    [Header("Health info")]
    [SerializeField] private Health health;
    [SerializeField] private GameObject healthTextParent;
    [SerializeField] private TMP_Text healthText;

    [Header("Dead group")]
    [SerializeField] private CanvasGroup deadCanvasGroup;
    [SerializeField] private float transitionTime = 2f;
    [SerializeField] private CanvasGroup aliveCanvasGroup;

    [Header("Revive timer")]
    [SerializeField] private CanvasGroup reviveTimerGroup;
    [SerializeField] private UITimer reviveTimer;
    [SerializeField] private float reviveTimerHideTime = 2f;
    [SerializeField] private float reviveTimerFadeTime = 1f;


    [Header("Hitmarker")]
    [SerializeField] private CanvasGroup hitMarkerGroup;
    [SerializeField] private AudioClip hitmarkerClip;
    [SerializeField] private float hitmarkerTransitionTime = 0.05f;
    [SerializeField] private float hitmarkerLifeTime = 0.1f;

    [Header("Information Bar")]
    [SerializeField] private CanvasGroup informationBarGroup;
    [SerializeField] private TMP_Text informationBarText;
    [SerializeField] private float informationBarLifetime = 5f;
    [SerializeField] private float informationBarFadeTime = 0.5f;

    [Header("Pause group")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private float pauseFadeTime = 0.4f;
    [SerializeField] private CanvasGroup[] pausePages;
    [SerializeField] private CanvasGroup mainPausePage;
    [SerializeField] private float pausePagesFadeTime = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource source;

    //routines
    private IEnumerator currentHitmarkerRoutine;
    private IEnumerator currentInformationBarRoutine;
    private IEnumerator currentInformationBarFadeTransitionRoutine;
    private IEnumerator currentReviveTimerFadeRoutine;
    private IEnumerator currentReviveTimerStartRoutine;
    private IEnumerator currentPauseFadeRoutine;
    private IEnumerator currentPausePageFadeRoutine;

    //local values
    private PlayerInputActions inputActions;
    private bool savedCursorVisibleState = false;
    private CursorLockMode savedCursorLockState = CursorLockMode.Locked;

    private void OnEnable()
    {
        if (aliveCanvasGroup != null)
            aliveCanvasGroup.alpha = 1f;
        if (deadCanvasGroup != null)
            deadCanvasGroup.alpha = 0f;
        if (takingBar != null)
            takingBar.SetActive(false);
        if (ammoTextsParent != null)
            ammoTextsParent.SetActive(false);
        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(health.HealthPoints, health.HealthPoints);
        }
        if (informationBarGroup != null)
            informationBarGroup.alpha = 0f;
        if (reviveTimerGroup != null)
            reviveTimerGroup.alpha = 0f;

        //input
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        inputActions.Player.Pause.started += Pause_started;

        HidePausePanel();
        if(mainPausePage != null)
            OpenPausePage(mainPausePage);
    }

    private void OnDisable()
    {
        if(health != null)
            health.OnHealthChanged -= UpdateHealthBar;
    }

    #region objects interactions
    public static void SetTakingBarActive(bool activeSelf)
    {
        if (instance.takingBar != null)
            instance.takingBar.SetActive(activeSelf);
    }
    #endregion
    #region Ammo
    public static void SetMagazineAmmo(int ammo)
    {
        if (instance == null || instance.magazineAmmoText == null)
            return;
        instance.magazineAmmoText.text = ammo.ToString();
    }
    public static void SetTotalAmmo(int ammo)
    {
        if (instance == null || instance.totalAmmoText == null)
            return;
        instance.totalAmmoText.text = ammo.ToString();
    }
    public static void SetAmmoTextActive(bool activeSelf)
    {
        if (instance == null)
            return;
        instance.ammoTextsParent.SetActive(activeSelf);
    }
    public static void UpdateAmmoTexts(WeaponAmmunitionData data)
    {
        SetMagazineAmmo(data.magazineAmmo);
        SetTotalAmmo(data.totalAmmo);
    }
    #endregion
    #region Health
    public void UpdateHealthBar(float oldValue, float newValue)
    {
        if (health == null || healthText == null)
            return;
        healthText.text = $"{newValue}";
    }
    #endregion
    #region Hitmarker
    public static void ActivateHitMarker()
    {
        if (!GlobalPreferences.Preferences.enableHitMarkers)
            return;

        if (instance != null)
        {
            if (instance.currentHitmarkerRoutine != null)
                instance.StopCoroutine(instance.currentHitmarkerRoutine);

            instance.currentHitmarkerRoutine = instance.HitmarkerEnumerator();
            instance.StartCoroutine(instance.currentHitmarkerRoutine);
        }
    }
    private IEnumerator HitmarkerEnumerator()
    {
        if (source != null && hitmarkerClip != null)
            source.PlayOneShot(hitmarkerClip);

        IEnumerator transitionRoutine = FadeTransitionEnumerator(hitMarkerGroup, 1f, hitmarkerTransitionTime);
        StartCoroutine(transitionRoutine);

        yield return new WaitForSeconds(hitmarkerTransitionTime);
        yield return new WaitForSeconds(hitmarkerLifeTime);

        StopCoroutine(transitionRoutine);
        transitionRoutine = FadeTransitionEnumerator(hitMarkerGroup, 0f, hitmarkerTransitionTime);
        StartCoroutine(transitionRoutine);
    }
    #endregion
    #region Alive and Dead groups
    public static void ActivateDeadUI()
    {
        if (instance == null || instance.deadCanvasGroup == null || instance.aliveCanvasGroup == null) return;
        instance.StartCoroutine(instance.FadeTransitionEnumerator(instance.deadCanvasGroup, 1f, instance.transitionTime));
        instance.aliveCanvasGroup.alpha = 0f;

        instance.StartReviveTimerFadingScenario();
    }
    public static void ActivateAliveUI()
    {
        if (instance == null || instance.deadCanvasGroup == null || instance.aliveCanvasGroup == null) return;
        instance.StopAllCoroutines();
        instance.deadCanvasGroup.alpha = 0f;
        instance.aliveCanvasGroup.alpha = 1f;
        instance.hitMarkerGroup.alpha = 0;
    }
    private IEnumerator FadeTransitionEnumerator(CanvasGroup group, float targetAlpha, float time)
    {
        float starteedAlpha = group.alpha;
        float startedTime = Time.time;
        float t = 0f;

        while(t < 1f)
        {
            t = (Time.time - startedTime) / time;
            group.alpha = Mathf.Lerp(starteedAlpha, targetAlpha, t);
            yield return null;
        }
    }
    #endregion
    #region Information Bar
    public static void ViewMessage(string message)
    {
        if (instance == null) {
            Debug.LogWarning("Cant view message, because PlayerUI instance is not assigned");
            return; 
        }

        if (instance.currentInformationBarRoutine != null)
            instance.StopCoroutine(instance.currentInformationBarRoutine);

        instance.StartCoroutine(instance.InfromationBarEnumerator(message, instance.informationBarLifetime));
    }
    public static void ViewMessage(string message, float lifeTime)
    {
        if (instance == null)
        {
            Debug.LogWarning("Cant view message, because PlayerUI instance is not assigned");
            return;
        }

        if (instance.currentInformationBarRoutine != null)
            instance.StopCoroutine(instance.currentInformationBarRoutine);

        instance.currentInformationBarRoutine = instance.InfromationBarEnumerator(message, lifeTime);
        instance.StartCoroutine(instance.currentInformationBarRoutine);
    }
    private IEnumerator InfromationBarEnumerator(string message, float lifeTime)
    {
        if(informationBarGroup == null || informationBarText == null)
        {
            Debug.LogWarning("PLayerUI: Information bar components is not assigned");
            yield break;
        }

        if (currentInformationBarFadeTransitionRoutine != null)
            StopCoroutine(currentInformationBarFadeTransitionRoutine);

        currentInformationBarFadeTransitionRoutine = FadeTransitionEnumerator(informationBarGroup, 1f, informationBarFadeTime);
        StartCoroutine(currentInformationBarFadeTransitionRoutine);
        informationBarText.text = message;

        yield return new WaitForSeconds(informationBarFadeTime + lifeTime);
        StartCoroutine(FadeTransitionEnumerator(informationBarGroup, 0f, informationBarFadeTime));
    }
    #endregion
    #region ReviveTimer
    private void StartReviveTimerFadingScenario()
    {
        if (currentReviveTimerStartRoutine != null)
            StopCoroutine(currentReviveTimerStartRoutine);
        currentReviveTimerStartRoutine = ReviveTimerStartEnumerator();
        StartCoroutine(currentReviveTimerStartRoutine);
    }
    private IEnumerator ReviveTimerStartEnumerator()
    {
        if (reviveTimer == null || reviveTimerGroup == null)
            yield break;

        reviveTimerGroup.alpha = 0f;

        yield return new WaitForSeconds(reviveTimerHideTime);

        if (currentReviveTimerFadeRoutine != null)
            StopCoroutine(currentReviveTimerFadeRoutine);
        currentReviveTimerFadeRoutine = FadeTransitionEnumerator(reviveTimerGroup, 1f, reviveTimerFadeTime);
        StartCoroutine(currentReviveTimerFadeRoutine);
    }
    public static void SetRespawnTime(float targetTime)
    {
        if (instance == null || instance.reviveTimer == null)
            return;

        instance.reviveTimer.TargetTime = targetTime;
    }
    #endregion
    #region Pause
    public void ShowPausePanel()
    {
        if (pauseCanvasGroup == null)
            return;

        OpenPausePage(mainPausePage);

        if (currentPauseFadeRoutine != null)
            StopCoroutine(currentPauseFadeRoutine);
        currentPauseFadeRoutine = FadeTransitionEnumerator(pauseCanvasGroup, 1f, pauseFadeTime);
        StartCoroutine(currentPauseFadeRoutine);

        savedCursorLockState = Cursor.lockState;
        savedCursorVisibleState = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerMovement.instance != null && CameraLooking.instance != null)
        {
            PlayerMovement.LockMovement = true;
            CameraLooking.instance.LockRotation = true;
            WeaponsController.LockWeapons = true;
        }
    }
    public void HidePausePanel()
    {
        if (pauseCanvasGroup == null)
            return;

        if (currentPauseFadeRoutine != null)
            StopCoroutine(currentPauseFadeRoutine);
        currentPauseFadeRoutine = FadeTransitionEnumerator(pauseCanvasGroup, 0f, pauseFadeTime);
        StartCoroutine(currentPauseFadeRoutine);

        Cursor.lockState = savedCursorLockState;
        Cursor.visible = savedCursorVisibleState;

        if (PlayerMovement.instance != null && CameraLooking.instance != null)
        {
            PlayerMovement.LockMovement = false;
            CameraLooking.instance.LockRotation = false;
            WeaponsController.LockWeapons = false;
        }
    }
    private void Pause_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (pauseCanvasGroup == null)
            return;

        if (pauseCanvasGroup.alpha == 0)
            ShowPausePanel();
        else
            HidePausePanel();
    }
    public void OpenPausePage(CanvasGroup page)
    {
        if (currentPausePageFadeRoutine != null)
            StopCoroutine(currentPausePageFadeRoutine);

        HideAllPausePagesWithoutAnimation();

        page.gameObject.SetActive(true);
        currentPausePageFadeRoutine = FadeTransitionEnumerator(page, 1f, pausePagesFadeTime);
        StartCoroutine(currentPausePageFadeRoutine);
    }
    private void HideAllPausePagesWithoutAnimation()
    {
        foreach(CanvasGroup _page in pausePages)
        {
            _page.alpha = 0f;
            _page.gameObject.SetActive(false);
        }
    }
    #endregion
    #region Connection
    public void Disconnect()
    {
        var manager = NetworkManager.Singleton;

        if (manager.IsListening)
            manager.Shutdown();
    }
    #endregion
}
