using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ShowcaseManager : MonoBehaviour
{
    [Header("Showcase Scenarios")]
    public ShowcaseScenario[] scenarios;

    [Header("UI References")]
    public GameObject mainMenuUI;
    public GameObject scenarioUI;
    public GameObject parametersUI;

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu";
    public string showcaseSceneName = "Showcase";

    private ShowcaseScenario currentScenario;
    private bool isInShowcase = false;

    void Start()
    {
        // Initialize scenarios if not set
        if (scenarios == null || scenarios.Length == 0)
        {
            InitializeDefaultScenarios();
        }

        // Show main menu by default
        ShowMainMenu();
    }

    void InitializeDefaultScenarios()
    {
        scenarios = new ShowcaseScenario[]
        {
            new ShowcaseScenario
            {
                name = "Fruit Collection Paradise",
                description = "A relaxing fruit collection experience with beautiful physics",
                sceneName = "FruitCollection",
                thumbnail = null,
                parameters = new ShowcaseParameters
                {
                    fruitCount = 50,
                    fruitTypes = new string[] { "Apple", "Orange", "Banana", "Strawberry" },
                    gravity = new Vector3(0, -9.81f, 0),
                    basketEnabled = true,
                    hammerEnabled = false,
                    knifeEnabled = false,
                    particleOptimization = true,
                    adaptiveSolver = true,
                    solverIterations = 5,
                    timeStep = 0.02f
                }
            },
            new ShowcaseScenario
            {
                name = "Destruction Derby",
                description = "High-energy destruction with hammers and explosives",
                sceneName = "DestructionDerby",
                thumbnail = null,
                parameters = new ShowcaseParameters
                {
                    fruitCount = 100,
                    fruitTypes = new string[] { "Watermelon", "Pineapple", "Coconut" },
                    gravity = new Vector3(0, -12f, 0),
                    basketEnabled = false,
                    hammerEnabled = true,
                    knifeEnabled = true,
                    particleOptimization = true,
                    adaptiveSolver = true,
                    solverIterations = 3,
                    timeStep = 0.016f,
                    hammerSettings = new HammerSettings
                    {
                        pushThreshold = 1.5f,
                        deformThreshold = 4.0f,
                        crushThreshold = 7.0f,
                        pushForce = 5.0f,
                        deformForce = 12.0f,
                        crushForce = 25.0f
                    }
                }
            },
            new ShowcaseScenario
            {
                name = "Physics Sandbox",
                description = "Experiment with massive particle systems and physics",
                sceneName = "PhysicsSandbox",
                thumbnail = null,
                parameters = new ShowcaseParameters
                {
                    fruitCount = 500,
                    fruitTypes = new string[] { "Sphere", "Cube", "Complex" },
                    gravity = new Vector3(0, -15f, 0),
                    basketEnabled = true,
                    hammerEnabled = true,
                    knifeEnabled = true,
                    particleOptimization = true,
                    adaptiveSolver = true,
                    solverIterations = 2,
                    timeStep = 0.01f,
                    performanceMode = true
                }
            }
        };
    }

    public void ShowMainMenu()
    {
        isInShowcase = false;
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (scenarioUI != null) scenarioUI.SetActive(false);
        if (parametersUI != null) parametersUI.SetActive(false);
    }

    public void ShowScenarioSelection()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (scenarioUI != null) scenarioUI.SetActive(true);
        if (parametersUI != null) parametersUI.SetActive(false);
    }

    public void ShowParameters(ShowcaseScenario scenario)
    {
        currentScenario = scenario;
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (scenarioUI != null) scenarioUI.SetActive(false);
        if (parametersUI != null) parametersUI.SetActive(true);
    }

    public void StartScenario(ShowcaseScenario scenario)
    {
        currentScenario = scenario;
        isInShowcase = true;

        // Save scenario parameters
        PlayerPrefs.SetString("CurrentScenario", scenario.name);
        SaveScenarioParameters(scenario.parameters);

        // Load showcase scene
        SceneManager.LoadScene(showcaseSceneName);
    }

    public void StartCurrentScenario()
    {
        if (currentScenario != null)
        {
            StartScenario(currentScenario);
        }
    }

    public void ReturnToMainMenu()
    {
        if (isInShowcase)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            ShowMainMenu();
        }
    }

    void SaveScenarioParameters(ShowcaseParameters parameters)
    {
        PlayerPrefs.SetInt("FruitCount", parameters.fruitCount);
        PlayerPrefs.SetString("FruitTypes", string.Join(",", parameters.fruitTypes));
        PlayerPrefs.SetFloat("GravityX", parameters.gravity.x);
        PlayerPrefs.SetFloat("GravityY", parameters.gravity.y);
        PlayerPrefs.SetFloat("GravityZ", parameters.gravity.z);
        PlayerPrefs.SetInt("BasketEnabled", parameters.basketEnabled ? 1 : 0);
        PlayerPrefs.SetInt("HammerEnabled", parameters.hammerEnabled ? 1 : 0);
        PlayerPrefs.SetInt("KnifeEnabled", parameters.knifeEnabled ? 1 : 0);
        PlayerPrefs.SetInt("ParticleOptimization", parameters.particleOptimization ? 1 : 0);
        PlayerPrefs.SetInt("AdaptiveSolver", parameters.adaptiveSolver ? 1 : 0);
        PlayerPrefs.SetInt("SolverIterations", parameters.solverIterations);
        PlayerPrefs.SetFloat("TimeStep", parameters.timeStep);
        PlayerPrefs.SetInt("PerformanceMode", parameters.performanceMode ? 1 : 0);

        // Save hammer settings if available
        if (parameters.hammerSettings != null)
        {
            PlayerPrefs.SetFloat("PushThreshold", parameters.hammerSettings.pushThreshold);
            PlayerPrefs.SetFloat("DeformThreshold", parameters.hammerSettings.deformThreshold);
            PlayerPrefs.SetFloat("CrushThreshold", parameters.hammerSettings.crushThreshold);
            PlayerPrefs.SetFloat("PushForce", parameters.hammerSettings.pushForce);
            PlayerPrefs.SetFloat("DeformForce", parameters.hammerSettings.deformForce);
            PlayerPrefs.SetFloat("CrushForce", parameters.hammerSettings.crushForce);
        }

        PlayerPrefs.Save();
    }

    public ShowcaseParameters LoadScenarioParameters()
    {
        ShowcaseParameters parameters = new ShowcaseParameters();

        parameters.fruitCount = PlayerPrefs.GetInt("FruitCount", 50);
        string fruitTypesStr = PlayerPrefs.GetString("FruitTypes", "Apple,Orange,Banana");
        parameters.fruitTypes = fruitTypesStr.Split(',');
        parameters.gravity = new Vector3(
            PlayerPrefs.GetFloat("GravityX", 0),
            PlayerPrefs.GetFloat("GravityY", -9.81f),
            PlayerPrefs.GetFloat("GravityZ", 0)
        );
        parameters.basketEnabled = PlayerPrefs.GetInt("BasketEnabled", 1) == 1;
        parameters.hammerEnabled = PlayerPrefs.GetInt("HammerEnabled", 0) == 1;
        parameters.knifeEnabled = PlayerPrefs.GetInt("KnifeEnabled", 0) == 1;
        parameters.particleOptimization = PlayerPrefs.GetInt("ParticleOptimization", 1) == 1;
        parameters.adaptiveSolver = PlayerPrefs.GetInt("AdaptiveSolver", 1) == 1;
        parameters.solverIterations = PlayerPrefs.GetInt("SolverIterations", 5);
        parameters.timeStep = PlayerPrefs.GetFloat("TimeStep", 0.02f);
        parameters.performanceMode = PlayerPrefs.GetInt("PerformanceMode", 0) == 1;

        // Load hammer settings if available
        if (PlayerPrefs.HasKey("PushThreshold"))
        {
            parameters.hammerSettings = new HammerSettings
            {
                pushThreshold = PlayerPrefs.GetFloat("PushThreshold", 2.0f),
                deformThreshold = PlayerPrefs.GetFloat("DeformThreshold", 5.0f),
                crushThreshold = PlayerPrefs.GetFloat("CrushThreshold", 8.0f),
                pushForce = PlayerPrefs.GetFloat("PushForce", 3.0f),
                deformForce = PlayerPrefs.GetFloat("DeformForce", 8.0f),
                crushForce = PlayerPrefs.GetFloat("CrushForce", 15.0f)
            };
        }

        return parameters;
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

[System.Serializable]
public class ShowcaseScenario
{
    public string name;
    public string description;
    public string sceneName;
    public Sprite thumbnail;
    public ShowcaseParameters parameters;
}

[System.Serializable]
public class ShowcaseParameters
{
    [Header("Fruit Settings")]
    public int fruitCount = 50;
    public string[] fruitTypes = { "Apple", "Orange", "Banana" };

    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public bool particleOptimization = true;
    public bool adaptiveSolver = true;
    public int solverIterations = 5;
    public float timeStep = 0.02f;
    public bool performanceMode = false;

    [Header("Tools")]
    public bool basketEnabled = true;
    public bool hammerEnabled = false;
    public bool knifeEnabled = false;

    [Header("Hammer Settings")]
    public HammerSettings hammerSettings;
}

[System.Serializable]
public class HammerSettings
{
    public float pushThreshold = 2.0f;
    public float deformThreshold = 5.0f;
    public float crushThreshold = 8.0f;
    public float pushForce = 3.0f;
    public float deformForce = 8.0f;
    public float crushForce = 15.0f;
}