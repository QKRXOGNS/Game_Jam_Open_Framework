using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using System.IO; 
using System;
using UnityEngine.UI;

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}



[System.Serializable]
public class InlineData
{
    public string mimeType;
    public string data;
}

// Text-only part
[System.Serializable]
public class TextPart
{
    public string text;
}

// Image-capable part
[System.Serializable]
public class ImagePart
{
    public string text;
    public InlineData inlineData;
}

[System.Serializable]
public class TextContent
{
    public string role;
    public TextPart[] parts;
}

[System.Serializable]
public class TextCandidate
{
    public TextContent content;
}

[System.Serializable]
public class TextResponse
{
    public TextCandidate[] candidates;
}

[System.Serializable]
public class ImageContent
{
    public string role;
    public ImagePart[] parts;
}

[System.Serializable]
public class ImageCandidate
{
    public ImageContent content;
}

[System.Serializable]
public class ImageResponse
{
    public ImageCandidate[] candidates;
}


// For text requests
[System.Serializable]
public class ChatRequest
{
    public TextContent[] contents;
    public TextContent system_instruction;
}


public class UnityAndGeminiV3: MonoBehaviour
{
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;

    public enum GeminiModel
    {
        Gemini_2_0_Flash_Exp = 0,
        Gemini_2_0_Flash = 1,
        Gemini_1_5_Pro = 2,
        Gemini_1_5_Flash = 3,
        Gemini_1_0_Pro = 4
    }
    
    public enum GeminiImageModel
    {
        Gemini_2_0_Flash_Exp_Image_Generation = 0,
        Gemini_Imagen_3_0_Generate_001 = 1
    }
    
    [Header("Model Selection")]
    public GeminiModel selectedModel = GeminiModel.Gemini_2_0_Flash;
    public GeminiImageModel selectedImageModel = GeminiImageModel.Gemini_2_0_Flash_Exp_Image_Generation;
    
    private string apiKey = "";

    [Header("ChatBot Function")]
    public TMP_InputField inputField;
    public TMP_Text uiText;
    public string botInstructions;
    private TextContent[] chatHistory;


    [Header("Prompt Function")]
    public string prompt = "";

    [Header("Image Prompt Function")]
    public string imagePrompt = "";
    public Image imageDisplay; 

    [Header("Media Prompt Function")]
    // Receives files with a maximum of 20 MB
    public string mediaFilePath = "";
    public string mediaPrompt = "";
    public enum MediaType
    {
        Video_MP4 = 0,
        Audio_MP3 = 1,
        PDF = 2,
        JPG = 3,
        PNG = 4
    }
    public MediaType mimeType = MediaType.Video_MP4;
    

    public string GetMimeTypeString()
    {
        switch (mimeType)
        {
            case MediaType.Video_MP4:
                return "video/mp4";
            case MediaType.Audio_MP3:
                return "audio/mp3";
            case MediaType.PDF:
                return "application/pdf";
            case MediaType.JPG:
                return "image/jpeg";
            case MediaType.PNG:
                return "image/png";
            default:
                return "error";
        }
    }

    public string GetModelEndpoint()
    {
        string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        switch (selectedModel)
        {
            case GeminiModel.Gemini_2_0_Flash_Exp:
                return baseUrl + "gemini-2.0-flash-exp:generateContent";
            case GeminiModel.Gemini_2_0_Flash:
                return baseUrl + "gemini-2.0-flash:generateContent";
            case GeminiModel.Gemini_1_5_Pro:
                return baseUrl + "gemini-1.5-pro:generateContent";
            case GeminiModel.Gemini_1_5_Flash:
                return baseUrl + "gemini-1.5-flash:generateContent";
            case GeminiModel.Gemini_1_0_Pro:
                return baseUrl + "gemini-1.0-pro:generateContent";
            default:
                return baseUrl + "gemini-2.0-flash:generateContent";
        }
    }

    public string GetImageModelEndpoint()
    {
        string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        switch (selectedImageModel)
        {
            case GeminiImageModel.Gemini_2_0_Flash_Exp_Image_Generation:
                return baseUrl + "gemini-2.0-flash-exp-image-generation:generateContent";
            case GeminiImageModel.Gemini_Imagen_3_0_Generate_001:
                return baseUrl + "imagen-3.0-generate-001:generateContent";
            default:
                return baseUrl + "gemini-2.0-flash-exp-image-generation:generateContent";
        }
    }


