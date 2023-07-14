using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using BepInEx.Configuration;

namespace AudioSurf2VRmod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class VrModRendering : BaseUnityPlugin
    {
        public static VrModRendering instance;

        //Bepinex config file variables
        public ConfigEntry<bool> vrScaleAlternative;
        public ConfigEntry<Vector3> vrScale;
        public ConfigEntry<Vector3> cameraPosition;
        public ConfigEntry<Quaternion> cameraRotation;
        public ConfigEntry<Vector3> uiPosition;
        public ConfigEntry<Vector3> uiScale;

        //Hub scene variables
        private Camera mainCamera_UIscene;
        private Camera navBarCam;
        private Camera camera_AspectBackground;
        private Camera camera;
        private Camera clippedScrollList_Camera;
        private Camera uiCam;

        private Vector3 cameraPlaneOffset = new Vector3(0, 0, -0.2f);
        private Vector3 actualCameraPlaneOffset = Vector3.zero;

        public Transform uiVRrender;
        public int uiWidthOffset = 60;
        public int uiHeightOffset = 32;

        public GameObject rideSummary;
        public RenderTexture rideSummaryTargetTexture;


        //Ingame vars
        private Camera cameraHead; 
        private Camera vrCamera;
        private Vector3 vrCameraOffset = new Vector3(0, 0, 1000);

        private GameObject vrScaleCamera;
        //private GameObject gameplayCameras;
        public GameObject cameraRig;

        private Camera pauseMenuCamera;

        public GameObject settingsRender;
        public Dictionary<RenderTexture, Camera> renderTextures = new Dictionary<RenderTexture, Camera>();
        public RenderTexture settingsTargetTexture;

        public bool gameStarted;

        //dusk skin glow "fix"
        private GameObject carLights;


        private void Awake()
        {
            XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);

            vrScaleAlternative = Config.Bind("General.VR",
                            "Alternative VR Scaling mod",
                            false,
                            "Alternative Scaling mod, allows for better scaling tweaks but messes with the camera movements.");

            vrScale = Config.Bind("General.VR",
                            "VR Scale",
                            new Vector3(1f, 1f, 1f),
                            "Ingame hotkey : LeftShift + U to increase, LeftShift + J to decrease. Allows to change your ingame size. Lowering it makes you tiny and the game will look bigger, Increasing it makes you huge and the world will look tiny");

            cameraPosition = Config.Bind("General.VR",
                            "Camera position",
                            new Vector3(0f, 0f, 0f),
                            "Ingame hotkey : I, J, K, L to move the view horizontally, LeftShift + I and LeftShift + K to move it vertically.");

            cameraRotation = Config.Bind("General.VR",
                            "Camera Rotation",
                            Quaternion.identity,
                            "Ingame hotkey : LeftShift + O and LeftShift + L to tilt the view.");

            uiPosition = Config.Bind("General.UI",
                            "UI Position",
                            new Vector3(0f, 0.5f, 2f),
                            "Ingame hotkey : LeftCtrl + I and LeftCtrl + K to move the UI forward and backward");

            uiScale = Config.Bind("General.UI",
                            "UI Scale",
                            new Vector3(0.02f, 0.02f, 0.02f),
                            "Ingame hotkey : LeftCtrl + U and LeftCtrl + J to increase or decrease the UI scale");


            instance = this;


            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!"); 
            Harmony.CreateAndPatchAll(typeof(HarmonyAudioSurf));
        }

        private void Start()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Application.targetFrameRate = 90;
     
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} Ingame framerate set to 90fps");
        }

        private void Update()
        {
            if (cameraRig != null)
            {
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.I))
                {
                    cameraRig.transform.localPosition += Vector3.forward / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.K))
                {
                    cameraRig.transform.localPosition += Vector3.back / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (!Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.L))
                {
                    cameraRig.transform.localPosition += Vector3.right / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.J))
                {
                    cameraRig.transform.localPosition += Vector3.left / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.I))
                {
                    cameraRig.transform.localPosition += Vector3.up / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.K))
                {
                    cameraRig.transform.localPosition += Vector3.down / 10 / cameraRig.transform.localScale.y;
                    cameraPosition.Value = cameraRig.transform.localPosition;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.U))
                {
                    vrScaleCamera.transform.localScale *= 1.1f;
                    vrScale.Value = vrScaleCamera.transform.localScale;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.J))
                {
                    vrScaleCamera.transform.localScale *= 0.9f;
                    if (vrScaleCamera.transform.localScale.y < 0.0001)
                        vrScaleCamera.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
                    vrScale.Value = vrScaleCamera.transform.localScale;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.O))
                {
                    cameraRig.transform.localRotation *= Quaternion.Euler(Vector3.left);
                    cameraRotation.Value = cameraRig.transform.localRotation;
                }
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.L))
                {
                    cameraRig.transform.localRotation *= Quaternion.Euler(Vector3.right);
                    cameraRotation.Value = cameraRig.transform.localRotation;
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.U))
                {
                    uiVRrender.transform.localScale *= 1.1f;
                    uiScale.Value = uiVRrender.transform.localScale;
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.J))
                {
                    uiVRrender.transform.localScale *=0.9f;
                    uiScale.Value = uiVRrender.transform.localScale;
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.I))
                {
                    uiVRrender.transform.localPosition += Vector3.forward / 100;
                    uiPosition.Value = uiVRrender.transform.localPosition;
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.K))
                {
                    uiVRrender.transform.localPosition += Vector3.back / 100;
                    uiPosition.Value = uiVRrender.transform.localPosition;
                }

                if (Input.GetKey(KeyCode.Backspace))
                {
                    uiVRrender.transform.localPosition = (Vector3)uiPosition.DefaultValue;
                    uiVRrender.transform.localScale = (Vector3)uiScale.DefaultValue;
                    cameraRig.transform.localRotation = (Quaternion)cameraRotation.DefaultValue;
                    cameraRig.transform.localPosition = (Vector3)cameraPosition.DefaultValue;
                    vrScaleCamera.transform.localScale = (Vector3)vrScale.DefaultValue;
                    uiPosition.Value = (Vector3)uiPosition.DefaultValue;
                    uiScale.Value = (Vector3)uiScale.DefaultValue;
                    cameraRotation.Value = (Quaternion)cameraRotation.DefaultValue;
                    cameraPosition.Value = (Vector3)cameraPosition.DefaultValue;
                    vrScale.Value = (Vector3)vrScale.DefaultValue;
                }
            }
        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            
            if (scene.name == "Hub")
            {
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Hub scene loaded");

                if (renderTextures.Count > 0)
                {
                    renderTextures.Clear();
                    Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Render Textures cleared");
                }
                    
                gameStarted = false;
                actualCameraPlaneOffset = Vector3.zero;

                uiVRrender = new GameObject("uiVRrender").transform;

                HubCamerasFinder();

                CameraBypass(mainCamera_UIscene, 0);
                CameraBypass(navBarCam, 1);
                CameraBypass(camera_AspectBackground, 2);
                CameraBypass(camera, 3);
                CameraBypass(clippedScrollList_Camera, 4);
                
                VRCameraSetup();

                if (XRSettings.enabled)
                    uiVRrender.transform.localPosition = new Vector3(0, 0, 1060);
                else
                    uiVRrender.transform.localPosition = new Vector3(0, 0, 60);

                uiVRrender.transform.localRotation = Quaternion.Euler(new Vector3(90, 180, 0));
            }

            
            if (scene.name == "GameplayTricky")
            {
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : GameplayTricky scene loaded");
                if (renderTextures.Count > 0)
                {
                    renderTextures.Clear();
                    Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Render Textures cleared");
                }

                uiVRrender = new GameObject("uiVRrender").transform;
                if (vrScaleAlternative.Value)
                    vrScaleCamera = GameObject.Find("CameraHolder1");
                else
                    vrScaleCamera = GameObject.Find("GameplayCameras"); 
                cameraRig = GameObject.Find("[CameraRig]");

                try
                {
                    GameObject.Find("BlitterCam_CloseCam").SetActive(false);
                }
                catch
                {
                }
                

                GameplayCameraFinder();
                CameraBypass(uiCam, 5);
                CameraBypass(navBarCam, 1);
                CameraBypass(pauseMenuCamera, 6);


                cameraHead = GameObject.Find("Camera (head)").GetComponent<Camera>();
                cameraHead.enabled = false;
            }

        }


        private void GameplayCameraFinder()
        {
            try
            {
                navBarCam = GameObject.Find("NavBarCam").GetComponent<Camera>();
                navBarCam.clearFlags = CameraClearFlags.SolidColor;
                navBarCam.backgroundColor = Color.clear;
                uiCam = GameObject.Find("UI_Cam").GetComponent<Camera>();
                uiCam.clearFlags = CameraClearFlags.SolidColor;
                uiCam.backgroundColor = Color.clear;
                pauseMenuCamera = GameObject.Find("HUD_PauseMenu").GetComponentInChildren<Camera>();
                pauseMenuCamera.clearFlags = CameraClearFlags.SolidColor;
                pauseMenuCamera.backgroundColor = Color.clear;

                Camera.main.nearClipPlane = 0.001f;
                Camera.main.farClipPlane = 1000000f;
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + uiCam.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + navBarCam.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + pauseMenuCamera.gameObject.name);
            }
            catch (NullReferenceException error)
            {
                Logger.LogInfo("One or several required cameras couldn't be found in Gameplay Scene. Error : " + error);
            }
        }

        private void HubCamerasFinder()
        {
            try
            {
                mainCamera_UIscene = GameObject.Find("Main Camera_UI scene").GetComponent<Camera>();
                mainCamera_UIscene.clearFlags = CameraClearFlags.SolidColor;
                mainCamera_UIscene.backgroundColor = Color.clear;
                navBarCam = GameObject.Find("NavBarCam").GetComponent<Camera>();
                navBarCam.clearFlags = CameraClearFlags.SolidColor;
                navBarCam.backgroundColor = Color.clear;
                camera_AspectBackground = GameObject.Find("Camera_AspectBackground").GetComponent<Camera>();
                camera_AspectBackground.clearFlags = CameraClearFlags.SolidColor;
                camera_AspectBackground.backgroundColor = Color.clear;
                camera = GameObject.Find("Camera").GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                clippedScrollList_Camera = GameObject.Find("ClippedScrollList_Camera").GetComponent<Camera>();
                clippedScrollList_Camera.clearFlags = CameraClearFlags.SolidColor;
                clippedScrollList_Camera.backgroundColor = Color.clear;



                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + mainCamera_UIscene.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + navBarCam.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + camera_AspectBackground.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + camera.gameObject.name);
                Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} : Camera found " + clippedScrollList_Camera.gameObject.name);
            }
            catch (NullReferenceException error)
            {
                Logger.LogInfo("A camera couldn't be found in Hub Scene. Error : " + error);
            }

        }


        public void CameraBypass(Camera camera, int cameraIndex)
        {
            GameObject uiPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            uiPlane.name = camera.name;
            uiPlane.transform.localScale = new Vector3(16f, 1f, 9f);
            uiPlane.transform.SetParent(uiVRrender, true);
            if (gameStarted)
            {
                uiVRrender.transform.SetParent(cameraRig.transform, true);
            }
            else
                uiVRrender.transform.SetParent(Camera.main.transform, true);

            //var texture = new RenderTexture(AspectUtility.screenWidth, AspectUtility.screenHeight, 16, RenderTextureFormat.ARGB32);
            var texture = new RenderTexture(AspectUtility.screenWidth + AspectUtility.xOffset + uiWidthOffset, AspectUtility.screenHeight + AspectUtility.yOffset + uiHeightOffset, 16, RenderTextureFormat.ARGB32);
            texture.name = camera.name + "RenderTexture" + cameraIndex;

            MeshRenderer renderer = uiPlane.GetComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Alpha Blended"));
            renderer.material.mainTexture = texture;
            camera.targetTexture = texture;
            renderer.sortingOrder = 1000;

            actualCameraPlaneOffset -= cameraPlaneOffset;

            uiPlane.transform.position = actualCameraPlaneOffset;

            if (cameraIndex == 100)
            {
                uiPlane.transform.localPosition = new Vector3(0, 1, 0);
                uiPlane.transform.localRotation = Quaternion.identity;
                uiPlane.transform.localScale = new Vector3(16f, 1f, 9f);
                settingsRender = uiPlane;
                settingsTargetTexture = texture;
            }
            else if (cameraIndex == 10)
            {
                uiPlane.transform.localPosition = new Vector3(0, 1, 0);
                uiPlane.transform.localRotation = Quaternion.identity;
                uiPlane.transform.localScale = new Vector3(16f, 1f, 9f);
                rideSummary = uiPlane;
                rideSummaryTargetTexture = texture;
            }
            else
            {
                renderTextures.Add(texture, camera);
            }
        }

        private void VRCameraSetup()
        {
            GameObject vrCameraHolderGO = new GameObject("VRCameraHolder");
            GameObject vrCameraGO = new GameObject("VRCamera");
            vrCamera = vrCameraGO.AddComponent<Camera>();

            vrCameraGO.transform.SetParent(vrCameraHolderGO.transform, true);

            
            if (XRSettings.enabled)
            {
                vrCameraHolderGO.transform.position += vrCameraOffset;
            }

            vrCamera.fieldOfView = 90f;
            vrCamera.nearClipPlane = 0.001f;
            vrCamera.farClipPlane = 1000000f;
            vrCamera.transform.position = new Vector3(0f, 1.8f, 0f);
            vrCamera.clearFlags = CameraClearFlags.SolidColor;
            vrCamera.backgroundColor = Color.black;
        }

        private void FixThrusters()
        {
            Thruster[] mainThrusters = GameObject.FindObjectsOfType<Thruster>();
            ThrusterClone[] thrusterClones = GameObject.FindObjectsOfType<ThrusterClone>();

            foreach (Thruster thruster in mainThrusters)
            {
                thruster.gameObject.layer = 0;
                thruster.GetComponent<Renderer>().sortingOrder = 50;
                Debug.LogError(thruster.gameObject);
            }
            foreach (ThrusterClone thrusterClone in thrusterClones)
            {
                thrusterClone.gameObject.layer = 0;
                thrusterClone.GetComponent<Renderer>().sortingOrder = 50;
                Debug.LogError(thrusterClone.gameObject);
            }
        }

        private void FixVehicle()
        {
            Renderer vehicleRenderer = GameObject.FindObjectOfType<Vehicle>().GetComponentInChildren<MeshRenderer>();
            vehicleRenderer.sortingOrder = 50;
            vehicleRenderer.gameObject.layer = 0;
        }


        public static class HarmonyAudioSurf
        {
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(NavFrame), "Clicked_Options")]
            [HarmonyPatch(typeof(PauseScreenManager), "OpenOptions")]
            public static void PostfixSettingsOpen()
            {
                if (!VrModRendering.instance.settingsRender)
                {
                    Camera cam = GameObject.Find("SettingsPopup(Clone)").GetComponentInChildren<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.clear;
                    VrModRendering.instance.CameraBypass(cam, 100);
                }
                else
                {
                    Camera cam = GameObject.Find("SettingsPopup(Clone)").GetComponentInChildren<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.clear;
                    cam.targetTexture = VrModRendering.instance.settingsTargetTexture;
                    VrModRendering.instance.settingsRender.SetActive(true);
                }
                    

            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Settings), "Clicked_OK")]
            public static void PostfixSettingsClose()
            {
                VrModRendering.instance.settingsRender.SetActive(false);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RideSummaryManager), "OnEnable")]
            public static void PostfixResults()
            {
                if (!VrModRendering.instance.rideSummary)
                {
                    Camera cam = GameObject.Find("RideSummary(Clone)").GetComponentInChildren<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.clear;
                    VrModRendering.instance.CameraBypass(cam, 10);
                }
                else
                {
                    Camera cam = GameObject.Find("RideSummary(Clone)").GetComponentInChildren<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.clear;
                    cam.targetTexture = VrModRendering.instance.rideSummaryTargetTexture;
                    VrModRendering.instance.settingsRender.SetActive(true);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RideSummaryManager), "CloseMe")]
            public static void PostfixCloseResults()
            {
                VrModRendering.instance.rideSummary.SetActive(false);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AspectUtility), "SetCamera")]
            public static void PostfixResolutionChanged()
            {
                if (VrModRendering.instance.renderTextures.Count > 0)
                {
                    foreach (KeyValuePair<RenderTexture, Camera> item in VrModRendering.instance.renderTextures)
                    {
                        item.Key.Release();
                        item.Key.width = 
                        item.Key.width = AspectUtility.screenWidth + AspectUtility.xOffset + VrModRendering.instance.uiWidthOffset;
                        item.Key.height = AspectUtility.screenHeight + AspectUtility.yOffset + VrModRendering.instance.uiHeightOffset;
                        item.Value.targetTexture = item.Key;
                        VrModRendering.instance.Logger.LogInfo(item.Key + " " + item.Value);
                    }
                }
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(Vehicle), "OnSetVehicle")]
            public static void PostfixPreGameStart()
            {
                //Fix dusk lights glow :
                try
                {
                    VrModRendering.instance.carLights = GameObject.Find("Player1/Vehicle/RenderedObject");
                    if (VrModRendering.instance.carLights != null)
                    {
                        Transform light1 = VrModRendering.instance.carLights.transform.GetChild(2);
                        Transform light2 = VrModRendering.instance.carLights.transform.GetChild(3);
                        light1.GetComponent<MeshRenderer>().enabled = false;
                        light2.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
                catch
                {
                    return;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AttachToHighway_NoRotation), "Start")]
            public static void PostfixCameraSettings()
            {
                VrModRendering.instance.uiVRrender.transform.localScale = (Vector3)VrModRendering.instance.uiScale.DefaultValue;
                VrModRendering.instance.uiVRrender.transform.localPosition = (Vector3)VrModRendering.instance.uiPosition.DefaultValue;
                VrModRendering.instance.uiVRrender.transform.localRotation = Quaternion.Euler(new Vector3(100, 180, 0));
                VrModRendering.instance.vrScaleCamera.transform.localScale = (Vector3)VrModRendering.instance.vrScale.DefaultValue;
                VrModRendering.instance.cameraRig.transform.localPosition = (Vector3)VrModRendering.instance.cameraPosition.DefaultValue;
                VrModRendering.instance.cameraRig.transform.localRotation = Quaternion.identity;

                VrModRendering.instance.uiVRrender.transform.localScale = VrModRendering.instance.uiScale.Value;
                VrModRendering.instance.uiVRrender.transform.localPosition = VrModRendering.instance.uiPosition.Value;
                VrModRendering.instance.uiVRrender.transform.localRotation = Quaternion.Euler(new Vector3(100, 180, 0));
                VrModRendering.instance.vrScaleCamera.transform.localScale = VrModRendering.instance.vrScale.Value;
                VrModRendering.instance.cameraRig.transform.localPosition = VrModRendering.instance.cameraPosition.Value;
                VrModRendering.instance.cameraRig.transform.localRotation = VrModRendering.instance.cameraRotation.Value;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(LobbyUI), "Play")]
            public static void PostfixGameStart()
            {
                VrModRendering.instance.gameStarted = true;
                VrModRendering.instance.uiVRrender.transform.SetParent(VrModRendering.instance.cameraRig.transform, true);
                try
                {
                    VrModRendering.instance.FixThrusters();
                    VrModRendering.instance.FixVehicle();
                }
                catch
                {
                }
            }
        }
    }
}
