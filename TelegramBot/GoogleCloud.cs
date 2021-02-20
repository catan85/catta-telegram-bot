using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
public class GoogleCloud
{
    public GoogleCloud()
    {
        string projectId = "catta-telegram-bot-storage";
        //Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"/tmp/keys/googleCloudStorageKey.json");
    }

    public void UploadFileToGoogleStorage(string filename)
    {
        StorageClient storage = StorageClient.Create();
        string bucketName = "catta-telegram-bot-voice-messages";

        try
        {
            UploadFile(bucketName, filename, ref storage);
            Console.WriteLine("Uploaded file " + filename);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void UploadFile(string bucketName, string localPath, ref StorageClient storage, string objectName = null)
    {
        using (var f = File.OpenRead(localPath))
        {
            objectName = objectName ?? Path.GetFileName(localPath);
            storage.UploadObject(bucketName, objectName, null, f);
        }
    }

    public void DeleteFileFromGoogleStorage(string filename)
    {
        string bucketName = "catta-telegram-bot-voice-messages";

        try
        {
            DeleteFile(bucketName, filename);
            Console.WriteLine("Deleted file " + filename);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }


    public void DeleteFile(
            string bucketName = "your-unique-bucket-name",
            string objectName = "your-object-name")
    {
        var storage = StorageClient.Create();
        storage.DeleteObject(bucketName, objectName);
    }
    

    public string AudioFileToText(string fileName)
    {
        string gcsUri = $"gs://catta-telegram-bot-voice-messages/{fileName}";
        var speech = SpeechClient.Create();
        var response = speech.Recognize(new RecognitionConfig()
        {
            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
            SampleRateHertz = 48000,
            LanguageCode = "it-IT",
        }, RecognitionAudio.FromStorageUri(gcsUri));

        string text = "";

        foreach (var result in response.Results)
        {
            foreach (var alternative in result.Alternatives)
            {
                text += alternative.Transcript + "\n";
            }
        }

        return text;
    }




    

}
