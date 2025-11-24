package com.yourcompany.gpsplugin;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;
import android.os.Looper;
import android.util.Log;

import androidx.core.app.ActivityCompat;

import com.unity3d.player.UnityPlayer;

import java.util.Locale; // ДОБАВЬ ЭТОТ ИМПОРТ

public class HighAccuracyGPSPlugin {
    private static final String TAG = "HighAccuracyGPS";
    private static LocationManager locationManager;
    private static LocationListener locationListener;
    private static Activity unityActivity;
    
    public static void Initialize() {
        Log.d(TAG, "Initializing HighAccuracyGPS Plugin");
        unityActivity = UnityPlayer.currentActivity;
    }
    
    public static boolean CheckPermissions() {
        Log.d(TAG, "Checking location permissions");
        return ActivityCompat.checkSelfPermission(unityActivity, 
                Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED &&
               ActivityCompat.checkSelfPermission(unityActivity, 
                Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED;
    }
    
    public static void RequestPermissions() {
        Log.d(TAG, "Requesting location permissions");
        String[] permissions = {
            Manifest.permission.ACCESS_FINE_LOCATION,
            Manifest.permission.ACCESS_COARSE_LOCATION
        };
        ActivityCompat.requestPermissions(unityActivity, permissions, 1);
    }
    
    public static void StartLocationUpdates() {
        Log.d(TAG, "Starting location updates");
        
        if (!CheckPermissions()) {
            Log.e(TAG, "Permissions not granted for location updates");
            UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", "no_permissions|0");
            return;
        }
        
        try {
            locationManager = (LocationManager) unityActivity.getSystemService(Activity.LOCATION_SERVICE);
            
            locationListener = new LocationListener() {
                @Override
                public void onLocationChanged(Location location) {
                    if (location == null) {
                        Log.e(TAG, "Location is null");
                        return;
                    }
                    
                    try {
                        String locationData = String.format(Locale.US,
                            "%.7f|%.7f|%.1f|%.1f|%.1f|%.1f|%d",
                            location.getLatitude(),
                            location.getLongitude(), 
                            location.getAccuracy(),
                            location.getAltitude(),
                            location.getSpeed(),
                            location.getBearing(),
                            location.getTime()
                        );
                        
                        Log.d(TAG, "Location updated: " + locationData);
                        UnityPlayer.UnitySendMessage("GPSManager", "OnLocationUpdate", locationData);
                        
                    } catch (Exception e) {
                        Log.e(TAG, "Error formatting location data: " + e.getMessage());
                    }
                }
                
                @Override
                public void onStatusChanged(String provider, int status, Bundle extras) {
                    Log.d(TAG, "GPS status changed: " + provider + " status: " + status);
                    UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", provider + "|" + status);
                }
                
                @Override
                public void onProviderEnabled(String provider) {
                    Log.d(TAG, "GPS provider enabled: " + provider);
                    UnityPlayer.UnitySendMessage("GPSManager", "OnProviderEnabled", provider);
                }
                
                @Override
                public void onProviderDisabled(String provider) {
                    Log.d(TAG, "GPS provider disabled: " + provider);
                    UnityPlayer.UnitySendMessage("GPSManager", "OnProviderDisabled", provider);
                }
            };
            
            // Запрос обновлений с максимальной точностью
            locationManager.requestLocationUpdates(
                LocationManager.GPS_PROVIDER,
                100,      // minTimeMs - 100ms
                0.1f,     // minDistanceM - 0.1 meter
                locationListener,
                Looper.getMainLooper()
            );
            
            Log.d(TAG, "Location updates started successfully");
            UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", "started|0");
            
        } catch (SecurityException e) {
            Log.e(TAG, "Security exception starting location updates: " + e.getMessage());
            UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", "security_error|0");
        } catch (Exception e) {
            Log.e(TAG, "Exception starting location updates: " + e.getMessage());
            UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", "error|0");
        }
    }
    
    public static void StopLocationUpdates() {
        Log.d(TAG, "Stopping location updates");
        if (locationManager != null && locationListener != null) {
            locationManager.removeUpdates(locationListener);
        }
        UnityPlayer.UnitySendMessage("GPSManager", "OnStatusChanged", "stopped|0");
    }
}