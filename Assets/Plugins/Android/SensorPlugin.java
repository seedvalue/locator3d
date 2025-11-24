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

    // -------- ДАННЫЕ СЕНСОРОВ -----------
    private final float[] accelerometerData = new float[3];
    private final float[] gyroscopeData = new float[3];
    private final float[] magnetometerData = new float[3];

    private final float[] gravityData = new float[3];
    private final float[] linearAccelerationData = new float[3];
    private final float[] rotationVectorData = new float[5];
    private final float[] gameRotationVectorData = new float[4];
    private final float[] geomagneticRotationVectorData = new float[3];

    private boolean sensorsStarted = false;

    // -------- УСТАНОВКА UNITY ACTIVITY --------
    public static void setUnityActivity(Activity activity) {
        unityActivity = activity;
    }

    // -------- СТАРТ --------
    public void startSensors() {
        if (unityActivity == null) {
            Log.e("SensorPlugin", "Unity Activity = null!");
            return;
        }

        sensorManager = (SensorManager) unityActivity.getSystemService(Activity.SENSOR_SERVICE);
        if (sensorManager == null) {
            Log.e("SensorPlugin", "SensorManager = null!");
            return;
        }

        // Движение
        register(Sensor.TYPE_ACCELEROMETER, "Accelerometer");
        register(Sensor.TYPE_GYROSCOPE, "Gyroscope");
        register(Sensor.TYPE_MAGNETIC_FIELD, "Magnetometer");

        // Позиционирование
        register(Sensor.TYPE_GRAVITY, "Gravity");
        register(Sensor.TYPE_LINEAR_ACCELERATION, "LinearAcceleration");
        register(Sensor.TYPE_ROTATION_VECTOR, "RotationVector");
        register(Sensor.TYPE_GAME_ROTATION_VECTOR, "GameRotationVector");
        register(Sensor.TYPE_GEOMAGNETIC_ROTATION_VECTOR, "GeomagneticRotationVector");

        sensorsStarted = true;
    }

    private void register(int type, String name) {
        try {
            Sensor sensor = sensorManager.getDefaultSensor(type);
            if (sensor != null) {
                sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_GAME);
                Log.d("SensorPlugin", name + " registered");
            } else {
                Log.d("SensorPlugin", name + " not available");
            }
        } catch (Exception e) {
            Log.e("SensorPlugin", "Register error " + name + ": " + e.getMessage());
        }
    }

    // -------- СТОП --------
    public void stopSensors() {
        if (sensorManager != null) {
            sensorManager.unregisterListener(this);
            sensorsStarted = false;
        }
    }

    @Override
    public void onSensorChanged(SensorEvent e) {
        switch (e.sensor.getType()) {
            case Sensor.TYPE_ACCELEROMETER:
                System.arraycopy(e.values, 0, accelerometerData, 0, 3);
                break;
            case Sensor.TYPE_GYROSCOPE:
                System.arraycopy(e.values, 0, gyroscopeData, 0, 3);
                break;
            case Sensor.TYPE_MAGNETIC_FIELD:
                System.arraycopy(e.values, 0, magnetometerData, 0, 3);
                break;

            case Sensor.TYPE_GRAVITY:
                System.arraycopy(e.values, 0, gravityData, 0, 3);
                break;
            case Sensor.TYPE_LINEAR_ACCELERATION:
                System.arraycopy(e.values, 0, linearAccelerationData, 0, 3);
                break;
            case Sensor.TYPE_ROTATION_VECTOR:
                System.arraycopy(e.values, 0, rotationVectorData, 0, e.values.length);
                break;
            case Sensor.TYPE_GAME_ROTATION_VECTOR:
                System.arraycopy(e.values, 0, gameRotationVectorData, 0, e.values.length);
                break;
            case Sensor.TYPE_GEOMAGNETIC_ROTATION_VECTOR:
                System.arraycopy(e.values, 0, geomagneticRotationVectorData, 0, 3);
                break;
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {}

    // -------- GETTERS --------
    public float[] getAccelerometerData() { return accelerometerData.clone(); }
    public float[] getGyroscopeData() { return gyroscopeData.clone(); }
    public float[] getMagnetometerData() { return magnetometerData.clone(); }

    public float[] getGravityData() { return gravityData.clone(); }
    public float[] getLinearAccelerationData() { return linearAccelerationData.clone(); }
    public float[] getRotationVectorData() { return rotationVectorData.clone(); }
    public float[] getGameRotationVectorData() { return gameRotationVectorData.clone(); }
    public float[] getGeomagneticRotationVectorData() { return geomagneticRotationVectorData.clone(); }

    public boolean areSensorsStarted() { return sensorsStarted; }
}
