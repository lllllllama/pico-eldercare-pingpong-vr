using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class RehabModeSelectUI : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject rehabTrainingSelectPanel;
        public GameObject rehabTrainingPanel;
        public GameObject trainingResultPanel;

        public Button rehabButton;
        public Button baduanjinButton;
        public Button taiChiButton;
        public Button backButton;
        public ModuleHomeMenu homeMenu;
        public ComfortWorldSpaceUIPlacer uiPlacer;

        public RehabSessionManager sessionManager;
        public bool showTrainingSelectOnStart = true;
        public bool placeUiOnStart = true;
        public bool placeUiOnMainMenuOpen = true;

        private void Awake()
        {
            ResolveReferences();
            BindButtonEvents();
        }

        private void Start()
        {
            if (showTrainingSelectOnStart)
            {
                ShowTrainingSelectPanel();
            }
            else
            {
                ShowMainMenuPanel();
            }

            if (placeUiOnStart)
            {
                PlaceUiInFrontOfUser();
            }
        }

        public void ShowMainMenuPanel()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, false);

            if (placeUiOnMainMenuOpen)
            {
                PlaceUiInFrontOfUser();
            }
        }

        public void ShowTrainingSelectPanel()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, true);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, false);
        }

        public void StartBaduanjinTraining()
        {
            StartTraining(RehabTrainingType.Baduanjin);
        }

        public void StartTaiChiTraining()
        {
            StartTraining(RehabTrainingType.TaiChi);
        }

        public void ShowTrainingResultPanel()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, true);
        }

        public void ReturnToMainEntry()
        {
            ResolveReferences();

            if (homeMenu != null)
            {
                homeMenu.LoadMainEntry();
                return;
            }

            ShowMainMenuPanel();
        }

        public void ResetUiPosition()
        {
            PlaceUiInFrontOfUser();
        }

        private void StartTraining(RehabTrainingType trainingType)
        {
            ResolveReferences();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, true);
            SetPanelActive(trainingResultPanel, false);

            if (sessionManager != null)
            {
                sessionManager.StartTraining(trainingType);
            }
            else
            {
                Debug.LogError("Cannot start rehab training because RehabSessionManager is not assigned.");
            }
        }

        private void ResolveReferences()
        {
            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }

            if (homeMenu == null)
            {
                homeMenu = FindObjectOfType<ModuleHomeMenu>(true);
            }

            if (uiPlacer == null)
            {
                uiPlacer = GetComponentInParent<ComfortWorldSpaceUIPlacer>();
            }
        }

        private void PlaceUiInFrontOfUser()
        {
            ResolveReferences();

            if (uiPlacer != null)
            {
                uiPlacer.PlaceInFrontOfUser();
            }
        }

        private void BindButtonEvents()
        {
            if (rehabButton != null)
            {
                rehabButton.onClick.RemoveListener(ShowTrainingSelectPanel);
                rehabButton.onClick.AddListener(ShowTrainingSelectPanel);
            }

            if (baduanjinButton != null)
            {
                baduanjinButton.onClick.RemoveListener(StartBaduanjinTraining);
                baduanjinButton.onClick.AddListener(StartBaduanjinTraining);
            }

            if (taiChiButton != null)
            {
                taiChiButton.onClick.RemoveListener(StartTaiChiTraining);
                taiChiButton.onClick.AddListener(StartTaiChiTraining);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenuPanel);
                backButton.onClick.RemoveListener(ReturnToMainEntry);
                backButton.onClick.AddListener(ReturnToMainEntry);
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}
