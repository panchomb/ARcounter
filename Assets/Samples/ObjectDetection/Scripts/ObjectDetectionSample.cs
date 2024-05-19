// Copyright 2022-2024 Niantic.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.Subsystems.ObjectDetection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectDetectionSample: MonoBehaviour
{
    [SerializeField]
    private float _probabilityThreshold = 0.5f;
    
    // [SerializeField] 
    // private SliderToggle _filterToggle;
    
    [SerializeField]
    private ARObjectDetectionManager _objectDetectionManager;
    
    private Color[] _colors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.black
    };
    
    // [SerializeField] [Tooltip("Slider GameObject to set probability threshold")]
    // private Slider _probabilityThresholdSlider;

    // [SerializeField] [Tooltip("Text to display current slider value")]
    // private Text _probabilityThresholdText;

    // [SerializeField]
    // private Dropdown _categoryDropdown;

    // [SerializeField]
    // private DrawRect _drawRect;

    private Canvas _canvas;
    // [SerializeField] [Tooltip("Categories to display in the dropdown")]
    // private List<string> _categoryNames;

    private bool _filterOn = false;

    // New Attributes for counter

    [SerializeField]
    private Image _countRectangle;

    [SerializeField]
    private TextMeshProUGUI _countText;

    private Queue<int> _lastTenCounts = new Queue<int>(10);

    // New attributes for capture button

    [SerializeField]
    private Button _captureButton;

    // The name of the actively selected semantic category
    private string _categoryName = string.Empty;
    private void Awake()
    {
        _canvas = FindObjectOfType<Canvas>();


        // _probabilityThresholdSlider.value = _probabilityThreshold;
        // _probabilityThresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
        // OnThresholdChanged(_probabilityThresholdSlider.value);

        // _categoryDropdown.onValueChanged.AddListener(categoryDropdown_OnValueChanged);

    }
    
    private void OnMetadataInitialized(ARObjectDetectionModelEventArgs args)
    {
        _objectDetectionManager.ObjectDetectionsUpdated += ObjectDetectionsUpdated;

        // Display person by default.
        // _categoryName = _categoryNames[0];
        // if (_categoryDropdown is not null && _categoryDropdown.options.Count == 0)
        // {
        //     _categoryDropdown.AddOptions(_categoryNames.ToList());

        //     var dropdownList = _categoryDropdown.options.Select(option => option.text).ToList();
        //     _categoryDropdown.value = dropdownList.IndexOf(_categoryName);
        // }

    }

    private void ObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
    {
        var result = args.Results;
        float confidence;
        if (result == null)
        {
            _countText.text = "Count: 0";
            return;
        }

        int vehicleCount = 0;

        for (int i = 0; i < result.Count; i++)
        {
            confidence = result[i].GetConfidence("vehicle");
            if (confidence > _probabilityThreshold)
            {
                vehicleCount++;
            }
        }

        if (_lastTenCounts.Count >= 10)
        {
            _lastTenCounts.Dequeue();
        }
        _lastTenCounts.Enqueue(vehicleCount);

        int modeCount;
        var groupedCounts = _lastTenCounts.GroupBy(x => x).OrderByDescending(g => g.Count()).ToList();
        if (groupedCounts.Count > 0 && groupedCounts[0].Count() > 1)
        {
            modeCount = groupedCounts[0].Key;
        }
        else
        {
            modeCount = vehicleCount;
        }


        _countText.text = $"Count: {modeCount}";
    }
    // private void OnThresholdChanged(float newThreshold){
    //     _probabilityThreshold = newThreshold;
    //     _probabilityThresholdText.text = "Confidence : " + newThreshold.ToString();
    // }
    // private void categoryDropdown_OnValueChanged(int val)
    // {
    //     // Update the display category from the dropdown value.
    //     _categoryName = _categoryDropdown.options[val].text;
    // }
    public void Start()
    {
        _objectDetectionManager.enabled = true;
        _objectDetectionManager.MetadataInitialized += OnMetadataInitialized;
        // _filterToggle.onValueChanged.AddListener(ToggleFilter);
        // _filterOn = _filterToggle.isOn;
        // _categoryDropdown.interactable = _filterOn;
    }
    // private void ToggleFilter(bool on){
    //     _filterOn = on;
    //     _categoryDropdown.interactable = on;
    // }
    private void OnDestroy()
    {
        _objectDetectionManager.MetadataInitialized -= OnMetadataInitialized;
        _objectDetectionManager.ObjectDetectionsUpdated -= ObjectDetectionsUpdated;
        // if (_probabilityThresholdSlider)
        // {
        //     _probabilityThresholdSlider.onValueChanged.RemoveListener(OnThresholdChanged);
        // }
        // if (_categoryDropdown is not null)
        // {
        //     _categoryDropdown.onValueChanged.RemoveListener(categoryDropdown_OnValueChanged);
        // }
        // _filterToggle.onValueChanged.RemoveListener(ToggleFilter);
    }

    public void CaptureObjects() 
    {
        StartCoroutine(CaptureObjectsCoroutine());
    }

    private IEnumerator CaptureObjectsCoroutine()
    {
        yield return new WaitForEndOfFrame();

        Niantic.Lightship.AR.XRSubsystems.XRDetectedObject[] detectedObjects;
        if (!_objectDetectionManager.TryGetDetectedObjects(out detectedObjects) || detectedObjects.Length == 0)
        {
            Debug.Log("No objects detected.");
            yield break;
        }

        foreach (var obj in detectedObjects)
        {
            float confidence = obj.GetConfidence("vehicle");
            int h = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.height);
            int w = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.width);
            if (confidence > _probabilityThreshold)
            {
                Rect _rect = obj.CalculateRect(w, h, Screen.orientation);
                yield return StartCoroutine(CaptureBoundingBox(_rect));
            }
        }
    }

    private IEnumerator CaptureBoundingBox(Rect boundingBox) {
        // Calculate the pixel coordinates of the bounding box
        int h = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.height);
        int w = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.width);  

        int x = Mathf.FloorToInt(boundingBox.x);
        int y = Mathf.FloorToInt(boundingBox.y);
        int width = Mathf.FloorToInt(boundingBox.width);
        int height = Mathf.FloorToInt(boundingBox.height);

        Texture2D screenImage = new Texture2D(w, h, TextureFormat.RGB24, false);
        screenImage.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        screenImage.Apply();

        Texture2D croppedImage = new Texture2D(width, height);
        croppedImage.SetPixels(screenImage.GetPixels(x, y, width, height));
        croppedImage.Apply();

        // Encode the cropped image to PNG
        byte[] bytes = croppedImage.EncodeToPNG();

        // Save to gallery using NativeGallery plugin
        string fileName = $"CapturedObject_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        NativeGallery.SaveImageToGallery(bytes, "ObjectCaptures", fileName, (success, path) =>
        {
            Debug.Log($"Saved captured object to: {path}");
        });
        yield return null;
    }
}