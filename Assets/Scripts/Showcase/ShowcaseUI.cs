using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShowcaseUI : MonoBehaviour
{
    [Header("Main Menu UI")]
    public GameObject mainMenuPanel;
    public Button startButton;
    public Button scenariosButton;
    public Button quitButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;

    [Header("Scenario Selection UI")]
    public GameObject scenarioPanel;
    public Transform scenarioButtonContainer;
    public GameObject scenarioButtonPrefab;
    public Button backToMainButton;

    [Header("Parameters UI")]
    public GameObject parametersPanel;
    public Button startScenarioButton;
    public Button backToScenariosButton;
    public Button resetToDefaultsButton;

    // Parameter UI Elements
    [Header("Fruit Parameters")]
    public Slider fruitCountSlider;
    public TextMeshProUGUI fruitCountText;
    public TMP_Dropdown fruitTypeDropdown;
    public Toggle[] fruitTypeToggles;

    [Header("Physics Parameters")]
    public Slider gravitySlider;
    public TextMeshProUGUI gravityText;
    public Toggle particleOptimizationToggle;
    public Toggle adaptiveSolverToggle;
    public Slider solverIterationsSlider;
    public TextMeshProUGUI solverIterationsText;
    public Slider timeStepSlider;
    public TextMeshProUGUI timeStepText;
    public Toggle performanceModeToggle;

    [Header("Tools Parameters")]
    public Toggle basketEnabledToggle;
    public Toggle hammerEnabledToggle;
    public Toggle knifeEnabledToggle;

    [Header("Hammer Parameters")]
    public GameObject hammerParametersPanel;
    public Slider pushThresholdSlider;
    public TextMeshProUGUI pushThresholdText;
    public Slider deformThresholdSlider;
    public TextMeshProUGUI deformThresholdText;
    public Slider crushThresholdSlider;
    public TextMeshProUGUI crushThresholdText;
    public Slider pushForceSlider;
    public TextMeshProUGUI pushForceText;
    public Slider deformForceSlider;
    public TextMeshProUGUI deformForceText;
    public Slider crushForceSlider;
    public TextMeshProUGUI crushForceText;

    [Header("Showcase Controls")]
    public GameObject showcaseControlsPanel;
    public Button restartButton;
    public Button returnToMenuButton;
    public Button performanceToggleButton;
    public TextMeshProUGUI performanceToggleText;

    private ShowcaseManager showcaseManager;
    private ShowcaseScenario currentScenario;
    private ShowcaseParameters currentParameters;
    private List<GameObject> scenarioButtons = new List<GameObject>();
    private bool performanceDisplayEnabled = true;

    void Start()
    {
        showcaseManager = FindObjectOfType<ShowcaseManager>();

        if (showcaseManager == null)
        {
            Debug.LogError("❌ ShowcaseUI: No ShowcaseManager found!");
            return;
        }

        SetupUI();
        SetupEventListeners();

        // Show main menu by default
        ShowMainMenu();
    }

    void SetupUI()
    {
        // Set version text
        if (versionText != null)
        {
            versionText.text = "Physics Simulation v1.0";
        }

        // Setup parameter ranges
        SetupParameterRanges();

        // Setup fruit type dropdown
        SetupFruitTypeDropdown();
    }

    void SetupParameterRanges()
    {
        // Fruit count
        if (fruitCountSlider != null)
        {
            fruitCountSlider.minValue = 10;
            fruitCountSlider.maxValue = 1000;
            fruitCountSlider.value = 50;
        }

        // Gravity
        if (gravitySlider != null)
        {
            gravitySlider.minValue = -20f;
            gravitySlider.maxValue = 0f;
            gravitySlider.value = -9.81f;
        }

        // Solver iterations
        if (solverIterationsSlider != null)
        {
            solverIterationsSlider.minValue = 1;
            solverIterationsSlider.maxValue = 10;
            solverIterationsSlider.value = 5;
        }

        // Time step
        if (timeStepSlider != null)
        {
            timeStepSlider.minValue = 0.005f;
            timeStepSlider.maxValue = 0.05f;
            timeStepSlider.value = 0.02f;
        }

        // Hammer thresholds
        if (pushThresholdSlider != null)
        {
            pushThresholdSlider.minValue = 0.5f;
            pushThresholdSlider.maxValue = 5f;
            pushThresholdSlider.value = 2f;
        }

        if (deformThresholdSlider != null)
        {
            deformThresholdSlider.minValue = 2f;
            deformThresholdSlider.maxValue = 10f;
            deformThresholdSlider.value = 5f;
        }

        if (crushThresholdSlider != null)
        {
            crushThresholdSlider.minValue = 5f;
            crushThresholdSlider.maxValue = 15f;
            crushThresholdSlider.value = 8f;
        }

        // Hammer forces
        if (pushForceSlider != null)
        {
            pushForceSlider.minValue = 1f;
            pushForceSlider.maxValue = 10f;
            pushForceSlider.value = 3f;
        }

        if (deformForceSlider != null)
        {
            deformForceSlider.minValue = 5f;
            deformForceSlider.maxValue = 20f;
            deformForceSlider.value = 8f;
        }

        if (crushForceSlider != null)
        {
            crushForceSlider.minValue = 10f;
            crushForceSlider.maxValue = 50f;
            crushForceSlider.value = 15f;
        }
    }

    void SetupFruitTypeDropdown()
    {
        if (fruitTypeDropdown != null)
        {
            fruitTypeDropdown.ClearOptions();
            fruitTypeDropdown.AddOptions(new List<string> { "Apple", "Orange", "Banana", "Strawberry", "Watermelon", "Pineapple", "Coconut", "Sphere", "Cube" });
        }
    }

    void SetupEventListeners()
    {
        // Main menu buttons
        if (startButton != null)
            startButton.onClick.AddListener(() => showcaseManager.ShowScenarioSelection());

        if (scenariosButton != null)
            scenariosButton.onClick.AddListener(() => showcaseManager.ShowScenarioSelection());

        if (quitButton != null)
            quitButton.onClick.AddListener(() => showcaseManager.QuitApplication());

        // Scenario selection buttons
        if (backToMainButton != null)
            backToMainButton.onClick.AddListener(() => showcaseManager.ShowMainMenu());

        // Parameter buttons
        if (startScenarioButton != null)
            startScenarioButton.onClick.AddListener(() => showcaseManager.StartCurrentScenario());

        if (backToScenariosButton != null)
            backToScenariosButton.onClick.AddListener(() => showcaseManager.ShowScenarioSelection());

        if (resetToDefaultsButton != null)
            resetToDefaultsButton.onClick.AddListener(ResetToDefaults);

        // Showcase control buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScenario);

        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(() => showcaseManager.ReturnToMainMenu());

        if (performanceToggleButton != null)
            performanceToggleButton.onClick.AddListener(TogglePerformanceDisplay);

        // Parameter sliders
        SetupSliderListeners();

        // Parameter toggles
        SetupToggleListeners();
    }

    void SetupSliderListeners()
    {
        if (fruitCountSlider != null)
            fruitCountSlider.onValueChanged.AddListener(OnFruitCountChanged);

        if (gravitySlider != null)
            gravitySlider.onValueChanged.AddListener(OnGravityChanged);

        if (solverIterationsSlider != null)
            solverIterationsSlider.onValueChanged.AddListener(OnSolverIterationsChanged);

        if (timeStepSlider != null)
            timeStepSlider.onValueChanged.AddListener(OnTimeStepChanged);

        if (pushThresholdSlider != null)
            pushThresholdSlider.onValueChanged.AddListener(OnPushThresholdChanged);

        if (deformThresholdSlider != null)
            deformThresholdSlider.onValueChanged.AddListener(OnDeformThresholdChanged);

        if (crushThresholdSlider != null)
            crushThresholdSlider.onValueChanged.AddListener(OnCrushThresholdChanged);

        if (pushForceSlider != null)
            pushForceSlider.onValueChanged.AddListener(OnPushForceChanged);

        if (deformForceSlider != null)
            deformForceSlider.onValueChanged.AddListener(OnDeformForceChanged);

        if (crushForceSlider != null)
            crushForceSlider.onValueChanged.AddListener(OnCrushForceChanged);
    }

    void SetupToggleListeners()
    {
        if (particleOptimizationToggle != null)
            particleOptimizationToggle.onValueChanged.AddListener(OnParticleOptimizationChanged);

        if (adaptiveSolverToggle != null)
            adaptiveSolverToggle.onValueChanged.AddListener(OnAdaptiveSolverChanged);

        if (performanceModeToggle != null)
            performanceModeToggle.onValueChanged.AddListener(OnPerformanceModeChanged);

        if (basketEnabledToggle != null)
            basketEnabledToggle.onValueChanged.AddListener(OnBasketEnabledChanged);

        if (hammerEnabledToggle != null)
            hammerEnabledToggle.onValueChanged.AddListener(OnHammerEnabledChanged);

        if (knifeEnabledToggle != null)
            knifeEnabledToggle.onValueChanged.AddListener(OnKnifeEnabledChanged);
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(scenarioPanel, false);
        SetPanelActive(parametersPanel, false);
        SetPanelActive(showcaseControlsPanel, false);
    }

    public void ShowScenarioSelection()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(scenarioPanel, true);
        SetPanelActive(parametersPanel, false);
        SetPanelActive(showcaseControlsPanel, false);

        CreateScenarioButtons();
    }

    public void ShowParameters(ShowcaseScenario scenario)
    {
        currentScenario = scenario;
        currentParameters = scenario.parameters;

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(scenarioPanel, false);
        SetPanelActive(parametersPanel, true);
        SetPanelActive(showcaseControlsPanel, false);

        LoadParametersToUI();
    }

    public void ShowShowcaseControls()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(scenarioPanel, false);
        SetPanelActive(parametersPanel, false);
        SetPanelActive(showcaseControlsPanel, true);
    }

    void CreateScenarioButtons()
    {
        // Clear existing buttons
        foreach (var button in scenarioButtons)
        {
            if (button != null)
                DestroyImmediate(button);
        }
        scenarioButtons.Clear();

        // Create new buttons
        if (scenarioButtonContainer != null && scenarioButtonPrefab != null)
        {
            foreach (var scenario in showcaseManager.scenarios)
            {
                GameObject buttonObj = Instantiate(scenarioButtonPrefab, scenarioButtonContainer);
                scenarioButtons.Add(buttonObj);

                // Setup button text
                var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = scenario.name;
                }

                // Setup button click
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => ShowParameters(scenario));
                }
            }
        }
    }

    void LoadParametersToUI()
    {
        if (currentParameters == null) return;

        // Load fruit parameters
        if (fruitCountSlider != null)
            fruitCountSlider.value = currentParameters.fruitCount;

        // Load physics parameters
        if (gravitySlider != null)
            gravitySlider.value = currentParameters.gravity.y;

        if (particleOptimizationToggle != null)
            particleOptimizationToggle.isOn = currentParameters.particleOptimization;

        if (adaptiveSolverToggle != null)
            adaptiveSolverToggle.isOn = currentParameters.adaptiveSolver;

        if (solverIterationsSlider != null)
            solverIterationsSlider.value = currentParameters.solverIterations;

        if (timeStepSlider != null)
            timeStepSlider.value = currentParameters.timeStep;

        if (performanceModeToggle != null)
            performanceModeToggle.isOn = currentParameters.performanceMode;

        // Load tool parameters
        if (basketEnabledToggle != null)
            basketEnabledToggle.isOn = currentParameters.basketEnabled;

        if (hammerEnabledToggle != null)
            hammerEnabledToggle.isOn = currentParameters.hammerEnabled;

        if (knifeEnabledToggle != null)
            knifeEnabledToggle.isOn = currentParameters.knifeEnabled;

        // Load hammer parameters
        if (currentParameters.hammerSettings != null)
        {
            if (pushThresholdSlider != null)
                pushThresholdSlider.value = currentParameters.hammerSettings.pushThreshold;

            if (deformThresholdSlider != null)
                deformThresholdSlider.value = currentParameters.hammerSettings.deformThreshold;

            if (crushThresholdSlider != null)
                crushThresholdSlider.value = currentParameters.hammerSettings.crushThreshold;

            if (pushForceSlider != null)
                pushForceSlider.value = currentParameters.hammerSettings.pushForce;

            if (deformForceSlider != null)
                deformForceSlider.value = currentParameters.hammerSettings.deformForce;

            if (crushForceSlider != null)
                crushForceSlider.value = currentParameters.hammerSettings.crushForce;
        }

        // Update hammer panel visibility
        UpdateHammerPanelVisibility();
    }

    void UpdateHammerPanelVisibility()
    {
        if (hammerParametersPanel != null)
        {
            hammerParametersPanel.SetActive(currentParameters.hammerEnabled);
        }
    }

    void ResetToDefaults()
    {
        if (currentScenario != null)
        {
            LoadParametersToUI();
        }
    }

    void RestartScenario()
    {
        var setup = FindObjectOfType<ShowcaseSetup>();
        if (setup != null)
        {
            setup.RestartScenario();
        }
    }

    void TogglePerformanceDisplay()
    {
        performanceDisplayEnabled = !performanceDisplayEnabled;

        var monitor = FindObjectOfType<PerformanceMonitor>();
        if (monitor != null)
        {
            monitor.showPerformanceStats = performanceDisplayEnabled;
        }

        if (performanceToggleText != null)
        {
            performanceToggleText.text = performanceDisplayEnabled ? "Hide Performance" : "Show Performance";
        }
    }

    // Parameter change handlers
    void OnFruitCountChanged(float value)
    {
        if (currentParameters != null)
        {
            currentParameters.fruitCount = Mathf.RoundToInt(value);
            if (fruitCountText != null)
                fruitCountText.text = $"Fruit Count: {currentParameters.fruitCount}";
        }
    }

    void OnGravityChanged(float value)
    {
        if (currentParameters != null)
        {
            currentParameters.gravity = new Vector3(0, value, 0);
            if (gravityText != null)
                gravityText.text = $"Gravity: {value:F1}";
        }
    }

    void OnSolverIterationsChanged(float value)
    {
        if (currentParameters != null)
        {
            currentParameters.solverIterations = Mathf.RoundToInt(value);
            if (solverIterationsText != null)
                solverIterationsText.text = $"Solver Iterations: {currentParameters.solverIterations}";
        }
    }

    void OnTimeStepChanged(float value)
    {
        if (currentParameters != null)
        {
            currentParameters.timeStep = value;
            if (timeStepText != null)
                timeStepText.text = $"Time Step: {value:F3}";
        }
    }

    void OnPushThresholdChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.pushThreshold = value;
            if (pushThresholdText != null)
                pushThresholdText.text = $"Push Threshold: {value:F1} m/s";
        }
    }

    void OnDeformThresholdChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.deformThreshold = value;
            if (deformThresholdText != null)
                deformThresholdText.text = $"Deform Threshold: {value:F1} m/s";
        }
    }

    void OnCrushThresholdChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.crushThreshold = value;
            if (crushThresholdText != null)
                crushThresholdText.text = $"Crush Threshold: {value:F1} m/s";
        }
    }

    void OnPushForceChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.pushForce = value;
            if (pushForceText != null)
                pushForceText.text = $"Push Force: {value:F1}";
        }
    }

    void OnDeformForceChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.deformForce = value;
            if (deformForceText != null)
                deformForceText.text = $"Deform Force: {value:F1}";
        }
    }

    void OnCrushForceChanged(float value)
    {
        if (currentParameters?.hammerSettings != null)
        {
            currentParameters.hammerSettings.crushForce = value;
            if (crushForceText != null)
                crushForceText.text = $"Crush Force: {value:F1}";
        }
    }

    void OnParticleOptimizationChanged(bool value)
    {
        if (currentParameters != null)
            currentParameters.particleOptimization = value;
    }

    void OnAdaptiveSolverChanged(bool value)
    {
        if (currentParameters != null)
            currentParameters.adaptiveSolver = value;
    }

    void OnPerformanceModeChanged(bool value)
    {
        if (currentParameters != null)
            currentParameters.performanceMode = value;
    }

    void OnBasketEnabledChanged(bool value)
    {
        if (currentParameters != null)
            currentParameters.basketEnabled = value;
    }

    void OnHammerEnabledChanged(bool value)
    {
        if (currentParameters != null)
        {
            currentParameters.hammerEnabled = value;
            UpdateHammerPanelVisibility();
        }
    }

    void OnKnifeEnabledChanged(bool value)
    {
        if (currentParameters != null)
            currentParameters.knifeEnabled = value;
    }

    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}