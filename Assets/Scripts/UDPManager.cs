using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;
using TMPro;

public class UDPManager : MonoBehaviour
{
    IPEndPoint remoteEndPoint; // Punto final remoto para enviar datos UDP
    UDPDATA mUDPDATA = new UDPDATA(); // Objeto de datos personalizado para manejar la información enviada por UDP

    private string IP; // Dirección IP del servidor
    public int port; // Puerto del servidor
    public TextMeshPro engineA; // Texto que muestra el valor actual del motor A
    public Text engineAHex; // Texto que muestra el valor hexadecimal del motor A
    public Slider sliderA; // Slider para controlar el motor A
    public TextMeshPro engineB; // Texto que muestra el valor actual del motor B
    public Text engineBHex; // Texto que muestra el valor hexadecimal del motor B
    public Slider sliderB; // Slider para controlar el motor B
    public TextMeshPro engineC; // Texto que muestra el valor actual del motor C
    public Text engineCHex; // Texto que muestra el valor hexadecimal del motor C
    public Slider sliderC; // Slider para controlar el motor C
    public Text Data; // Texto que muestra todos los datos enviados

    UdpClient client; // Cliente UDP para enviar datos
    public bool active = false; // Indica si se están enviando datos activamente
    public float SmoothEngine = 0.5f; // Velocidad de suavizado para los motores
    public float A = 0, B = 0, C = 0, longg; // Valores de motores y longitud para cálculos

    public Transform vehicle; // Objeto del vehículo cuyas rotaciones afectan a los motores

    public void Start()
    {
        init(); // Inicializa las configuraciones y la conexión UDP
    }

    public void init()
    {
        // Configuración inicial de IP y puerto
        IP = "192.168.15.201";
        port = 7408;

        // Configuración del cliente UDP
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
        client = new UdpClient(53342);

        // Inicialización de datos predeterminados para los motores
        mUDPDATA.mAppControlField.ConfirmCode = "55aa";
        mUDPDATA.mAppControlField.PassCode = "0000";
        mUDPDATA.mAppControlField.FunctionCode = "1301";
        mUDPDATA.mAppWhoField.AcceptCode = "ffffffff";
        mUDPDATA.mAppDataField.RelaTime = "00000064";
        mUDPDATA.mAppDataField.PlayMotorA = "00000000";
        mUDPDATA.mAppDataField.PlayMotorB = "00000000";
        mUDPDATA.mAppDataField.PlayMotorC = "00000000";

        // Valores iniciales de los motores
        A = B = C = 100;

        // Configuración inicial de sliders y texto
        sliderA.value = A;
        sliderB.value = B;
        sliderC.value = C;

        string HexA = DecToHexMove(A);
        string HexB = DecToHexMove(B);
        string HexC = DecToHexMove(C);

        engineAHex.text = "Engine A: " + HexA;
        engineBHex.text = "Engine B: " + HexB;
        engineCHex.text = "Engine C: " + HexC;

        // Asignación de valores iniciales a los datos UDP
        mUDPDATA.mAppDataField.PlayMotorA = HexA;
        mUDPDATA.mAppDataField.PlayMotorB = HexB;
        mUDPDATA.mAppDataField.PlayMotorC = HexC;

        engineA.text = ((int)sliderA.value).ToString();
        engineB.text = ((int)sliderB.value).ToString();
        engineC.text = ((int)sliderC.value).ToString();

        Data.text = "Data: " + mUDPDATA.GetToString();

        sendString(mUDPDATA.GetToString()); // Envía los datos iniciales por UDP

        StartCoroutine(UpMovePlatform(3)); // Inicia la rutina para gestionar el envío de datos
    }

    public void ActiveSend()
    {
        active = true; // Activa el envío de datos
    }

    public void ResertPositionEngine()
    {
        // Resetea los valores de los motores a 0
        mUDPDATA.mAppDataField.PlayMotorA = "00000000";
        mUDPDATA.mAppDataField.PlayMotorB = "00000000";
        mUDPDATA.mAppDataField.PlayMotorC = "00000000";

        sendString(mUDPDATA.GetToString()); // Envía los datos reseteados
    }

    public void SetPositionEngine()
    {
        // Actualiza las posiciones de los motores según los valores actuales
        string HexA = DecToHexMove(A);
        string HexB = DecToHexMove(B);
        string HexC = DecToHexMove(C);

        mUDPDATA.mAppDataField.PlayMotorA = HexA;
        mUDPDATA.mAppDataField.PlayMotorB = HexB;
        mUDPDATA.mAppDataField.PlayMotorC = HexC;

        sendString(mUDPDATA.GetToString()); // Envía los datos actualizados
    }

    IEnumerator UpMovePlatform(float wait)
    {
        active = false; // Pausa el envío de datos temporalmente
        yield return new WaitForSeconds(3f); // Espera 3 segundos
        active = true; // Reactiva el envío de datos
    }

    void CalcularRotacion()
    {
        // Ajusta los valores de los motores según las rotaciones del vehículo
        if (vehicle.eulerAngles.z > 0.1 && vehicle.eulerAngles.z < 180)
        {
            B = Mathf.Lerp(B, 200, Time.deltaTime * SmoothEngine);
            C = Mathf.Lerp(C, 0, Time.deltaTime * SmoothEngine);
        }
        else if (vehicle.eulerAngles.z >= 180 && vehicle.eulerAngles.z <= 360)
        {
            B = Mathf.Lerp(B, 0, Time.deltaTime * SmoothEngine);
            C = Mathf.Lerp(C, 200, Time.deltaTime * SmoothEngine);
        }
        else
        {
            B = C = Mathf.Lerp(B, 100, Time.deltaTime * SmoothEngine);
        }
    }

    void FixedUpdate()
    {
        CalcularRotacion(); // Actualiza los valores de los motores en cada frame fijo
    }

    void OnApplicationQuit()
    {
        ResertPositionEngine(); // Resetea los valores al salir de la aplicación
        if (client != null) client.Close();
    }

    string DecToHexMove(float num)
    {
        // Convierte un número decimal en un string hexadecimal
        int d = (int)((num / 5f) * 10000f);
        return "000" + d.ToString("X");
    }

    private void sendString(string message)
    {
        // Envía un mensaje por UDP
        try
        {
            if (!string.IsNullOrEmpty(message))
            {
                print(message);
            }
        }
        catch (Exception err)
        {
            print(err.ToString()); // Muestra errores si ocurren
        }
    }

    private void OnDrawGizmos()
    {
        // Dibuja visualizaciones en la escena para depuración
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(vehicle.position, vehicle.position + vehicle.forward * longg);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(vehicle.position, vehicle.position + vehicle.right * longg);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(vehicle.position, vehicle.position + vehicle.up * longg);
    }
}
