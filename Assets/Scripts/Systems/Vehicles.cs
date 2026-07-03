using UnityEngine;
using Unity.Netcode;
using System.Collections;

public enum VehicleType { Car, Truck, Motorbike }

public class Vehicle : NetworkBehaviour
{
    public VehicleType vType = VehicleType.Car;
    public NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> driverId = new NetworkVariable<ulong>(999999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float moveSpeed = 18f;
    private float turnSpeed = 120f;
    private Transform driverSeat;

    private void Start()
    {
        if (vType == VehicleType.Motorbike) moveSpeed = 22f;
        else if (vType == VehicleType.Truck) moveSpeed = 14f;

        Material mat = new Material(ProceduralArt.GetSafeShader("Standard")); mat.color = vType == VehicleType.Motorbike ? Color.red : (vType == VehicleType.Truck ? Color.yellow : Color.blue);

        GameObject realCarPrefab = Resources.Load<GameObject>("Models/Car");
        GameObject body = null;
        if (realCarPrefab != null && vType == VehicleType.Car)
        {
            body = Object.Instantiate(realCarPrefab, transform); body.name = "RealCarMesh"; body.transform.localPosition = new Vector3(0, 0.5f, 0); body.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            foreach (Renderer r in body.GetComponentsInChildren<Renderer>()) { r.material = mat; }
        }
        else
        {
            body = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj"); body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0, 1f, 0); body.transform.localScale = vType == VehicleType.Motorbike ? new Vector3(0.8f, 1.2f, 2.5f) : (vType == VehicleType.Truck ? new Vector3(2.5f, 2.5f, 5.5f) : new Vector3(2f, 1.5f, 4.2f));
            body.GetComponent<Renderer>().material = mat;

            Vector3[] wPos = vType == VehicleType.Motorbike ? new Vector3[] { new Vector3(0, 0.5f, 1.2f), new Vector3(0, 0.5f, -1.2f) } : new Vector3[] { new Vector3(1f, 0.5f, 1.8f), new Vector3(-1f, 0.5f, 1.8f), new Vector3(1f, 0.5f, -1.8f), new Vector3(-1f, 0.5f, -1.8f) };
            foreach (Vector3 p in wPos) { GameObject wheel = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj"); wheel.transform.SetParent(transform); wheel.transform.localPosition = p; wheel.transform.localRotation = Quaternion.Euler(0, 0, 90); wheel.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f); wheel.GetComponent<Renderer>().material.color = Color.black; Destroy(wheel.GetComponent<Collider>()); }
        }

        driverSeat = new GameObject("DriverSeat").transform; driverSeat.SetParent(transform); driverSeat.localPosition = new Vector3(0, 1.8f, 0);
        gameObject.AddComponent<BoxCollider>().size = new Vector3(2.5f, 2.5f, 5f);
    }

    private void Update()
    {
        if (driverId.Value != 999999 && NetworkManager.Singleton != null && driverId.Value == NetworkManager.Singleton.LocalClientId)
        {
            Vector2 m = TouchControls.Instance != null ? TouchControls.Instance.MoveInput : Vector2.zero;
            transform.Rotate(Vector3.up * m.x * turnSpeed * Time.deltaTime);
            transform.position += transform.forward * m.y * moveSpeed * Time.deltaTime;
            RequestSyncServerRpc(transform.position, transform.rotation);
        }
        else if (!IsServer) { transform.position = Vector3.Lerp(transform.position, netPosition.Value, Time.deltaTime * 15f); transform.rotation = Quaternion.Slerp(transform.rotation, netRotation.Value, Time.deltaTime * 15f); }
    }

    [ServerRpc(RequireOwnership = false)] private void RequestSyncServerRpc(Vector3 p, Quaternion r) { netPosition.Value = p; netRotation.Value = r; transform.position = p; transform.rotation = r; }
    [ServerRpc(RequireOwnership = false)] public void RequestEnterServerRpc(ulong clientId) { if (driverId.Value == 999999) driverId.Value = clientId; }
    [ServerRpc(RequireOwnership = false)] public void RequestExitServerRpc() { driverId.Value = 999999; }

    public Transform GetSeat() { return driverSeat; }
}

public class Zipline : MonoBehaviour
{
    public Vector3 endPos;
    private void Start()
    {
        GameObject line = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj"); line.transform.SetParent(transform);
        Vector3 mid = (transform.position + endPos) / 2f; line.transform.position = mid; line.transform.up = endPos - transform.position;
        line.transform.localScale = new Vector3(0.2f, Vector3.Distance(transform.position, endPos) / 2f, 0.2f); line.GetComponent<Renderer>().material.color = Color.gray; Destroy(line.GetComponent<Collider>());
        BoxCollider col = gameObject.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(3, 3, 3);
    }
}

public class LedgeClimb : MonoBehaviour
{
    private void Start() { BoxCollider col = gameObject.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(3, 1, 3); }
}


