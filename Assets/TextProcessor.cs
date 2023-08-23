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
    private string lastPartialResult = "";
    private string speechPrompt = "...";

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
        //StartCoroutine(WaitAndShowStartButton());
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

    /*private IEnumerator WaitAndShowStartButton()
    {
        yield return new WaitForSeconds(.66f);

    }*/

    public void OnStartRecordingPressed()
    {
        print("Start Clicked");
        debugText.text = "starting...";
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
        previewText.gameObject.SetActive(true);
        errorText.gameObject.SetActive(true);
        StartRecording();
    }

    /*public void StartRecording()
    {
        SpeechRecognizer.StartRecording(true);
        previewText.gameObject.SetActive(true);
        errorText.gameObject.SetActive(true);
        previewText.text = cumulativeText + speechPrompt;
    }

    public void OnPartialResult(string result)
    {
        previewText.text = cumulativeText + result; ;
    }

    public void OnFinalResult(string result)
    {
        previewText.text = cumulativeText + result;
        cumulativeText += result + ". ";
        StartRecording();
    }*/

    public void StartRecording()
    {
        lastPartialResult = ""; // Reset the last partial result on a new recording
        SpeechRecognizer.StartRecording(true);        
        previewText.text = cumulativeText + speechPrompt;
    }

    public void OnPartialResult(string result)
    {
        if (result.StartsWith(lastPartialResult))
        {
            // Only append the new part of the result
            string newText = result.Substring(lastPartialResult.Length);
            cumulativeText += newText;
            lastPartialResult = result;
            previewText.text = cumulativeText;
        }
        else
        {
            // Handle the case where the new partial result doesn't start with the previous partial result (e.g., correction or complete change)
            cumulativeText = cumulativeText.Substring(0, cumulativeText.Length - lastPartialResult.Length) + result;
            lastPartialResult = result;
            previewText.text = cumulativeText;
        }
    }

    public void OnFinalResult(string result)
    {
        cumulativeText = cumulativeText.Substring(0, cumulativeText.Length - lastPartialResult.Length) + result + ". ";
        lastPartialResult = ""; // Reset the last partial result
        previewText.text = cumulativeText;
        StartRecording();
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
            previewText.text = speechPrompt;
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

    
}
