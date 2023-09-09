using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KKSpeech;
using TMPro;

using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using SimpleJSON;


public class TextProcessor : MonoBehaviour
{
    public Button startRecordingButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI previewText;
    public TextMeshProUGUI answersText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI debugText;

    private string cumulativeText = "";
    private string currentRecording = "";
    private string lastChunkEnd = "";

    private string speechPrompt = "...";

    private float chunkingTime = 12f;

    private void Awake()
    {
        titleText.gameObject.SetActive(true);
        startRecordingButton.gameObject.SetActive(true);
        previewText.gameObject.SetActive(false);
        //answersText.gameObject.SetActive(false);
        errorText.gameObject.SetActive(false);
        debugText.text = "";
    }

    void Start()
    {
        print("Initializing...");
        InitializeRecorder();
        SendTextSnippet("and so then I asked.  Layers of the OSI stack.  I think I know some networking.  Will there be a test on");
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
        answersText.gameObject.SetActive(true);
        errorText.gameObject.SetActive(true);
        //answersText.text = "";
        errorText.text = "";
        StartRecording();
        InvokeRepeating("ChunkText", chunkingTime, chunkingTime);
    }

    public void StartRecording()
    {
        print("starting new recording");
        currentRecording = ""; // Reset current recording text
        lastChunkEnd = cumulativeText; // Initialize last chunk end to current cumulative text
        SpeechRecognizer.StartRecording(true);
        previewText.text = cumulativeText + speechPrompt;
    }

    public void OnPartialResult(string result)
    {
        print("partial result");
        currentRecording = result; // Always update the current recording with the latest partial result
        previewText.text = cumulativeText + currentRecording + speechPrompt;
    }

    public void OnFinalResult(string result)
    {
        print("final result");
        cumulativeText += result + " "; // Append the final result to cumulativeText
        lastChunkEnd = cumulativeText; // Update the last chunk end marker
        currentRecording = ""; // Reset current recording to ensure no duplicates
        previewText.text = cumulativeText + speechPrompt;
        StartRecording(); // Restart the recording
        SendTextSnippet(result);
    }

    public void ChunkText()
    {
        string newText = cumulativeText.Substring(lastChunkEnd.Length) + currentRecording;
        cumulativeText += newText + "|"; // Append new text and a bar to delineate chunks
        lastChunkEnd = cumulativeText; // Update the last chunk end marker
        /*if (newText.Length > 15)
        {
            SendTextSnippet(newText);
        }*/
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
        //Debug.LogError(error);
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

    public void SendTextSnippet(string snippet)
    {
        StartCoroutine(PostRequest("https://kbr1ahdla6.execute-api.us-west-2.amazonaws.com/snippet", snippet));
    }

    private IEnumerator PostRequest(string url, string snippet)
    {
        // Create the payload
        JSONNode payload = new JSONObject();
        payload["payload"]["text_snippet"] = snippet;
        payload["payload"]["use_test_data"] = false;

        // Create the request
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload.ToString());
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending Snippet: " + snippet);
        // Send the request and wait for a response
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Successful QnA Return");
            // Parse the response
            JSONNode responseJSON = JSON.Parse(www.downloadHandler.text);

            /*string userExit = responseJSON["user_exit"];
            string message = responseJSON["message"];*/
            JSONArray qnaPairs = responseJSON["qna_pairs"].AsArray;

            /*Debug.Log("User Exit: " + userExit);
            Debug.Log("Message: " + message);*/

            // Loop through qnaPairs
            /*for (int i = 0; i < qnaPairs.Count; i++)
            {
                Debug.Log("Question: " + qnaPairs[i]["question"]);
                Debug.Log("Answer: " + qnaPairs[i]["answer"]);


            }*/
            UpdateAnswersText(qnaPairs);
        }
    }
    public void UpdateAnswersText(JSONArray qnaPairs)
    {
        // Initialize a StringBuilder for efficient string concatenation
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Loop through qnaPairs and append each question and answer pair to the StringBuilder
        for (int i = 0; i < qnaPairs.Count; i++)
        {
            string question = qnaPairs[i]["question"];
            string answer = qnaPairs[i]["answer"];

            // Add question in orange and slightly smaller font (assuming default font size is 50)
            sb.Append("<size=45><color=#FFA500>" + question + "</color></size>\n");

            // Add answer in bright green
            sb.Append("<color=#00FF00>" + answer + "</color>\n\n");  // Extra \n for spacing between pairs
        }

        // Prepend new content to existing TMP text
        answersText.text = sb.ToString() + answersText.text;
    }
}
