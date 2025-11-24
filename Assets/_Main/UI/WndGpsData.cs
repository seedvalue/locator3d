using TMPro;
using UnityEngine;
using UnityEngine.UI;
//53.953168, 27.677397
// угол магаза 125м

//



public class WndGpsData : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text statusText;
    public TMP_Text coordinatesText;
    public TMP_Text accuracyText;
    public TMP_Text altitudeText;
    public TMP_Text satellitesText;
    public Button startButton;
    public Button stopButton;
    public Button debugStatusButton;
    
    [Header("GPS Manager Reference")]
    public GPSManager gpsManager;
    
    private GPSDataModel gpsData;
    
    void Start()
    {
        if (gpsManager == null)
        {
            gpsManager = FindObjectOfType<GPSManager>();
        }
        
        if (gpsManager != null)
        {
            gpsData = gpsManager.GetGPSData();
            SetupUI();
            SubscribeToEvents();
        }
        else
        {
            Debug.LogError("WndGpsData: GPSManager not found!");
        }
    }
    
    void SetupUI()
    {
        startButton.onClick.AddListener(() => gpsManager.StartGPS());
        stopButton.onClick.AddListener(() => gpsManager.StopGPS());
        debugStatusButton.onClick.AddListener(DebugGPSStatus);
        
        UpdateUIState();
        UpdateStatus("Initializing GPS...");
    }
    
    void SubscribeToEvents()
    {
        if (gpsManager != null)
        {
            gpsManager.OnInitialized += OnGPSInitialized;
            gpsManager.OnStatusMessage += OnStatusChanged; // Исправлено имя события
            gpsManager.OnTrackingStarted += OnTrackingStarted;
            gpsManager.OnTrackingStopped += OnTrackingStopped;
            gpsManager.OnDataUpdated += OnDataUpdated;
        }
    }
    
    void UnsubscribeFromEvents()
    {
        if (gpsManager != null)
        {
            gpsManager.OnInitialized -= OnGPSInitialized;
            gpsManager.OnStatusMessage -= OnStatusChanged; // Исправлено имя события
            gpsManager.OnTrackingStarted -= OnTrackingStarted;
            gpsManager.OnTrackingStopped -= OnTrackingStopped;
            gpsManager.OnDataUpdated -= OnDataUpdated;
        }
    }
    
    void OnGPSInitialized()
    {
        UpdateUIState();
        UpdateStatus("GPS Initialized");
    }
    
    void OnStatusChanged(string status)
    {
        UpdateStatus(status);
    }
    
    void OnTrackingStarted()
    {
        UpdateUIState();
        UpdateStatus("Tracking started...");
    }
    
    void OnTrackingStopped()
    {
        UpdateUIState();
        UpdateStatus("Tracking stopped");
    }
    
    void OnDataUpdated()
    {
        UpdateDisplay();
    }
    
    void UpdateUIState()
    {
        if (gpsManager == null) return;
        
        bool hasPermissions = gpsManager.HasLocationPermissions();
        bool isTracking = gpsManager.IsTracking;
        
        startButton.interactable = !isTracking && hasPermissions;
        stopButton.interactable = isTracking;
        
        if (gpsData != null && gpsData.hasValidData)
        {
            coordinatesText.color = Color.green;
        }
        else
        {
            coordinatesText.color = Color.yellow;
        }
        
        if (!hasPermissions)
        {
            startButton.interactable = false;
        }
    }
    
    public void DebugGPSStatus()
    {
        if (gpsManager == null || gpsData == null) return;
        
        Debug.Log($"=== GPS Status ===");
        Debug.Log($"Tracking: {gpsManager.IsTracking}");
        Debug.Log($"Permissions: {gpsManager.HasLocationPermissions()}");
        Debug.Log($"Updates: {gpsManager.GetUpdateCount()}");
        Debug.Log($"Coords: {gpsData.latitude:F6}, {gpsData.longitude:F6}");
        Debug.Log($"Accuracy: {gpsData.accuracy:F1}m");
        Debug.Log($"Has Valid Data: {gpsData.hasValidData}");
    }
    
    void UpdateDisplay()
    {
        if (gpsData == null) return;
        
        UpdateCoordinates($"Lat: {gpsData.latitude:F6}\nLon: {gpsData.longitude:F6}");
        UpdateAccuracy($"Accuracy: {gpsData.accuracy:F1} m");
        UpdateAltitude($"Altitude: {gpsData.altitude:F1} m\nSpeed: {gpsData.speed:F1} m/s");
        UpdateSatellites($"Updates: {gpsData.updateCount}\nBearing: {gpsData.bearing:F0}°");
        
        // Цвет точности
        if (accuracyText != null)
        {
            if (gpsData.accuracy <= 5f)
                accuracyText.color = Color.green;
            else if (gpsData.accuracy <= 10f)
                accuracyText.color = Color.yellow;
            else
                accuracyText.color = Color.red;
        }
            
        UpdateUIState();
    }
    
    void UpdateStatus(string status) 
    { 
        if (statusText != null) 
            statusText.text = status; 
    }
    
    void UpdateCoordinates(string coords) 
    { 
        if (coordinatesText != null) 
            coordinatesText.text = coords; 
    }
    
    void UpdateAccuracy(string acc) 
    { 
        if (accuracyText != null) 
            accuracyText.text = acc; 
    }
    
    void UpdateAltitude(string alt) 
    { 
        if (altitudeText != null) 
            altitudeText.text = alt; 
    }
    
    void UpdateSatellites(string sat) 
    { 
        if (satellitesText != null) 
            satellitesText.text = sat; 
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}