using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KKSpeech;
using TMPro;

public class TextProcessor : MonoBehaviour
{
    public Button startRecordingButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI previewText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI debugText;

    private string cumulativeText = "";
    private string currentRecording = "";
    private string speechPrompt = "...";

    private float chunkingTime = 7f;

    private void Awake()
    {
        titleText.gameObject.SetActive(true);
        startRecordingButton.gameObject.SetActive(true);
        previewText.gameObject.SetActive(false);
        errorText.gameObject.SetActive(false);
        debugText.text = "";
    }

    void Start()
    {
        print("Initializing...");
        InitializeRecorder();
        InvokeRepeating("ChunkText", chunkingTime, chunkingTime);
    }

    private void Update()
    {
        WriteLogs();
    }

    private void InitializeRecorder()
    {
        if (SpeechRecognizer.ExistsOnDevice())
        {
            SpeechRecognizerListener listener = GameObject.FindObjectOfType<SpeechRecognizerListener>();
            listener.onAuthorizationStatusFetched.AddListener(OnAuthorizationStatusFetched);
            listener.onAvailabilityChanged.AddListener(OnAvailabilityChange);
            listener.onErrorDuringRecording.AddListener(OnError);
            listener.onErrorOnStartRecording.AddListener(OnError);
            listener.onFinalResults.AddListener(OnFinalResult);
            listener.onPartialResults.AddListener(OnPartialResult);
            listener.onEndOfSpeech.AddListener(OnEndOfSpeech);
            SpeechRecognizer.RequestAccess();
        }
        else
        {
            errorText.text = "Sorry, but this device doesn't support speech recognition";
            startRecordingButton.gameObject.SetActive(false);
        }
    }

    public void OnStartRecordingPressed()
    {
        print("Start Clicked");
        if (SpeechRecognizer.IsRecording())
        {
#if UNITY_IOS && !UNITY_EDITOR
			SpeechRecognizer.StopIfRecording();
			startRecordingButton.gameObject.SetActive(false);
#elif UNITY_ANDROID && !UNITY_EDITOR
			SpeechRecognizer.StopIfRecording();
#endif
        }
        else
        {
            StartRecordingFirst();
        }
    }

    public void StartRecordingFirst()
    {
        startRecordingButton.gameObject.SetActive(false);
        titleText.gameObject.SetActive(false);
        previewText.gameObject.SetActive(true);
        errorText.gameObject.SetActive(true);
        errorText.text = "";
        StartRecording();
    }

    public void StartRecording()
    {
        currentRecording = ""; // Reset current recording text
        SpeechRecognizer.StartRecording(true);
        previewText.text = cumulativeText + speechPrompt;
    }

    public void OnPartialResult(string result)
    {
        currentRecording = result; // Always update the current recording with the latest partial result
        previewText.text = cumulativeText + currentRecording + speechPrompt;
    }

    public void OnFinalResult(string result)
    {
        cumulativeText += result + ". "; // Append the final result to cumulativeText
        currentRecording = ""; // Reset current recording
        previewText.text = cumulativeText + speechPrompt;
        StartRecording();
    }

    public void ChunkText()
    {
        cumulativeText += currentRecording + "| "; // Append the current recording and a bar to delineate chunks
        currentRecording = ""; // Reset current recording since it's now part of cumulativeText
    }


    public void OnAvailabilityChange(bool available)
    {
        startRecordingButton.gameObject.SetActive(available);
        if (!available)
        {
            errorText.text = "Speech Recognition not available";
        }
        else
        {
            //previewText.text = speechPrompt;
        }
    }

    public void OnAuthorizationStatusFetched(AuthorizationStatus status)
    {
        switch (status)
        {
            case AuthorizationStatus.Authorized:
                startRecordingButton.gameObject.SetActive(true);
                break;
            default:
                startRecordingButton.gameObject.SetActive(false);
                errorText.text = "Cannot use Speech Recognition, authorization status is " + status;
                break;
        }
    }

    public void OnEndOfSpeech()
    {
        StartRecording();
    }

    public void OnError(string error)
    {
        errorText.text = error;
        Debug.LogError(error);
        StartRecording();
    }

    public void OnLogButtonPress()
    {
        debugText.enabled = !debugText.enabled;
    }
    void WriteLogs()
    {
        debugText.text = LogManager.myLog;
    }
}
