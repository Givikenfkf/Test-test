using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace WinlatorXRBridge
{
    [BepInPlugin("com.yourname.winlatorxrbridge", "WinlatorXR Bridge", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private WinlatorXRListener _listener;
        private string _versionPath = @"Z:\tmp\xr\version";

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("WinlatorXRBridge Awake");

            // read API version (if available)
            try
            {
                if (File.Exists(_versionPath))
                {
                    var v = File.ReadAllText(_versionPath).Trim();
                    Log.LogInfo($"WinlatorXR version file found: '{v}'");
                }
                else
                {
                    Log.LogWarning($"WinlatorXR version file not found at {_versionPath}. You should create it with content '0.2'.");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Could not read version file: {ex}");
            }

            // Create listener and subscribe
            _listener = new WinlatorXRListener(port: 7278);
            _listener.OnFrame += OnFrameReceived;
            _listener.OnLog += s => Log.LogInfo("[Listener] " + s);
            _listener.OnError += e => Log.LogError("[Listener] " + e);

            // start listener
            try
            {
                _listener.Start();
                Log.LogInfo("WinlatorXRListener started on 127.0.0.1:7278");
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to start WinlatorXRListener: " + ex);
            }
        }

        private void OnDestroy()
        {
            Log.LogInfo("WinlatorXRBridge OnDestroy - stopping listener");
            try
            {
                if (_listener != null)
                {
                    _listener.OnFrame -= OnFrameReceived;
                    _listener.Stop();
                    _listener.Dispose();
                    _listener = null;
                }
            }
            catch (Exception ex)
            {
                Log.LogError("Error disposing listener: " + ex);
            }
        }

        // Called on Unity main thread when a frame arrives (we schedule actual Unity changes here)
        private void OnFrameReceived(XRFrame frame)
        {
            // Only log a short summary to avoid spamming the logfile
            Log.LogInfo($"Frame received: LPos={frame.LPos.X:F2},{frame.LPos.Y:F2},{frame.LPos.Z:F2} " +
                        $"RPos={frame.RPos.X:F2},{frame.RPos.Y:F2},{frame.RPos.Z:F2} " +
                        $"HPos={frame.HPos.X:F2},{frame.HPos.Y:F2},{frame.HPos.Z:F2} Buttons='{frame.ButtonStr}'");

            // TODO: map frame values into Gorilla Tag objects here.
            // Example: schedule a Unity main-thread action to update transforms:
            // UnityMainThreadDispatcher.Enqueue(() => ApplyFrameToGameObjects(frame));
            //
            // I don't auto-manipulate game state here because Gorilla Tag modding requires
            // careful hooking and you know the exact places to apply transforms.
        }

        // Example placeholder (not called by default)
        private void ApplyFrameToGameObjects(XRFrame frame)
        {
            // Example: find a GameObject and set transform (this is for demonstration only)
            // var go = GameObject.Find("WinlatorHeadProxy");
            // if (go != null) go.transform.position = new Vector3(frame.HPos.X, frame.HPos.Y, frame.HPos.Z);
        }
    }
}
