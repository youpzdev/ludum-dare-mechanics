using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DemoFlow : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private DemoPlayer player;
    [SerializeField] private DemoSettings settings;

    [Space]
    [Header("HUD")]
    [SerializeField] private TMP_Text outcome;
    [SerializeField] private TMP_Text status;
    [SerializeField] private HudOutcomeFeedback outcomeFeedback;

    [Space]
    [Header("Scene navigation")]
    [Tooltip("Scene order for keys 1, 2 and 3.")]
    [SerializeField] private string[] sceneNames;

    [Space]
    [Header("Interactions")]
    [SerializeField] private BridgeDrawing drawing;
    [SerializeField] private CarryController carry;
    [SerializeField] private GrappleController grapple;

    private Tween outcomeTween;
    private bool finished;

    public bool Won { get; private set; }
    public bool Finished => finished;

    public void ConfigureInteractions(BridgeDrawing bridgeDrawing, CarryController carryController,
        GrappleController grappleController)
    {
        drawing = bridgeDrawing;
        carry = carryController;
        grapple = grappleController;
    }

    private void Update()
    {
        HandleSceneInput();

        if (!finished && player != null && player.transform.position.y < settings.FallHeight)
            Finish(false);
    }

    private void HandleSceneInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame)
            Restart();
        if (keyboard.digit1Key.wasPressedThisFrame)
            SwitchScene(0);
        if (keyboard.digit2Key.wasPressedThisFrame)
            SwitchScene(1);
        if (keyboard.digit3Key.wasPressedThisFrame)
            SwitchScene(2);
    }

    public void SetStatus(string text)
    {
        if (status == null)
            throw new InvalidOperationException("DemoFlow status reference is unassigned on " + name);
        status.text = text;
    }

    public void Finish(bool won)
    {
        if (finished)
            return;

        if (settings == null || outcome == null || float.IsNaN(settings.OutcomeDuration) ||
            float.IsInfinity(settings.OutcomeDuration) || settings.OutcomeDuration <= 0f)
            throw new InvalidOperationException("DemoFlow requires outcome and settings with positive finite OutcomeDuration on " + name);

        finished = true;
        Won = won;
        if (drawing != null)
        {
            drawing.EndStroke();
            drawing.enabled = false;
        }
        if (carry != null)
        {
            carry.CancelDrag();
            carry.enabled = false;
        }
        if (grapple != null)
        {
            grapple.Release();
            grapple.enabled = false;
        }
        if (player != null)
            player.Stop();
        outcome.text = won ? "Финиш. R - еще раз" : "Ты упал. R - повторить";
        outcome.color = won ? settings.SuccessColor : settings.FailureColor;
        outcome.alpha = 0f;
        outcomeTween = DOTween.To(() => outcome.alpha, value => outcome.alpha = value,
            1f, settings.OutcomeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        if (outcomeFeedback != null)
            outcomeFeedback.Play(won, settings.OutcomeDuration);
    }

    private void OnDisable()
    {
        if (outcomeFeedback != null)
            outcomeFeedback.Clear();
        if (outcomeTween != null)
        {
            outcomeTween.Kill();
            outcomeTween = null;
            if (outcome != null)
                outcome.alpha = 1f;
        }
    }

    public void Restart() { LoadScene(SceneManager.GetActiveScene().path); }

    public void SwitchScene(int index)
    {
        if (index >= 0 && index < sceneNames.Length)
            LoadScene(ResolveScenePath(SceneManager.GetActiveScene().path, sceneNames[index]));
    }

    private static string ResolveScenePath(string currentPath, string sceneName)
    {
        int directoryEnd = currentPath.LastIndexOf('/');

        return currentPath.Substring(0, directoryEnd + 1) + sceneName + ".unity";
    }

    private static void LoadScene(string path)
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(path);
#endif
    }
}
