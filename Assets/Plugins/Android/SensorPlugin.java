package com.yourcompany.sensorplugin;

import android.app.Activity;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.util.Log;

public class SensorPlugin implements SensorEventListener {
    private static Activity unityActivity;
    private SensorManager sensorManager;
    
    // Основные данные сенсоров
    private float[] accelerometerData = new float[3];
    private float[] gyroscopeData = new float[3];
    private float[] magnetometerData = new float[3];
    
    // Новые сенсоры для позиционирования
    private float[] gravityData = new float[3];
    private float[] linearAccelerationData = new float[3];
    private float[] rotationVectorData = new float[5];
    private float[] gameRotationVectorData = new float[4];
    private float[] geomagneticRotationVectorData = new float[3];
    private float[] orientationData = new float[3];
    private float[] lightData = new float[1];
    private float[] proximityData = new float[1];
    private float[] pressureData = new float[1];
    private float[] humidityData = new float[1];
    private float[] ambientTemperatureData = new float[1];
    
    private boolean sensorsStarted = false;
    
    /**
     * Установка активности Unity для доступа к системным сервисам
     */
    public static void setUnityActivity(Activity activity) {
        unityActivity = activity;
    }
    
    /**
     * Запуск всех доступных сенсоров
     */
    public void startSensors() {
        if (unityActivity == null) {
            Log.e("SensorPlugin", "Unity Activity is NULL!");
            return;
        }
        
        sensorManager = (SensorManager)unityActivity.getSystemService(Activity.SENSOR_SERVICE);
        if (sensorManager == null) {
            Log.e("SensorPlugin", "SensorManager is NULL!");
            return;
        }
        
        // Основные сенсоры движения
        registerSensor(Sensor.TYPE_ACCELEROMETER, "Accelerometer");
        registerSensor(Sensor.TYPE_GYROSCOPE, "Gyroscope");
        registerSensor(Sensor.TYPE_MAGNETIC_FIELD, "Magnetometer");
        
        // Сенсоры для позиционирования в пространстве
        registerSensor(Sensor.TYPE_GRAVITY, "Gravity");
        registerSensor(Sensor.TYPE_LINEAR_ACCELERATION, "Linear Acceleration");
        registerSensor(Sensor.TYPE_ROTATION_VECTOR, "Rotation Vector");
        registerSensor(Sensor.TYPE_GAME_ROTATION_VECTOR, "Game Rotation Vector");
        registerSensor(Sensor.TYPE_GEOMAGNETIC_ROTATION_VECTOR, "Geomagnetic Rotation Vector");
        registerSensor(Sensor.TYPE_ORIENTATION, "Orientation");
        
        // Окружающая среда
        registerSensor(Sensor.TYPE_LIGHT, "Light");
        registerSensor(Sensor.TYPE_PROXIMITY, "Proximity");
        registerSensor(Sensor.TYPE_PRESSURE, "Pressure");
        registerSensor(Sensor.TYPE_RELATIVE_HUMIDITY, "Humidity");
        registerSensor(Sensor.TYPE_AMBIENT_TEMPERATURE, "Ambient Temperature");
        
        sensorsStarted = true;
    }
    
