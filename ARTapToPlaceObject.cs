using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObject : MonoBehaviour
{
    public GameObject[] objectsToSpawn; // Array of objects to spawn (now supports 3 objects)
    public ARPlaneManager planeManager;
    public Button togglePlaneButton; // Button to toggle plane detection
    public Button selectObject1Button; // Button to select object 1
    public Button selectObject2Button; // Button to select object 2
    public Button selectObject3Button; // Button to select object 3
    public Button rotateButton; // Button to rotate the object
    public Scrollbar sizeScrollbar; // UI Scrollbar reference for scaling
    public float heightOffset = 0.1f; // Height offset variable
    public float minScale = 0.1f, maxScale = 2.0f; // Scale range
    public float rotationDuration = 2.0f; // Duration of the rotation (in seconds)

    private GameObject spawnedObject;
    private ARRaycastManager raycastManager;
    private bool isPlaneDetectionActive = false; // Default: Plane detection is off
    private GameObject selectedObject; // Currently selected object to spawn
    private Dictionary<int, GameObject> spawnedObjects = new Dictionary<int, GameObject>(); // Track spawned objects
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        // Disable plane manager by default
        if (planeManager != null)
        {
            planeManager.enabled = false;
        }

        // Set up button listeners
        if (togglePlaneButton != null)
        {
            togglePlaneButton.onClick.AddListener(TogglePlaneDetection);
        }

        if (selectObject1Button != null)
        {
            selectObject1Button.onClick.AddListener(() => SelectObject(0)); // Select object 1
        }

        if (selectObject2Button != null)
        {
            selectObject2Button.onClick.AddListener(() => SelectObject(1)); // Select object 2
        }

        if (selectObject3Button != null)
        {
            selectObject3Button.onClick.AddListener(() => SelectObject(2)); // Select object 3
        }

        if (rotateButton != null)
        {
            rotateButton.onClick.AddListener(RotateObjectGradually); // Set the listener for the rotate button
        }

        if (sizeScrollbar != null)
        {
            sizeScrollbar.onValueChanged.AddListener(UpdateObjectScale);
        }
    }

    void Update()
    {
        if (!isPlaneDetectionActive)
            return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;

                // Adjust the rotation to spawn the object horizontally
                Quaternion horizontalRotation = Quaternion.Euler(-90, 0, 0); // Rotate -90 degrees around the X-axis
                Vector3 positionWithOffset = hitPose.position + Vector3.up * heightOffset;

                if (selectedObject != null)
                {
                    // Check if the selected object has already been spawned
                    int selectedIndex = System.Array.IndexOf(objectsToSpawn, selectedObject);
                    if (!spawnedObjects.ContainsKey(selectedIndex))
                    {
                        // Instantiate the selected object with the corrected rotation
                        spawnedObject = Instantiate(selectedObject, positionWithOffset, horizontalRotation);

                        // Adjust the rotation of the models based on their index
                        if (selectedIndex == 0) // First model (sleeping)
                        {
                            spawnedObject.transform.Rotate(90, 0, 0); // Rotate 90 degrees around the X-axis to make it stand
                            spawnedObject.transform.Rotate(0, 180, 0); // Rotate 180 degrees around the Y-axis to face the front
                        }
                        else if (selectedIndex == 1) // Second model
                        {
                            spawnedObject.transform.Rotate(180, 180, 0); // Rotate 180 degrees around the X and Y axes
                            spawnedObject.transform.Rotate(0, 180, 0); // Rotate 180 degrees around the Y-axis to face the front
                        }
                        else if (selectedIndex == 2) // Third model
                        {
                            spawnedObject.transform.Rotate(180, 0, 0); // Rotate 180 degrees around the X-axis
                            spawnedObject.transform.Rotate(0, 180, 0); // Rotate 180 degrees around the Y-axis to face the front
                        }

                        // Add the spawned object to the dictionary
                        spawnedObjects.Add(selectedIndex, spawnedObject);

                        UpdateObjectScale(sizeScrollbar.value); // Set initial scale
                    }
                    else
                    {
                        Debug.Log("Object already spawned: " + selectedObject.name);
                    }
                }
            }
        }
    }

    public void TogglePlaneDetection()
    {
        isPlaneDetectionActive = !isPlaneDetectionActive;

        if (planeManager != null)
        {
            planeManager.enabled = isPlaneDetectionActive;

            // Enable or disable all existing planes
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(isPlaneDetectionActive);
            }
        }

        Debug.Log("Plane Detection: " + (isPlaneDetectionActive ? "Enabled" : "Disabled"));
    }

    public void SelectObject(int index)
    {
        if (index >= 0 && index < objectsToSpawn.Length)
        {
            selectedObject = objectsToSpawn[index]; // Set the selected object
            Debug.Log("Selected Object: " + selectedObject.name);
        }
    }

    public void UpdateObjectScale(float value)
    {
        if (spawnedObject != null)
        {
            float scaleFactor = Mathf.Lerp(minScale, maxScale, value);
            spawnedObject.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    public void RotateObjectGradually()
    {
        if (spawnedObject != null)
        {
            StartCoroutine(RotateObjectCoroutine(spawnedObject.transform, 180f, rotationDuration));
        }
    }

    private IEnumerator RotateObjectCoroutine(Transform objTransform, float targetAngle, float duration)
    {
        Quaternion startRotation = objTransform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, targetAngle, 0);
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            objTransform.rotation = Quaternion.Slerp(startRotation, endRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        objTransform.rotation = endRotation; // Ensure it ends exactly at the target angle
    }
}
