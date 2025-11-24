using UnityEngine;

[System.Serializable]
public class SensorData
{
    // ----------------------- ДВИЖЕНИЕ -----------------------
    public Vector3 accelerometer;     // Акселерометр
    public Vector3 gyroscope;         // Гироскоп
    public Vector3 magnetometer;      // Магнитометр

    // ------------------- ПОЗИЦИОНИРОВАНИЕ -------------------
    public Vector3 gravity;                 // Гравитационный вектор
    public Vector3 linearAcceleration;      // Линейное ускорение
    public Vector3 rotationVector;          // Rotation Vector
    public Vector3 gameRotationVector;      // Game Rotation
    public Vector3 geomagneticRotationVector; // Геомагнитный вектор

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
    }

    public override string ToString()
    {
        return $"=== MOTION ===\n" +
               $"ACCEL: {accelerometer}\n" +
               $"GYRO:  {gyroscope}\n" +
               $"MAG:   {magnetometer}\n\n" +
               $"=== POSITION ===\n" +
               $"GRAV:  {gravity}\n" +
               $"LIN ACC: {linearAcceleration}\n" +
               $"ROT_VEC: {rotationVector}\n" +
               $"GAME_ROT: {gameRotationVector}\n" +
               $"GEO_ROT: {geomagneticRotationVector}\n";
    }
}