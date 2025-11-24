using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class GPSManager : MonoBehaviour
{
    [Header("GPS Settings")]
    public float desiredAccuracy = 1f;
    public float updateDistance = 0.1f;
    
    [Header("Data Model")]
    public GPSDataModel gpsData = new GPSDataModel();
    
    private AndroidJavaClass pluginClass;
    private float currentAccuracy = 999f;
    
    // События для уведомления внешних слушателей
    public event Action OnInitialized;
    public event Action<string> OnStatusMessage;
    public event Action OnTrackingStarted;
    public event Action OnTrackingStopped;
    public event Action OnDataUpdated;
    
    void Start()
    {
        Debug.Log("GPSManager: Initializing...");
        InitializePlugin();
        SetupDataModelEvents();
        StartCoroutine(DelayedInit());
    }

    void SetupDataModelEvents()
    {
        gpsData.OnDataUpdated += () => OnDataUpdated?.Invoke();
        gpsData.OnTrackingStateChanged += () => 
        {
            if (gpsData.isTracking)
                OnTrackingStarted?.Invoke();
            else
                OnTrackingStopped?.Invoke();
        };
    }

    IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(1);
        
        if (HasLocationPermissions())
        {
            Debug.Log("GPSManager: Permissions granted, starting GPS");
            StartGPS();
        }
        else
        {
            Debug.Log("GPSManager: Requesting permissions");
            RequestPermissions();
            gpsData.SetStatus("Need location permissions");
        }
        
        OnInitialized?.Invoke();
    }
    
    void Update()
    {
        if (Time.frameCount % 600 == 0 && !gpsData.isTracking && HasLocationPermissions())
        {
            Debug.Log("GPSManager: Auto-restarting GPS");
            StartGPS();
        }
    }
    
    void InitializePlugin()
    {
        try
        {
            pluginClass = new AndroidJavaClass("com.yourcompany.gpsplugin.HighAccuracyGPSPlugin");
            pluginClass.CallStatic("Initialize");
            Debug.Log("GPSManager: Plugin initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("GPSManager: Failed to initialize plugin - " + e.Message);
        }
    }
    
    void RequestPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        
        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
    }
    
    public bool HasLocationPermissions()
    {
        return Permission.HasUserAuthorizedPermission(Permission.FineLocation) &&
               Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
    }
    
    public void StartGPS()
    {
        if (!HasLocationPermissions())
        {
            Debug.LogWarning("GPSManager: No permissions, requesting...");
            RequestPermissions();
            return;
        }
        
        if (gpsData.isTracking)
        {
            Debug.Log("GPSManager: Already tracking");
            return;
        }
        
        if (pluginClass == null)
        {
            Debug.LogError("GPSManager: Plugin not initialized");
            return;
        }
        
        try
        {
            Debug.Log("GPSManager: Starting GPS...");
            pluginClass.CallStatic("StartLocationUpdates");
            gpsData.SetTrackingState(true);
            gpsData.SetStatus("Starting GPS...");
        }
        catch (Exception e)
        {
            Debug.LogError("GPSManager: Error starting GPS - " + e.Message);
            gpsData.SetStatus("Start error: " + e.Message);
        }
    }
    
    public void StopGPS()
    {
        if (!gpsData.isTracking) return;
        
        try
        {
            Debug.Log("GPSManager: Stopping GPS...");
            pluginClass.CallStatic("StopLocationUpdates");
            gpsData.SetTrackingState(false);
            gpsData.SetStatus("GPS Stopped");
        }
        catch (Exception e)
        {
            Debug.LogError("GPSManager: Error stopping GPS - " + e.Message);
        }
    }
    
    // Callback из Java плагина
    public void OnLocationUpdate(string locationData)
    {
        Debug.Log("GPSManager: Location update: " + locationData);

        try
        {
            string[] parts = locationData.Split('|');
            if (parts.Length >= 6)
            {
                double lat = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                double lon = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                float acc = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                double alt = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                float spd = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
                float brg = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);

                gpsData.UpdateLocation(lat, lon, alt, acc, spd, brg);
                currentAccuracy = acc;

                Debug.Log($"GPSManager: Update #{gpsData.updateCount} - {gpsData}");
            }
            else
            {
                Debug.LogError($"GPSManager: Invalid data - {parts.Length} parts, expected 6");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("GPSManager: Parse error - " + e.Message);
        }
    }
    
    // Переименован метод чтобы избежать конфликта с событием
    public void OnStatusUpdate(string statusData)
    {
        Debug.Log("GPSManager: Status - " + statusData);
        string[] parts = statusData.Split('|');
        if (parts.Length > 0)
        {
            gpsData.SetStatus("Status: " + parts[0]);
            OnStatusMessage?.Invoke(parts[0]);
        }
    }
    
    public void OnProviderEnabled(string provider)
    {
        Debug.Log("GPSManager: Provider enabled - " + provider);
        gpsData.SetStatus(provider + " enabled");
        OnStatusMessage?.Invoke(provider + " enabled");
    }
    
    public void OnProviderDisabled(string provider)
    {
        Debug.Log("GPSManager: Provider disabled - " + provider);
        gpsData.SetStatus(provider + " disabled");
        OnStatusMessage?.Invoke(provider + " disabled");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && gpsData.isTracking)
        {
            Debug.Log("GPSManager: App paused - stopping GPS");
            StopGPS();
        }
        else if (!pauseStatus && HasLocationPermissions())
        {
            Debug.Log("GPSManager: App resumed - restarting GPS");
            StartCoroutine(DelayedRestart());
        }
    }
    
    IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(2);
        StartGPS();
    }
    
    void OnDestroy()
    {
        if (gpsData != null)
        {
            gpsData.OnDataUpdated -= () => OnDataUpdated?.Invoke();
            gpsData.OnTrackingStateChanged -= () => 
            {
                if (gpsData.isTracking)
                    OnTrackingStarted?.Invoke();
                else
                    OnTrackingStopped?.Invoke();
            };
        }
        
        if (gpsData.isTracking) StopGPS();
    }
    
    // Публичные свойства для доступа к данным
    public Vector2 GetCoordinates() => gpsData.GetCoordinates();
    public float GetAccuracy() => currentAccuracy;
    public bool IsTracking => gpsData.isTracking;
    public bool HasValidData => gpsData.hasValidData;
    public int GetUpdateCount() => gpsData.updateCount;
    public GPSDataModel GetGPSData() => gpsData;
}