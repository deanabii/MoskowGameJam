#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhotoModeSystem.Editor
{
    public static class PhotoModeSceneSetup
    {
        [MenuItem("Tools/Photo Mode/Setup Photo Mode UI in Current Active Scene")]
        public static void SetupInActiveScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            BuildPhotoModeUIHierarchy(activeScene, false);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"<color=green>[PhotoModeSceneSetup] Fitur Photo Mode UI berhasil dipasang pada Active Scene: '{activeScene.name}'</color>");
        }

        [MenuItem("Tools/Photo Mode/Create Standalone PhotoMode UI Scene (Without Camera)")]
        public static void CreateStandaloneScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPhotoModeUIHierarchy(newScene, true);

            string sceneFolderPath = "Assets/PhotoModeSystem/Scenes";
            if (!Directory.Exists(sceneFolderPath))
            {
                Directory.CreateDirectory(sceneFolderPath);
            }

            string scenePath = Path.Combine(sceneFolderPath, "PhotoModeUI.unity");
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PhotoModeSceneSetup] Scene Standalone PhotoModeUI berhasil dibuat tanpa kamera di: {scenePath}</color>");
        }

        private static void BuildPhotoModeUIHierarchy(UnityEngine.SceneManagement.Scene targetScene, bool isStandalone)
        {
            // 1. Core Managers Root
            GameObject coreRoot = GameObject.Find("[PhotoMode_System]");
            if (coreRoot == null)
            {
                coreRoot = new GameObject("[PhotoMode_System]");
            }

            PhotoStorageManager storage = coreRoot.GetComponent<PhotoStorageManager>();
            if (storage == null) storage = coreRoot.AddComponent<PhotoStorageManager>();

            SocialMediaManager social = coreRoot.GetComponent<SocialMediaManager>();
            if (social == null) social = coreRoot.AddComponent<SocialMediaManager>();

            PhotoModeStateBridge bridge = coreRoot.GetComponent<PhotoModeStateBridge>();
            if (bridge == null) bridge = coreRoot.AddComponent<PhotoModeStateBridge>();

            PhotoMetadataDetector detector = coreRoot.GetComponent<PhotoMetadataDetector>();
            if (detector == null) detector = coreRoot.AddComponent<PhotoMetadataDetector>();

            // 2. Main UI Canvas (Cari canvas yang sudah ada di scene, atau buat baru)
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("[Canvas_PhotoModeUI]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 99;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // 3. Shutter Animation Overlay
            Transform existingCapture = canvasObj.transform.Find("PhotoCaptureUI");
            GameObject captureUIObj = existingCapture != null ? existingCapture.gameObject : new GameObject("PhotoCaptureUI", typeof(RectTransform), typeof(CanvasGroup));
            captureUIObj.transform.SetParent(canvasObj.transform, false);
            RectTransform captureRect = captureUIObj.GetComponent<RectTransform>();
            captureRect.anchorMin = Vector2.zero;
            captureRect.anchorMax = Vector2.one;
            captureRect.offsetMin = Vector2.zero;
            captureRect.offsetMax = Vector2.zero;

            CanvasGroup flashGroup = captureUIObj.GetComponent<CanvasGroup>();

            Transform existingFrame = captureUIObj.transform.Find("WhiteBorderFrame");
            GameObject photoFrameObj = existingFrame != null ? existingFrame.gameObject : new GameObject("WhiteBorderFrame", typeof(RectTransform), typeof(Image));
            photoFrameObj.transform.SetParent(captureUIObj.transform, false);
            Image borderImage = photoFrameObj.GetComponent<Image>();
            borderImage.color = Color.white;
            RectTransform frameRect = photoFrameObj.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.12f, 0.12f);
            frameRect.anchorMax = new Vector2(0.88f, 0.88f);

            Transform existingRaw = photoFrameObj.transform.Find("PreviewRawImage");
            GameObject rawImageObj = existingRaw != null ? existingRaw.gameObject : new GameObject("PreviewRawImage", typeof(RectTransform), typeof(RawImage));
            rawImageObj.transform.SetParent(photoFrameObj.transform, false);
            RawImage previewRaw = rawImageObj.GetComponent<RawImage>();
            RectTransform rawRect = rawImageObj.GetComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = new Vector2(12, 12);
            rawRect.offsetMax = new Vector2(-12, -12);

            PhotoAnimationController animController = captureUIObj.GetComponent<PhotoAnimationController>();
            if (animController == null) animController = captureUIObj.AddComponent<PhotoAnimationController>();
            animController.flashCanvasGroup = flashGroup;
            animController.photoFrameRect = frameRect;
            animController.previewRawImage = previewRaw;
            animController.detector = detector;

            // 4. Main TAB Overlay Window
            Transform existingTabMenu = canvasObj.transform.Find("MainTabMenuUI");
            GameObject mainTabObj = existingTabMenu != null ? existingTabMenu.gameObject : new GameObject("MainTabMenuUI", typeof(RectTransform), typeof(MainTabMenuUI));
            mainTabObj.transform.SetParent(canvasObj.transform, false);
            MainTabMenuUI tabMenu = mainTabObj.GetComponent<MainTabMenuUI>();

            Transform existingOverlay = mainTabObj.transform.Find("OverlayPanel");
            GameObject overlayPanel = existingOverlay != null ? existingOverlay.gameObject : new GameObject("OverlayPanel", typeof(RectTransform), typeof(Image));
            overlayPanel.transform.SetParent(mainTabObj.transform, false);
            Image bgOverlay = overlayPanel.GetComponent<Image>();
            bgOverlay.color = new Color(0.07f, 0.08f, 0.11f, 0.92f); // Sleek modern dark glass background
            RectTransform overlayRect = overlayPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;

            tabMenu.mainOverlayPanel = overlayPanel;

            // Top-Right Close Button ("X") for Main TAB Window
            Transform existingClose = overlayPanel.transform.Find("CloseButtonX");
            GameObject closeBtnObj = existingClose != null ? existingClose.gameObject : new GameObject("CloseButtonX", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(overlayPanel.transform, false);
            closeBtnObj.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
            RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-25, -25);
            closeRect.sizeDelta = new Vector2(55, 55);

            Transform existingCloseTxt = closeBtnObj.transform.Find("TextTMP");
            GameObject closeTextObj = existingCloseTxt != null ? existingCloseTxt.gameObject : new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeTxt = closeTextObj.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 28;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAlignmentOptions.Center;
            RectTransform closeTxtRect = closeTextObj.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;

            tabMenu.closeButtonX = closeBtnObj.GetComponent<Button>();

            // Left Sidebar Navigation Container
            Transform existingSidebar = overlayPanel.transform.Find("SidebarLeft");
            GameObject sidebarObj = existingSidebar != null ? existingSidebar.gameObject : new GameObject("SidebarLeft", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            sidebarObj.transform.SetParent(overlayPanel.transform, false);
            RectTransform sidebarRect = sidebarObj.GetComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0f, 0f);
            sidebarRect.anchorMax = new Vector2(0.22f, 1f);
            sidebarRect.offsetMin = new Vector2(25, 25);
            sidebarRect.offsetMax = new Vector2(-10, -25);
            sidebarObj.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.18f, 1f);

            VerticalLayoutGroup sidebarLayout = sidebarObj.GetComponent<VerticalLayoutGroup>();
            sidebarLayout.spacing = 15;
            sidebarLayout.padding = new RectOffset(15, 15, 35, 35);
            sidebarLayout.childControlHeight = false;
            sidebarLayout.childControlWidth = true;

            // Gallery Sidebar Tab Button (Image Icon + TextMeshProUGUI)
            var (galBtnObj, galIconImg, galTextTMP) = CreateSidebarTabButton("Galeri", sidebarObj.transform);
            tabMenu.galleryTabButton = galBtnObj.GetComponent<Button>();
            tabMenu.galleryTabIconImage = galIconImg;
            tabMenu.galleryTabTextTMP = galTextTMP;

            // Social Sidebar Tab Button (Image Icon + TextMeshProUGUI)
            var (socBtnObj, socIconImg, socTextTMP) = CreateSidebarTabButton("Sosial Media", sidebarObj.transform);
            tabMenu.socialTabButton = socBtnObj.GetComponent<Button>();
            tabMenu.socialTabIconImage = socIconImg;
            tabMenu.socialTabTextTMP = socTextTMP;

            // Main Content Container Area
            Transform existingContent = overlayPanel.transform.Find("ContentArea");
            GameObject contentArea = existingContent != null ? existingContent.gameObject : new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(overlayPanel.transform, false);
            RectTransform contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.22f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(15, 25);
            contentRect.offsetMax = new Vector2(-25, -90);

            // Gallery Panel
            Transform existingGalPanel = contentArea.transform.Find("GalleryPanel");
            GameObject galleryPanel = existingGalPanel != null ? existingGalPanel.gameObject : new GameObject("GalleryPanel", typeof(RectTransform), typeof(GalleryPanelUI));
            galleryPanel.transform.SetParent(contentArea.transform, false);
            RectTransform galRect = galleryPanel.GetComponent<RectTransform>();
            galRect.anchorMin = Vector2.zero;
            galRect.anchorMax = Vector2.one;
            GalleryPanelUI galUI = galleryPanel.GetComponent<GalleryPanelUI>();
            tabMenu.galleryPanelObj = galleryPanel;

            // Gallery Grid Content
            Transform existingGalGrid = galleryPanel.transform.Find("GridContent");
            GameObject gridParent = existingGalGrid != null ? existingGalGrid.gameObject : new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup));
            gridParent.transform.SetParent(galleryPanel.transform, false);
            RectTransform gridRect = gridParent.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            GridLayoutGroup gridGroup = gridParent.GetComponent<GridLayoutGroup>();
            gridGroup.cellSize = new Vector2(230, 165);
            gridGroup.spacing = new Vector2(18, 18);
            galUI.gridContentParent = gridParent.transform;

            // Zoomed Photo Detail Modal
            Transform existingDetailModal = overlayPanel.transform.Find("PhotoDetailModal");
            GameObject detailModalObj = existingDetailModal != null ? existingDetailModal.gameObject : new GameObject("PhotoDetailModal", typeof(RectTransform), typeof(Image), typeof(PhotoDetailUI));
            detailModalObj.transform.SetParent(overlayPanel.transform, false);
            detailModalObj.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
            RectTransform detailRect = detailModalObj.GetComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;

            PhotoDetailUI detailUI = detailModalObj.GetComponent<PhotoDetailUI>();
            detailUI.modalRoot = detailModalObj;
            detailUI.tabMenuUI = tabMenu;
            detailUI.galleryPanel = galUI;
            galUI.photoDetailModal = detailUI;

            // Top-Right Close Button ("X") for Photo Detail Modal
            Transform existingDetailClose = detailModalObj.transform.Find("CloseButtonX");
            GameObject detailCloseBtnObj = existingDetailClose != null ? existingDetailClose.gameObject : new GameObject("CloseButtonX", typeof(RectTransform), typeof(Image), typeof(Button));
            detailCloseBtnObj.transform.SetParent(detailModalObj.transform, false);
            detailCloseBtnObj.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
            RectTransform detailCloseRect = detailCloseBtnObj.GetComponent<RectTransform>();
            detailCloseRect.anchorMin = new Vector2(1f, 1f);
            detailCloseRect.anchorMax = new Vector2(1f, 1f);
            detailCloseRect.pivot = new Vector2(1f, 1f);
            detailCloseRect.anchoredPosition = new Vector2(-30, -30);
            detailCloseRect.sizeDelta = new Vector2(50, 50);

            Transform existingDetailCloseTxt = detailCloseBtnObj.transform.Find("TextTMP");
            GameObject detailCloseTextObj = existingDetailCloseTxt != null ? existingDetailCloseTxt.gameObject : new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailCloseTextObj.transform.SetParent(detailCloseBtnObj.transform, false);
            TextMeshProUGUI detailCloseTxt = detailCloseTextObj.GetComponent<TextMeshProUGUI>();
            detailCloseTxt.text = "X";
            detailCloseTxt.fontSize = 26;
            detailCloseTxt.fontStyle = FontStyles.Bold;
            detailCloseTxt.color = Color.white;
            detailCloseTxt.alignment = TextAlignmentOptions.Center;
            RectTransform detailCloseTxtRect = detailCloseTextObj.GetComponent<RectTransform>();
            detailCloseTxtRect.anchorMin = Vector2.zero;
            detailCloseTxtRect.anchorMax = Vector2.one;

            detailUI.closeButtonX = detailCloseBtnObj.GetComponent<Button>();

            // Zoomed RawImage View
            Transform existingZoomedImg = detailModalObj.transform.Find("ZoomedRawImage");
            GameObject zoomedImgObj = existingZoomedImg != null ? existingZoomedImg.gameObject : new GameObject("ZoomedRawImage", typeof(RectTransform), typeof(RawImage));
            zoomedImgObj.transform.SetParent(detailModalObj.transform, false);
            RectTransform zoomedRect = zoomedImgObj.GetComponent<RectTransform>();
            zoomedRect.anchorMin = new Vector2(0.08f, 0.08f);
            zoomedRect.anchorMax = new Vector2(0.62f, 0.92f);
            detailUI.zoomedRawImage = zoomedImgObj.GetComponent<RawImage>();

            // Detail Metadata Text (TMP)
            Transform existingMetaText = detailModalObj.transform.Find("MetadataTextTMP");
            GameObject metaTextObj = existingMetaText != null ? existingMetaText.gameObject : new GameObject("MetadataTextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            metaTextObj.transform.SetParent(detailModalObj.transform, false);
            RectTransform metaRect = metaTextObj.GetComponent<RectTransform>();
            metaRect.anchorMin = new Vector2(0.65f, 0.45f);
            metaRect.anchorMax = new Vector2(0.95f, 0.92f);
            TextMeshProUGUI metaTMP = metaTextObj.GetComponent<TextMeshProUGUI>();
            metaTMP.fontSize = 18;
            metaTMP.color = Color.white;
            detailUI.metadataText = metaTMP;

            // Detail Panel Tools Container (Hapus, Share, Back)
            Transform existingTools = detailModalObj.transform.Find("DetailTools");
            GameObject toolsObj = existingTools != null ? existingTools.gameObject : new GameObject("DetailTools", typeof(RectTransform), typeof(VerticalLayoutGroup));
            toolsObj.transform.SetParent(detailModalObj.transform, false);
            RectTransform toolsRect = toolsObj.GetComponent<RectTransform>();
            toolsRect.anchorMin = new Vector2(0.65f, 0.08f);
            toolsRect.anchorMax = new Vector2(0.95f, 0.40f);

            VerticalLayoutGroup toolsLayout = toolsObj.GetComponent<VerticalLayoutGroup>();
            toolsLayout.spacing = 12;

            GameObject delBtn = CreateActionButton("Hapus", new Color(0.8f, 0.2f, 0.2f, 1f), toolsObj.transform);
            detailUI.deleteButton = delBtn.GetComponent<Button>();

            GameObject shareBtn = CreateActionButton("Share", new Color(0.15f, 0.65f, 0.3f, 1f), toolsObj.transform);
            detailUI.shareButton = shareBtn.GetComponent<Button>();

            GameObject backBtn = CreateActionButton("Kembali", new Color(0.4f, 0.4f, 0.45f, 1f), toolsObj.transform);
            detailUI.backButton = backBtn.GetComponent<Button>();

            detailModalObj.SetActive(false);

            // Social Media Panel
            Transform existingSocialPanel = contentArea.transform.Find("SocialMediaPanel");
            GameObject socialPanel = existingSocialPanel != null ? existingSocialPanel.gameObject : new GameObject("SocialMediaPanel", typeof(RectTransform), typeof(SocialMediaPanelUI));
            socialPanel.transform.SetParent(contentArea.transform, false);
            RectTransform socRect = socialPanel.GetComponent<RectTransform>();
            socRect.anchorMin = Vector2.zero;
            socRect.anchorMax = Vector2.one;
            SocialMediaPanelUI socialUI = socialPanel.GetComponent<SocialMediaPanelUI>();
            tabMenu.socialPanelObj = socialPanel;

            // Social Feed Grid Parent
            Transform existingFeedGrid = socialPanel.transform.Find("FeedGrid");
            GameObject feedGridObj = existingFeedGrid != null ? existingFeedGrid.gameObject : new GameObject("FeedGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            feedGridObj.transform.SetParent(socialPanel.transform, false);
            RectTransform feedRect = feedGridObj.GetComponent<RectTransform>();
            feedRect.anchorMin = new Vector2(0f, 0f);
            feedRect.anchorMax = new Vector2(0.6f, 1f);
            GridLayoutGroup feedGrid = feedGridObj.GetComponent<GridLayoutGroup>();
            feedGrid.cellSize = new Vector2(190, 140);
            feedGrid.spacing = new Vector2(12, 12);
            socialUI.feedGridParent = feedGridObj.transform;

            // Notification UI Panel
            Transform existingNotifPanel = socialPanel.transform.Find("NotificationPanel");
            GameObject notifPanel = existingNotifPanel != null ? existingNotifPanel.gameObject : new GameObject("NotificationPanel", typeof(RectTransform), typeof(NotificationUIManager), typeof(VerticalLayoutGroup));
            notifPanel.transform.SetParent(socialPanel.transform, false);
            RectTransform notifRect = notifPanel.GetComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.62f, 0f);
            notifRect.anchorMax = new Vector2(1f, 1f);
            NotificationUIManager notifUI = notifPanel.GetComponent<NotificationUIManager>();
            notifUI.notificationContainer = notifPanel.transform;
            socialUI.notificationUI = notifUI;

            socialPanel.SetActive(false);

            // Event System Fix
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("[EventSystem]", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule), typeof(PhotoModeEventSystemFix));
            }
            else
            {
                UnityEngine.EventSystems.EventSystem existingEs = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (existingEs.GetComponent<PhotoModeEventSystemFix>() == null)
                {
                    existingEs.gameObject.AddComponent<PhotoModeEventSystemFix>();
                }
            }
        }

        private static (GameObject btnObj, Image iconImage, TextMeshProUGUI textTMP) CreateSidebarTabButton(string label, Transform parent)
        {
            Transform existingBtn = parent.Find($"Tab_{label}");
            if (existingBtn != null)
            {
                Image existingIcon = existingBtn.Find("IconImage")?.GetComponent<Image>();
                TextMeshProUGUI existingText = existingBtn.Find("LabelTMP")?.GetComponent<TextMeshProUGUI>();
                return (existingBtn.gameObject, existingIcon, existingText);
            }

            GameObject btnObj = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 95);
            btnObj.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);

            VerticalLayoutGroup layout = btnObj.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6;

            // Icon as Image Component
            GameObject iconObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnObj.transform, false);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = Color.white;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(38, 38);

            // Text as TextMeshProUGUI Component
            GameObject labelObj = new GameObject("LabelTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI lblTMP = labelObj.GetComponent<TextMeshProUGUI>();
            lblTMP.text = label;
            lblTMP.fontSize = 15;
            lblTMP.color = Color.white;
            lblTMP.alignment = TextAlignmentOptions.Center;

            return (btnObj, iconImg, lblTMP);
        }

        private static GameObject CreateActionButton(string label, Color color, Transform parent)
        {
            Transform existingBtn = parent.Find($"Btn_{label}");
            if (existingBtn != null) return existingBtn.gameObject;

            GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 45);
            btnObj.GetComponent<Image>().color = color;

            GameObject labelObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = labelObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 18;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            RectTransform lblRect = labelObj.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;

            return btnObj;
        }
    }
}
#endif
