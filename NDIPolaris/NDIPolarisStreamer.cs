// Based on sample C# code from NDI
// Modified by David Black

using System.Collections.Generic;
using UnityEngine;
using System;

// NDI
using NDI.CapiSample;
using NDI.CapiSample.Data;
using NDI.CapiSample.Protocol;
using System.Threading;

namespace NDI.CapiSampleApplication
{
    /// <summary>
    /// This sample program showcases a common connection process to receive pose data for Aurora and Vega products.
    /// </summary>
    public class NDIPolarisStreamer : MonoBehaviour
    {
        private Capi cAPI;
        private bool isReading = false;
        private bool bx2;
        private bool doneSetup = false;
        private FixedSizedQueue<Pose> poseQueue;
        public string COMPort;
        public string SROMFilePath= "Assets/NDIPolaris/sroms/C3HD.rom";
        public Matrix4x4 NDItoUnity;
        public UnityEngine.Transform publishedTransform;

        public void Start()
        {
            NDItoUnity = Matrix4x4.identity;
            poseQueue = new FixedSizedQueue<Pose>();
            poseQueue.Limit = 10;

            // Create a new CAPI instance based on the connection type
            cAPI = new CapiSerial(COMPort);

            // Use the same log output format as this sample application
            cAPI.SetLogger(log);
            //cAPI.LogTransit = true;

            // This code is very slow and blocking, so turf it to a different thread
            Thread initNDIThread = new Thread(() =>
            {
               if (!cAPI.Connect())
               {
                   log("Could not connect to " + cAPI.GetConnectionInfo());
                   return;
               }
               log("Connected");

                // Get the API Revision this will tell us if BX2 is supported.
               string revision = cAPI.GetAPIRevision();
               log("Revision: " + revision);

               if (!cAPI.Initialize())
               {
                   log("Could not initialize.");
                   return;
               }
               log("Initialized");

                // Initialize tool ports
               if (!InitializePorts(cAPI))
               {
                   return;
               }

               if (IsBX2Supported(cAPI.GetAPIRevision()))
                   bx2 = true;

               if (!cAPI.TrackingStart())
               {
                   log("Could not start tracking.");
                   return;
               }
               log("TrackingStarted");
               doneSetup = true;
           });
           initNDIThread.Start();
        }

        public void Update()
        {
            if (doneSetup && cAPI.IsConnected && !isReading)
            {
                isReading = true;
                log("Starting read thread");
                ReadThread();
            }

            bool gotNewPose = poseQueue.q.TryDequeue(out Pose newPose);

            if (gotNewPose)
            {
                publishedTransform.SetPositionAndRotation(newPose.position, newPose.rotation);
            }
        }

        private void ReadThread()
        {
            var reader = new Thread(() =>
            {
                while (cAPI.IsConnected)
                {
                    List<Tool> tools;
                    if (bx2)
                        tools = cAPI.SendBX2("--6d=tools");
                    else
                        tools = cAPI.SendBX();
                    foreach (var t in tools)
                    {
                        // Get transform of the tool
                        
                        CapiSample.Data.Transform probeTransform = t.transform;
                        UnityEngine.Vector3 pos = NDItoUnityVector(probeTransform.position)*0.001f;
                        UnityEngine.Quaternion quat = NDItoUnityQuaternion(probeTransform.orientation);
                        if (Mathf.Abs(pos.x) > 10000 || Mathf.Abs(pos.y) > 10000 || Mathf.Abs(pos.z) > 10000 || Mathf.Abs(quat.x) > 10 || Mathf.Abs(quat.y) > 10 || Mathf.Abs(quat.z) > 10 || Mathf.Abs(quat.w) > 10)
                            continue;
                        
                        UnityEngine.Vector3 scale = UnityEngine.Vector3.one; // NDI is in mm but Unity is in m
                        Matrix4x4 mat = Matrix4x4.TRS(pos, quat, scale);

                        // Change of coordinates
                        Matrix4x4 inUnityCoords = mat * NDItoUnity; // (Or NDItoUnity * mat ???)

                        // Set transform to this
                        Vector4 cl = inUnityCoords.GetColumn(3);
                        UnityEngine.Vector3 newPos = new UnityEngine.Vector3(cl.x, cl.y, cl.z);
                        UnityEngine.Quaternion newQuat = inUnityCoords.rotation;
                        poseQueue.Enqueue(new Pose(newPos, newQuat));
                    }
                }

                log("Disconnected while tracking.");
                log("Stopping reading");
                isReading = false;
            });
            reader.Start();
        }

        /// <summary>
        /// Convert CAPI Vector3 to Unity Vector3
        /// </summary>
        /// <param name="vec">CAPI Vector3 to be converted.</param>
        private UnityEngine.Vector3 NDItoUnityVector(CapiSample.Data.Vector3 vec)
        {
            return new UnityEngine.Vector3((float)vec.x, (float)vec.y, (float)vec.z);
        }

        /// <summary>
        /// Convert CAPI Quaternion to Unity Quaternion
        /// </summary>
        /// <param name="quat">CAPI quaternion to be converted.</param>
        private UnityEngine.Quaternion NDItoUnityQuaternion(CapiSample.Data.Quaternion quat)
        {
            return new UnityEngine.Quaternion((float)quat.qx, (float)quat.qy, (float)quat.qz, (float)quat.q0);
        }

        /// <summary>
        /// Timestamped log output function.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        public static void log(string message)
        {
            string time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            Debug.Log(time + " [NDI Polaris] " + message);
        }

        private bool InitializePorts(Capi cAPI)
        {
            // Polaris Section
            // ---
            // Request a new tool port handle so that we can load an SROM
            Port tool = cAPI.PortHandleRequest();
            if (tool == null)
            {
                log("Could not get available port for tool.");
            }
            else if (!tool.LoadSROM(SROMFilePath))
            {
                log("Could not load SROM file for tool.");
                return false;
            }
            // ---

            // Initialize all ports not currently initialized
            var ports = cAPI.PortHandleSearchRequest(PortHandleSearchType.NotInit);
            foreach (var port in ports)
            {
                if (!port.Initialize())
                {
                    log("Could not initialize port " + port.PortHandle + ".");
                    return false;
                }

                if (!port.Enable())
                {
                    log("Could not enable port " + port.PortHandle + ".");
                    return false;
                }
            }

            // List all enabled ports
            log("Enabled Ports:");
            ports = cAPI.PortHandleSearchRequest(PortHandleSearchType.Enabled);
            foreach (var port in ports)
            {
                port.GetInfo();
                log(port.ToString());
            }

            return true;
        }

        /// <summary>
        /// Check for BX2 command support.
        /// </summary>
        /// <param name="apiRevision">API revision string returned by CAPI.GetAPIRevision()</param>
        /// <returns>True if supported.</returns>
        private static bool IsBX2Supported(string apiRevision)
        {
            // Refer to the API guide for how to interpret the APIREV response
            char deviceFamily = apiRevision[0];
            int majorVersion = int.Parse(apiRevision.Substring(2, 3));

            // As of early 2017, the only NDI device supporting BX2 is the Vega
            // Vega is a Polaris device with API major version 003
            if (deviceFamily == 'G' && majorVersion >= 3)
            {
                return true;
            }

            return false;
        }

        private void OnApplicationQuit()
        {
            if (!cAPI.TrackingStop())
            {
                log("Could not stop tracking.");
                return;
            }
            log("TrackingStopped");

            if (!cAPI.Disconnect())
            {
                log("Could not disconnect.");
                return;
            }
            log("Disconnected");
        }
    }
}