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
    private string speechPrompt = "...";

    private void Awake()
    {
        titleText.enabled = true;
        startRecordingButton.enabled = true;
        previewText.enabled = false;
        errorText.enabled = false;
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
        startRecordingButton.enabled = true;

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
        StartRecording();
    }

    public void StartRecording()
    {
        SpeechRecognizer.StartRecording(true);
        previewText.enabled = true;
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