    /**
     * Вспомогательный метод для регистрации сенсоров
     */
    private void registerSensor(int sensorType, String sensorName) {
        try {
            Sensor sensor = sensorManager.getDefaultSensor(sensorType);
            if (sensor != null) {
                sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_GAME);
                Log.d("SensorPlugin", sensorName + " registered");
            } else {
                Log.e("SensorPlugin", sensorName + " not available");
            }
        } catch (Exception e) {
            Log.e("SensorPlugin", "Error registering " + sensorName + ": " + e.getMessage());
        }
    }
    
    /**
     * Остановка всех сенсоров
     */
    public void stopSensors() {
        if (sensorManager != null) {
            sensorManager.unregisterListener(this);
            sensorsStarted = false;
            Log.d("SensorPlugin", "All sensors stopped");
        }
    }
    
    /**
     * Обработка изменений данных сенсоров
     */
    @Override
    public void onSensorChanged(SensorEvent event) {
        switch (event.sensor.getType()) {
            case Sensor.TYPE_ACCELEROMETER:
                System.arraycopy(event.values, 0, accelerometerData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "Accel: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
            case Sensor.TYPE_GYROSCOPE:
                System.arraycopy(event.values, 0, gyroscopeData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "Gyro: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
            case Sensor.TYPE_MAGNETIC_FIELD:
                System.arraycopy(event.values, 0, magnetometerData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "Mag: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
                
            // Новые сенсоры позиционирования
            case Sensor.TYPE_GRAVITY:
                System.arraycopy(event.values, 0, gravityData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "Gravity: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
            case Sensor.TYPE_LINEAR_ACCELERATION:
                System.arraycopy(event.values, 0, linearAccelerationData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "LinearAccel: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
            case Sensor.TYPE_ROTATION_VECTOR:
                System.arraycopy(event.values, 0, rotationVectorData, 0, Math.min(5, event.values.length));
                Log.d("SensorPlugin", "RotationVec: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2] + ", " + event.values[3] + (event.values.length > 4 ? ", " + event.values[4] : ""));
                break;
            case Sensor.TYPE_GAME_ROTATION_VECTOR:
                System.arraycopy(event.values, 0, gameRotationVectorData, 0, Math.min(4, event.values.length));
                Log.d("SensorPlugin", "GameRotationVec: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2] + ", " + event.values[3]);
                break;
            case Sensor.TYPE_GEOMAGNETIC_ROTATION_VECTOR:
                System.arraycopy(event.values, 0, geomagneticRotationVectorData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "GeoRotationVec: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
            case Sensor.TYPE_ORIENTATION:
                System.arraycopy(event.values, 0, orientationData, 0, Math.min(3, event.values.length));
                Log.d("SensorPlugin", "Orientation: " + event.values[0] + ", " + event.values[1] + ", " + event.values[2]);
                break;
                
            // Сенсоры окружающей среды
            case Sensor.TYPE_LIGHT:
                System.arraycopy(event.values, 0, lightData, 0, 1);
                Log.d("SensorPlugin", "Light: " + event.values[0]);
                break;
            case Sensor.TYPE_PROXIMITY:
                System.arraycopy(event.values, 0, proximityData, 0, 1);
                Log.d("SensorPlugin", "Proximity: " + event.values[0]);
                break;
            case Sensor.TYPE_PRESSURE:
                System.arraycopy(event.values, 0, pressureData, 0, 1);
                Log.d("SensorPlugin", "Pressure: " + event.values[0]);
                break;
            case Sensor.TYPE_RELATIVE_HUMIDITY:
                System.arraycopy(event.values, 0, humidityData, 0, 1);
                Log.d("SensorPlugin", "Humidity: " + event.values[0]);
                break;
            case Sensor.TYPE_AMBIENT_TEMPERATURE:
                System.arraycopy(event.values, 0, ambientTemperatureData, 0, 1);
                Log.d("SensorPlugin", "AmbientTemp: " + event.values[0]);
                break;
        }
    }
    
    /**
     * Обработка изменения точности сенсора
     */
    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
        Log.d("SensorPlugin", "Accuracy changed: " + sensor.getName() + " = " + accuracy);
    }
    
    // Методы для получения данных основных сенсоров
    public float[] getAccelerometerData() {
        return accelerometerData.clone();
    }
    
    public float[] getGyroscopeData() {
        return gyroscopeData.clone();
    }
    
    public float[] getMagnetometerData() {
        return magnetometerData.clone();
    }
    
    // Методы для получения данных новых сенсоров позиционирования
    public float[] getGravityData() {
        return gravityData.clone();
    }
    
    public float[] getLinearAccelerationData() {
        return linearAccelerationData.clone();
    }
    
    public float[] getRotationVectorData() {
        return rotationVectorData.clone();
    }
    
    public float[] getGameRotationVectorData() {
        return gameRotationVectorData.clone();
    }
    
    public float[] getGeomagneticRotationVectorData() {
        return geomagneticRotationVectorData.clone();
    }
    
    public float[] getOrientationData() {
        return orientationData.clone();
    }
    
    // Методы для получения данных сенсоров окружающей среды
    public float[] getLightData() {
        return lightData.clone();
    }
    
    public float[] getProximityData() {
        return proximityData.clone();
    }
    
    public float[] getPressureData() {
        return pressureData.clone();
    }
    
    public float[] getHumidityData() {
        return humidityData.clone();
    }
    
    public float[] getAmbientTemperatureData() {
        return ambientTemperatureData.clone();
    }
    
    /**
     * Проверка активности сенсоров
     */
    public boolean areSensorsStarted() {
        return sensorsStarted;
    }
}