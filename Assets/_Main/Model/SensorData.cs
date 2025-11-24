using UnityEngine;

[System.Serializable]
public class SensorData
{
    // Motion Sensors
    public Vector3 accelerometer;
    public Vector3 gyroscope;
    public Vector3 magnetometer;
    
    // Positioning Sensors
    public Vector3 gravity;
    public Vector3 linearAcceleration;
    public Vector3 rotationVector;
    public Vector3 gameRotationVector;
    public Vector3 geomagneticRotationVector;
    public Vector3 orientation;
    
    // Environment Sensors
    public float light;
    public float proximity;
    public float pressure;
    public float humidity;
    public float ambientTemperature;
    
    public SensorData()
    {
        accelerometer = Vector3.zero;
        gyroscope = Vector3.zero;
        magnetometer = Vector3.zero;
        gravity = Vector3.zero;
        linearAcceleration = Vector3.zero;
        rotationVector = Vector3.zero;
        gameRotationVector = Vector3.zero;
        geomagneticRotationVector = Vector3.zero;
        orientation = Vector3.zero;
        
        light = 0f;
        proximity = 0f;
        pressure = 0f;
        humidity = 0f;
        ambientTemperature = 0f;
    }
    
    public override string ToString()
    {
        return $"=== MOTION SENSORS ===\n" +
               $"ACCEL: {accelerometer.x:F4}, {accelerometer.y:F4}, {accelerometer.z:F4}\n" +
               $"GYRO:  {gyroscope.x:F4}, {gyroscope.y:F4}, {gyroscope.z:F4}\n" +
               $"MAG:   {magnetometer.x:F3}, {magnetometer.y:F3}, {magnetometer.z:F3}\n\n" +
               
               "=== POSITIONING ===\n" +
               $"GRAV:  {gravity.x:F4}, {gravity.y:F4}, {gravity.z:F4}\n" +
               $"LIN_ACC: {linearAcceleration.x:F4}, {linearAcceleration.y:F4}, {linearAcceleration.z:F4}\n" +
               $"ROT_VEC: {rotationVector.x:F3}, {rotationVector.y:F3}, {rotationVector.z:F3}\n" +
               $"GAME_ROT: {gameRotationVector.x:F3}, {gameRotationVector.y:F3}, {gameRotationVector.z:F3}\n" +
               $"GEO_ROT: {geomagneticRotationVector.x:F3}, {geomagneticRotationVector.y:F3}, {geomagneticRotationVector.z:F3}\n" +
               $"ORIENT: {orientation.x:F1}°, {orientation.y:F1}°, {orientation.z:F1}°\n\n" +
               
               "=== ENVIRONMENT ===\n" +
               $"LIGHT: {light:F1} lx\n" +
               $"PROXIMITY: {proximity:F2} cm\n" +
               $"PRESSURE: {pressure:F1} hPa\n" +
               $"HUMIDITY: {humidity:F1}%\n" +
               $"TEMP: {ambientTemperature:F1}°C";
    }
}