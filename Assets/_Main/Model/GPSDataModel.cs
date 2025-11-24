using System;
using UnityEngine;

[Serializable]
public class GPSDataModel
{
    [Header("GPS Coordinates")]
    public double latitude = 0;
    public double longitude = 0;
    public double altitude = 0;
    
    [Header("GPS Metrics")]
    public float accuracy = 999f;
    public float speed = 0;
    public float bearing = 0;
    
    [Header("Status Flags")]
    public bool hasValidData = false;
    public bool isTracking = false;
    public int updateCount = 0;
    public string statusMessage = "Initializing...";
    
    // События для уведомления об изменениях
    public event Action OnDataUpdated;
    public event Action OnTrackingStateChanged;
    
    public void UpdateLocation(double lat, double lon, double alt, float acc, float spd, float brg)
    {
        latitude = lat;
        longitude = lon;
        altitude = alt;
        accuracy = acc;
        speed = spd;
        bearing = brg;
        updateCount++;
        hasValidData = true;
        
        OnDataUpdated?.Invoke();
    }
    
    public void SetTrackingState(bool tracking)
    {
        if (isTracking != tracking)
        {
            isTracking = tracking;
            OnTrackingStateChanged?.Invoke();
        }
    }
    
    public void SetStatus(string status)
    {
        statusMessage = status;
        OnDataUpdated?.Invoke();
    }
    
    public void Reset()
    {
        hasValidData = false;
        updateCount = 0;
        accuracy = 999f;
        statusMessage = "Reset";
        OnDataUpdated?.Invoke();
    }
    
    public Vector2 GetCoordinates() => new Vector2((float)latitude, (float)longitude);
    
    public override string ToString()
    {
        return $"Lat: {latitude:F6}, Lon: {longitude:F6}, Acc: {accuracy:F1}m, HasData: {hasValidData}";
    }
}