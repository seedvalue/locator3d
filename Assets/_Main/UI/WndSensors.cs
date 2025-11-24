using UnityEngine;
using TMPro;

public class WndSensors : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text sensorDataText;
    
    [Header("Update Settings")]
    [SerializeField] private float uiUpdateInterval = 0.1f;
    
    private float updateTimer = 0f;
    
    void Start()
    {
        if (sensorDataText == null)
        {
            Debug.LogError("WndSensors: sensorDataText is not assigned!");
            return;
        }
        
        sensorDataText.text = "Initializing sensors...";
        
        // Подписываемся на обновления данных сенсоров
        if (SensorsManager.Instance != null)
        {
            SensorsManager.Instance.OnSensorDataUpdated += OnSensorDataUpdated;
            
            if (!SensorsManager.Instance.IsInitialized)
            {
                sensorDataText.text = "Sensors manager not initialized yet...";
            }
        }
        else
        {
            sensorDataText.text = "SensorsManager instance not found!";
        }
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий при уничтожении объекта
        if (SensorsManager.Instance != null)
        {
            SensorsManager.Instance.OnSensorDataUpdated -= OnSensorDataUpdated;
        }
    }
    
    void Update()
    {
        // Для случаев, когда менеджер еще не инициализирован
        if (SensorsManager.Instance == null || !SensorsManager.Instance.IsInitialized)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= 1f) // Проверяем статус каждую секунду
            {
                updateTimer = 0f;
                if (SensorsManager.Instance == null)
                {
                    sensorDataText.text = "Waiting for SensorsManager...";
                }
                else if (!SensorsManager.Instance.IsInitialized)
                {
                    sensorDataText.text = "Sensors initializing...";
                }
            }
            return;
        }
    }
    
    private void OnSensorDataUpdated(SensorData data)
    {
        updateTimer += Time.deltaTime;
        
        // Обновляем UI с интервалом для оптимизации
        if (updateTimer >= uiUpdateInterval)
        {
            updateTimer = 0f;
            sensorDataText.text = data.ToString();
        }
    }
    
    public void SetUpdateInterval(float interval)
    {
        uiUpdateInterval = Mathf.Max(0.01f, interval);
    }
}