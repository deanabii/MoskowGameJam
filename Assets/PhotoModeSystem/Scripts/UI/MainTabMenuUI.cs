using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class MainTabMenuUI : MonoBehaviour
    {
        [Header("Root Window")]
        public GameObject mainOverlayPanel;
        public Button closeButtonX;

        [Header("Sidebar Tab Buttons (Image Icon + TMP Text)")]
        public Button galleryTabButton;
        public Image galleryTabIconImage;
        public TextMeshProUGUI galleryTabTextTMP;

        public Button socialTabButton;
        public Image socialTabIconImage;
        public TextMeshProUGUI socialTabTextTMP;

        [Header("Tab Panels")]
        public GameObject galleryPanelObj;
        public GameObject socialPanelObj;

        private void Start()
        {
            if (mainOverlayPanel != null)
            {
                mainOverlayPanel.SetActive(false);
            }

            if (closeButtonX != null)
            {
                closeButtonX.onClick.AddListener(CloseMenu);
            }

            if (galleryTabButton != null)
            {
                galleryTabButton.onClick.AddListener(OpenGalleryTab);
            }

            if (socialTabButton != null)
            {
                socialTabButton.onClick.AddListener(OpenSocialTab);
            }

            OpenGalleryTab();
        }

        private void Update()
        {
            // Toggle TAB Menu
            if (PhotoModeInputUtility.IsTabPressed())
            {
                // Disallow opening menu while photo animation is running
                if (PhotoAnimationController.Instance != null && PhotoAnimationController.Instance.isPhotoProcessing) return;

                bool targetState = mainOverlayPanel != null && !mainOverlayPanel.activeSelf;
                if (targetState)
                {
                    OpenMenu();
                }
                else
                {
                    CloseMenu();
                }
            }
        }

        public void OpenMenu()
        {
            if (mainOverlayPanel != null)
            {
                mainOverlayPanel.SetActive(true);
            }

            if (PhotoModeStateBridge.Instance != null)
            {
                PhotoModeStateBridge.Instance.SetPhotoModeOpen(true);
            }

            OpenGalleryTab();
        }

        public void CloseMenu()
        {
            if (mainOverlayPanel != null)
            {
                mainOverlayPanel.SetActive(false);
            }

            if (PhotoModeStateBridge.Instance != null)
            {
                PhotoModeStateBridge.Instance.SetPhotoModeOpen(false);
            }
        }

        public void OpenGalleryTab()
        {
            if (galleryPanelObj != null) galleryPanelObj.SetActive(true);
            if (socialPanelObj != null) socialPanelObj.SetActive(false);
        }

        public void OpenSocialTab()
        {
            if (galleryPanelObj != null) galleryPanelObj.SetActive(false);
            if (socialPanelObj != null) socialPanelObj.SetActive(true);
        }
    }
}
