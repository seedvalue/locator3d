using UnityEngine;
using System.Collections;
using UnityEngine.Android;
using System;

public class SensorsManager : MonoBehaviour
{
    public static SensorsManager Instance { get; private set; }
    
    public event Action<SensorData> OnSensorDataUpdated;
    
    [SerializeField] private float updateInterval = 0.1f;
    
    private AndroidJavaObject sensorPlugin;
    private SensorData currentSensorData;
    private float updateTimer = 0f;
    private bool isInitialized = false;
    
    public SensorData CurrentSensorData => currentSensorData;
    public bool IsInitialized => isInitialized;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentSensorData = new SensorData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        
        InitializePlugin();
        yield return StartCoroutine(RequestPermissions());
        
        if (sensorPlugin != null)
        {
            sensorPlugin.Call("startSensors");
            bool started = sensorPlugin.Call<bool>("areSensorsStarted");
            isInitialized = started;
            
            Debug.Log($"SensorsManager initialized: {started}");
        }
        else
        {
            Debug.LogError("SensorsManager: Plugin initialization failed");
        }
    }
    
    void InitializePlugin()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            AndroidJavaClass pluginClass = new AndroidJavaClass("com.yourcompany.sensorplugin.SensorPlugin");
            pluginClass.CallStatic("setUnityActivity", unityActivity);
            
            sensorPlugin = new AndroidJavaObject("com.yourcompany.sensorplugin.SensorPlugin");
            Debug.Log("SensorsManager: Android Sensor Plugin initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("SensorsManager: Plugin init failed: " + e.Message);
        }
    }
    
    IEnumerator RequestPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1);
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateSensorData();
        }
    }
    
    void UpdateSensorData()
    {
        try
        {
            // Motion Sensors
            float[] accel = sensorPlugin.Call<float[]>("getAccelerometerData");
            float[] gyro = sensorPlugin.Call<float[]>("getGyroscopeData");
            float[] mag = sensorPlugin.Call<float[]>("getMagnetometerData");
            
            currentSensorData.accelerometer = new Vector3(accel[0], accel[1], accel[2]);
            currentSensorData.gyroscope = new Vector3(gyro[0], gyro[1], gyro[2]);
            currentSensorData.magnetometer = new Vector3(mag[0], mag[1], mag[2]);
            
            // Positioning Sensors
            float[] gravity = sensorPlugin.Call<float[]>("getGravityData");
            float[] linearAccel = sensorPlugin.Call<float[]>("getLinearAccelerationData");
            float[] rotationVec = sensorPlugin.Call<float[]>("getRotationVectorData");
            float[] gameRotationVec = sensorPlugin.Call<float[]>("getGameRotationVectorData");
            float[] geoRotationVec = sensorPlugin.Call<float[]>("getGeomagneticRotationVectorData");
            float[] orientation = sensorPlugin.Call<float[]>("getOrientationData");
            
            currentSensorData.gravity = new Vector3(gravity[0], gravity[1], gravity[2]);
            currentSensorData.linearAcceleration = new Vector3(linearAccel[0], linearAccel[1], linearAccel[2]);
            currentSensorData.rotationVector = new Vector3(rotationVec[0], rotationVec[1], rotationVec[2]);
            currentSensorData.gameRotationVector = new Vector3(gameRotationVec[0], gameRotationVec[1], gameRotationVec[2]);
            currentSensorData.geomagneticRotationVector = new Vector3(geoRotationVec[0], geoRotationVec[1], geoRotationVec[2]);
            currentSensorData.orientation = new Vector3(orientation[0], orientation[1], orientation[2]);
            
            // Environment Sensors
            float[] light = sensorPlugin.Call<float[]>("getLightData");
            float[] proximity = sensorPlugin.Call<float[]>("getProximityData");
            float[] pressure = sensorPlugin.Call<float[]>("getPressureData");
            float[] humidity = sensorPlugin.Call<float[]>("getHumidityData");
            float[] ambientTemp = sensorPlugin.Call<float[]>("getAmbientTemperatureData");
            
            currentSensorData.light = light[0];
            currentSensorData.proximity = proximity[0];
            currentSensorData.pressure = pressure[0];
            currentSensorData.humidity = humidity[0];
            currentSensorData.ambientTemperature = ambientTemp[0];
            
            OnSensorDataUpdated?.Invoke(currentSensorData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("SensorsManager: Error reading sensors: " + e.Message);
        }
    }
    
    public void StartSensors()
    {
        if (sensorPlugin != null)
        {
            sensorPlugin.Call("startSensors");
            isInitialized = true;
        }
    }
    
    public void StopSensors()
    {
        if (sensorPlugin != null)
        {
            sensorPlugin.Call("stopSensors");
            isInitialized = false;
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (sensorPlugin != null)
        {
            if (pauseStatus)
            {
                StopSensors();
            }
            else
            {
                StartSensors();
            }
        }
    }
    
    void OnDestroy()
    {
        StopSensors();
    }
}