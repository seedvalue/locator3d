using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Android;

public class SensorsManager : MonoBehaviour
{
    public static SensorsManager Instance { get; private set; }

    public event Action<SensorData> OnSensorDataUpdated;

    [SerializeField] private float updateInterval = 0.1f;

    private AndroidJavaObject sensorPlugin;
    private SensorData currentSensorData;
    private float timer;
    private bool isInitialized;

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
        else Destroy(gameObject);
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.3f);

        InitializePlugin();
        yield return RequestPermissions();

        if (sensorPlugin != null)
        {
            sensorPlugin.Call("startSensors");
            isInitialized = sensorPlugin.Call<bool>("areSensorsStarted");
            Debug.Log($"Sensors initialized: {isInitialized}");
        }
    }

    // ------------------------- ИНИЦИАЛИЗАЦИЯ ANDROID ПЛАГИНА -------------------------
    void InitializePlugin()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass pluginClass = new AndroidJavaClass("com.yourcompany.sensorplugin.SensorPlugin");
            pluginClass.CallStatic("setUnityActivity", activity);

            sensorPlugin = new AndroidJavaObject("com.yourcompany.sensorplugin.SensorPlugin");

            Debug.Log("SensorsManager: Plugin initialized");
        }
        catch (Exception e)
        {
            Debug.LogError("SensorsManager Init Error: " + e.Message);
        }
    }

    // ------------------------- ЗАПРОС РАЗРЕШЕНИЙ -------------------------
    IEnumerator RequestPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1);
        }
    }

    // ------------------------- ОБНОВЛЕНИЕ ДАННЫХ -------------------------
    void Update()
    {
        if (!isInitialized) return;

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateSensorData();
        }
    }

    void UpdateSensorData()
    {
        try
        {
            // Движение
            float[] accel = sensorPlugin.Call<float[]>("getAccelerometerData");
            float[] gyro = sensorPlugin.Call<float[]>("getGyroscopeData");
            float[] mag = sensorPlugin.Call<float[]>("getMagnetometerData");

            currentSensorData.accelerometer = new Vector3(accel[0], accel[1], accel[2]);
            currentSensorData.gyroscope = new Vector3(gyro[0], gyro[1], gyro[2]);
            currentSensorData.magnetometer = new Vector3(mag[0], mag[1], mag[2]);

            // Позиционирование
            float[] grav = sensorPlugin.Call<float[]>("getGravityData");
            float[] lin = sensorPlugin.Call<float[]>("getLinearAccelerationData");
            float[] rot = sensorPlugin.Call<float[]>("getRotationVectorData");
            float[] gRot = sensorPlugin.Call<float[]>("getGameRotationVectorData");
            float[] geo = sensorPlugin.Call<float[]>("getGeomagneticRotationVectorData");

            currentSensorData.gravity = new Vector3(grav[0], grav[1], grav[2]);
            currentSensorData.linearAcceleration = new Vector3(lin[0], lin[1], lin[2]);
            currentSensorData.rotationVector = new Vector3(rot[0], rot[1], rot[2]);
            currentSensorData.gameRotationVector = new Vector3(gRot[0], gRot[1], gRot[2]);
            currentSensorData.geomagneticRotationVector = new Vector3(geo[0], geo[1], geo[2]);

            OnSensorDataUpdated?.Invoke(currentSensorData);
        }
        catch (Exception e)
        {
            Debug.LogError("SensorsManager Read Error: " + e.Message);
        }
    }

    // ------------------------- СТАРТ / СТОП -------------------------
    public void StartSensors()
    {
        sensorPlugin?.Call("startSensors");
        isInitialized = true;
    }

    public void StopSensors()
    {
        sensorPlugin?.Call("stopSensors");
        isInitialized = false;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) StopSensors();
        else StartSensors();
    }

    void OnDestroy() => StopSensors();
}
