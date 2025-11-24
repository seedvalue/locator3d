using UnityEngine;
using TMPro;

public class GPSTargetIndicator : MonoBehaviour
{
    [Header("Target Settings")]
    public Vector2 targetCoordinates = new Vector2(53.953168f, 27.677397f);
    
    [Header("UI References")]
    public TMP_Text distanceText;
    public TMP_Text accuracyText;
    
    [Header("3D Arrow Reference")]
    public Transform arrow3D; // 3D стрелка, которая будет указывать направление
    
    [Header("GPS and Sensors References")]
    public GPSManager gpsManager;
    public SensorsManager sensorsManager;
    
    [Header("Visual Settings")]
    public Color highAccuracyColor = Color.green;
    public Color mediumAccuracyColor = Color.yellow;
    public Color lowAccuracyColor = Color.red;
    
    private GPSDataModel gpsData;
    private SensorData sensorData;
    private bool isInitialized = false;
    
    // Радиус Земли в метрах
    private const double EARTH_RADIUS = 6371000.0;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        if (gpsManager == null)
            gpsManager = FindObjectOfType<GPSManager>();
            
        if (sensorsManager == null)
            sensorsManager = FindObjectOfType<SensorsManager>();
        
        if (gpsManager != null)
        {
            gpsData = gpsManager.GetGPSData();
            gpsManager.OnDataUpdated += OnGPSDataUpdated;
        }
        
        if (sensorsManager != null)
        {
            sensorsManager.OnSensorDataUpdated += OnSensorDataUpdated;
        }
        
        isInitialized = (gpsManager != null && sensorsManager != null);
        
        if (!isInitialized)
        {
            Debug.LogWarning("GPSTargetIndicator: GPSManager or SensorsManager not found!");
        }
    }
    
    void OnGPSDataUpdated()
    {
        UpdateDirectionAndDistance();
    }
    
    void OnSensorDataUpdated(SensorData data)
    {
        sensorData = data;
        UpdateDirectionAndDistance();
    }
    
    void UpdateDirectionAndDistance()
    {
        if (!isInitialized || !gpsData.hasValidData || arrow3D == null)
            return;
        
        // Рассчитываем расстояние до цели
        double distance = CalculateDistance(
            gpsData.latitude, gpsData.longitude,
            targetCoordinates.x, targetCoordinates.y
        );
        
        // Рассчитываем направление (азимут) к цели
        double bearing = CalculateBearing(
            gpsData.latitude, gpsData.longitude,
            targetCoordinates.x, targetCoordinates.y
        );
        
        // Обновляем направление стрелки с учетом ориентации устройства
        UpdateArrowDirection(bearing);
        
        // Обновляем UI
        UpdateUI(distance, gpsData.accuracy);
    }
    
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        
        double a = Mathf.Sin((float)dLat / 2) * Mathf.Sin((float)dLat / 2) +
                  Mathf.Cos((float)ToRadians(lat1)) * Mathf.Cos((float)ToRadians(lat2)) *
                  Mathf.Sin((float)dLon / 2) * Mathf.Sin((float)dLon / 2);
        
        double c = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
        
        return EARTH_RADIUS * c;
    }
    
    double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = ToRadians(lon2 - lon1);
        
        double y = Mathf.Sin((float)dLon) * Mathf.Cos((float)ToRadians(lat2));
        double x = Mathf.Cos((float)ToRadians(lat1)) * Mathf.Sin((float)ToRadians(lat2)) -
                  Mathf.Sin((float)ToRadians(lat1)) * Mathf.Cos((float)ToRadians(lat2)) * 
                  Mathf.Cos((float)dLon);
        
        double bearing = Mathf.Atan2((float)y, (float)x);
        bearing = ToDegrees(bearing);
        bearing = (bearing + 360) % 360;
        
        return bearing;
    }
    
    void UpdateArrowDirection(double targetBearing)
    {
        if (sensorData == null) return;
        
        // Получаем текущую ориентацию устройства из данных сенсоров
        float deviceHeading = sensorData.orientation.y; // Обычно азимут хранится в Y
        
        // Рассчитываем относительное направление к цели
        float relativeBearing = (float)targetBearing - deviceHeading;
        
        // Нормализуем угол
        if (relativeBearing < 0) relativeBearing += 360;
        if (relativeBearing > 360) relativeBearing -= 360;
        
        // Поворачиваем стрелку
        arrow3D.rotation = Quaternion.Euler(0, relativeBearing, 0);
    }
    
    void UpdateUI(double distance, float accuracy)
    {
        // Обновляем текст расстояния
        if (distanceText != null)
        {
            if (distance < 1000)
                distanceText.text = $"Расстояние: {distance:F1} м";
            else
                distanceText.text = $"Расстояние: {distance/1000:F2} км";
        }
        
        // Обновляем текст точности и цвет
        if (accuracyText != null)
        {
            accuracyText.text = $"Точность: {accuracy:F1} м";
            
            // Меняем цвет в зависимости от точности
            if (accuracy <= 5f)
                accuracyText.color = highAccuracyColor;
            else if (accuracy <= 15f)
                accuracyText.color = mediumAccuracyColor;
            else
                accuracyText.color = lowAccuracyColor;
        }
    }
    
    double ToRadians(double degrees)
    {
        return degrees * Mathf.PI / 180.0;
    }
    
    double ToDegrees(double radians)
    {
        return radians * 180.0 / Mathf.PI;
    }
    
    void OnDestroy()
    {
        if (gpsManager != null)
            gpsManager.OnDataUpdated -= OnGPSDataUpdated;
            
        if (sensorsManager != null)
            sensorsManager.OnSensorDataUpdated -= OnSensorDataUpdated;
    }
    
    // Публичные методы для изменения целевых координат
    public void SetTargetCoordinates(double latitude, double longitude)
    {
        targetCoordinates = new Vector2((float)latitude, (float)longitude);
        UpdateDirectionAndDistance();
    }
    
    public void SetTargetCoordinates(Vector2 coordinates)
    {
        targetCoordinates = coordinates;
        UpdateDirectionAndDistance();
    }
    
    // Публичные свойства для доступа к данным
    public double GetDistanceToTarget()
    {
        if (!isInitialized || !gpsData.hasValidData)
            return 0;
            
        return CalculateDistance(
            gpsData.latitude, gpsData.longitude,
            targetCoordinates.x, targetCoordinates.y
        );
    }
    
    public float GetCurrentAccuracy()
    {
        return isInitialized ? gpsData.accuracy : 999f;
    }
    
    public bool IsTracking()
    {
        return isInitialized && gpsData.isTracking;
    }
}