using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ChatGPTManager : MonoBehaviour
{
    static protected string apikey = "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";
    static protected string url = "https://api.openai.com/v1/chat/completions";

    public static ChatGPTManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void PostRequest(string message)
    {
        var usermessage = new Message("user", message);
        StartCoroutine(PostRequest(usermessage));
    }

    IEnumerator PostRequest(Message inputText)
    {
        Message promptMessage = new Message("assistant", "Kamu adalah seorang penilai jawaban pada suatu soal yang hanya boleh merespon benar atau salah.");
        Message[] messages = { promptMessage, inputText };

        string jsonData = JsonUtility.ToJson(new OpenAIAPIRequest(messages));

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apikey}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Received: " + request.downloadHandler.text);
            // Handle the response here, display it in UI, etc.
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }


    [Serializable]
    public class OpenAIAPIRequest
    {
        public string model = "gpt-3.5-turbo";
        public Message[] messages;
        public float temperature = 0.5f;
        public int max_tokens = 50;
        public float top_p = 1f;
        public float presence_penalty = 0f;
        public float frequency_penalty = 0f;

        public OpenAIAPIRequest(Message[] messages, string model = "gpt-3.5.turbo", float temperature = 0.1f, int maxToken = 50, float top_p = 1f, float presence_penalty = 0f, float frequency_penalty = 0f)
        {
            this.model = model;
            this.messages = messages;
            this.temperature = temperature;
            this.max_tokens = maxToken;
            this.top_p = top_p;
            this.presence_penalty = presence_penalty;
            this.frequency_penalty = frequency_penalty;

        }
    }

    [Serializable]
    public class Message
    {
        public string role = "assistant";
        public string content = "";

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