    void Start()
    {
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;   
        chatHistory = new TextContent[] { };
        if (prompt != ""){StartCoroutine( SendPromptRequestToGemini(prompt));};
        if (imagePrompt != ""){StartCoroutine( SendPromptRequestToGeminiImageGenerator(imagePrompt));};
        if (mediaPrompt != "" && mediaFilePath != ""){StartCoroutine(SendPromptMediaRequestToGemini(mediaPrompt, mediaFilePath));};
    }

    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{GetModelEndpoint()}?key={apiKey}";
     
        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"{" + promptText + "}\"}]}]}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string text = response.candidates[0].content.parts[0].text;
                        Debug.Log(text);
                    }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }

    public void SendChat()
    {
        string userMessage = inputField.text;
        StartCoroutine( SendChatRequestToGemini(userMessage));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage)
    {

        string url = $"{GetModelEndpoint()}?key={apiKey}";
     
        TextContent userContent = new TextContent
        {
            role = "user",
            parts = new TextPart[]
            {
                new TextPart { text = newMessage }
            }
        };

        TextContent instruction = new TextContent
        {
            parts = new TextPart[]
            {
                new TextPart {text = botInstructions}
            }
        }; 

        List<TextContent> contentsList = new List<TextContent>(chatHistory);
        contentsList.Add(userContent);
        chatHistory = contentsList.ToArray(); 

        ChatRequest chatRequest = new ChatRequest { contents = chatHistory, system_instruction = instruction };

        string jsonData = JsonUtility.ToJson(chatRequest);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string reply = response.candidates[0].content.parts[0].text;
                        TextContent botContent = new TextContent
                        {
                            role = "model",
                            parts = new TextPart[]
                            {
                                new TextPart { text = reply }
                            }
                        };

                        Debug.Log(reply);
                        //This part shows the text in the Canvas
                        uiText.text = reply;
                        //This part adds the response to the chat history, for your next message
                        contentsList.Add(botContent);
                        chatHistory = contentsList.ToArray();
                    }
                else
                {
                    Debug.Log("No text found.");
                }
             }
        }  
    }


    private IEnumerator SendPromptRequestToGeminiImageGenerator(string promptText)
    {
        string url = $"{GetImageModelEndpoint()}?key={apiKey}";
        
        // Create the proper JSON structure with model specification
        string jsonData = $@"{{
            ""contents"": [{{
                ""parts"": [{{
                    ""text"": ""{promptText}""
                }}]
            }}],
            ""generationConfig"": {{
                ""responseModalities"": [""Text"", ""Image""]
            }}
        }}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError(www.error);
            } 
            else 
            {
                Debug.Log("Request complete!");
                Debug.Log("Full response: " + www.downloadHandler.text); // Log full response for debugging
                
                // Parse the JSON response
                try 
                {
                    ImageResponse response = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);
                    
                    if (response.candidates != null && response.candidates.Length > 0 && 
                        response.candidates[0].content != null && 
                        response.candidates[0].content.parts != null)
                    {
                        foreach (var part in response.candidates[0].content.parts)
                        {
                            if (!string.IsNullOrEmpty(part.text))
                            {
                                Debug.Log("Text response: " + part.text);
                            }
                            else if (part.inlineData != null && !string.IsNullOrEmpty(part.inlineData.data))
                            {
                                // This is the base64 encoded image data
                                byte[] imageBytes = System.Convert.FromBase64String(part.inlineData.data);
                                
                                // Create a texture from the bytes
                                Texture2D tex = new Texture2D(2, 2);
                                tex.LoadImage(imageBytes);
                                byte[] pngBytes = tex.EncodeToPNG();
                                string path = Application.persistentDataPath + "/gemini-image.png";
                                File.WriteAllBytes(path, pngBytes);
                                Debug.Log("Saved to: " + path);
                                Debug.Log("Image received successfully!");

                                // Load the saved image back as Texture2D
                                string imagePath = Path.Combine(Application.persistentDataPath, "gemini-image.png");
                                
                                Texture2D generatedTex = new Texture2D(2, 2);
                                generatedTex.LoadImage(File.ReadAllBytes(imagePath));
                                
                                // Apply to UI Image component
                                if (imageDisplay != null)
                                {
                                    // Create sprite from texture
                                    Sprite imageSprite = Sprite.Create(generatedTex, 
                                        new Rect(0.0f, 0.0f, generatedTex.width, generatedTex.height), 
                                        new Vector2(0.5f, 0.5f));
                                    
                                    imageDisplay.sprite = imageSprite;
                                    Debug.Log("Image displayed in UI Image component!");
                                }
                                else
                                {
                                    Debug.LogError("Image Display component not assigned!");
                                }

                            

                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No valid response parts found.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                }
            }
        }
    }


    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private IEnumerator SendPromptMediaRequestToGemini(string promptText, string mediaPath)
    {
        // Read video file and convert to base64
        byte[] mediaBytes = File.ReadAllBytes(mediaPath);
        string base64Media = System.Convert.ToBase64String(mediaBytes);

        string url = $"{GetModelEndpoint()}?key={apiKey}";

        string mimeTypeMedia = GetMimeTypeString();



        string jsonBody = $@"
        {{
        ""contents"": [
            {{
            ""parts"": [
                {{
                ""text"": ""{promptText}""
                }},
                {{
                ""inline_data"": {{
                    ""mime_type"": ""{mimeTypeMedia}"",
                    ""data"": ""{base64Media}""
                }}
                }}
            ]
            }}
        ]
        }}";


        // Serialize the request into JSON
        // string jsonData = JsonUtility.ToJson(jsonBody);
        Debug.Log("Sending JSON: " + jsonBody); // For debugging

        // byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody);


        // Create and send the request
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError(www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            } 
            else 
            {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                {
                    string text = response.candidates[0].content.parts[0].text;
                    Debug.Log(text);
                }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }

}



