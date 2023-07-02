using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using NuGet.Protocol;
using static NuGet.Client.ManagedCodeConventions;
using System.Reflection;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/", async (HttpContext context) =>
{
    
    var viewHtml = await File.ReadAllTextAsync("Views/ImageUploaderView.cshtml");
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(viewHtml);
});

app.MapPost("/", async context =>
{
    var form = await context.Request.ReadFormAsync();
    if (String.IsNullOrEmpty(form["title"]) || form.Files.Count == 0){
    context.Response.StatusCode = 400;
    await context.Response.WriteAsync("Enter all details");
    return;
}
    string imageTitle = form["title"];
    IFormFile imageFile = form.Files.GetFile("ImageFile");
    string imageId = Guid.NewGuid().ToString();
   

    //validate 
    var fileExtension = Path.GetExtension(imageFile.FileName);
  
    if (!(fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".gif")) {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("The file extension" +fileExtension +" is not supported.");
        return;
    }
    bool firstImage = true;
    // check if already exists
   /* string jsonFilePath = "jsonDB.json";
    string desiredFilename = imageFile.FileName;
    string jsonData = File.ReadAllText(jsonFilePath);

    if (!string.IsNullOrEmpty(jsonData)) {
        firstImage = false;
        using (JsonDocument document = JsonDocument.Parse(jsonData))
        {

            JsonElement imageFileElement = document.RootElement.GetProperty("ImageFile");

            JsonElement fileNameElement = imageFileElement.GetProperty("FileName");
            if (fileNameElement.GetString()== desiredFilename)
            {
                await context.Response.WriteAsync("The file " + imageFile.FileName + " already exists.");
                return;
            }

        }
    }*/

    
    //save image
   var newFilePath = "./Data/"+imageFile.FileName;

    using (var stream1 = System.IO.File.Create(newFilePath))
    {
        await imageFile.CopyToAsync(stream1);
    }
    JsonObject imageObject = new JsonObject();
    imageObject["Title"] = imageTitle;
    imageObject["ImageId"] = imageId;
    imageObject["FileName"] = imageFile.FileName;
    imageObject["ContentType"] = imageFile.ContentType;
    // Write in JSON //


    var fileName = "JSONDb.json";
    using var stream = File.Create(fileName);
    await JsonSerializer.SerializeAsync(stream, imageObject);
    await stream.DisposeAsync();
   /* if (!firstImage)
    {
        jsonString = "," + System.Environment.NewLine + jsonString ;
    }
    File.AppendAllText(fileName, jsonString);*/

    // redirect to following page
    context.Response.Redirect($"/picture/{imageId}"); //The url should be in the form of "/picture/{xxxx}" where xxxx is uploaded image id
});

app.MapGet("/picture/{imageId}" ,async(string imageId, HttpContext context) =>
{
    string jsonFilePath = "jsonDB.json";
    string jsonData = File.ReadAllText(jsonFilePath);
    string imageToBeDisplayedFileName = null;
    string imageToBeDisplayedContentType = null;
    string imageToBeDisplayedTitle = null;
    if (!string.IsNullOrEmpty(jsonData))
    {
        using (JsonDocument document = JsonDocument.Parse(jsonData))
        {

            JsonElement imageIdElement = document.RootElement.GetProperty("ImageId");
            if (imageIdElement.GetString() == imageId)
            {

                JsonElement imageFileElement = document.RootElement.GetProperty("FileName");

                //JsonElement fileNameElement = imageFileElement.GetProperty("FileName");
                imageToBeDisplayedFileName = imageFileElement.GetString();

                //JsonElement ContentTypeElement = imageFileElement.GetProperty("ContentType");
                //imageToBeDisplayedContentType = ContentTypeElement.GetString();
                JsonElement imageContentType = document.RootElement.GetProperty("ContentType");
                imageToBeDisplayedContentType = imageContentType.GetString();
                JsonElement imageTitle = document.RootElement.GetProperty("Title");
                imageToBeDisplayedTitle = imageTitle.GetString();
            }

        }
    }
    string imagePath = Path.Combine("Data", imageToBeDisplayedFileName);

    if (File.Exists(imagePath))
    {

        // Set the content type

        context.Response.ContentType = imageToBeDisplayedContentType;

        // Read the image bytes
        byte[] imageArray = await File.ReadAllBytesAsync(imagePath);
        string imageString = Convert.ToBase64String(imageArray);

        var viewHtml = $@"<html>
                <head>
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                    <link rel=""stylesheet"" href=""https://www.w3schools.com/w3css/4/w3.css"">
                    <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css"">
                    <style>
                     .center {{display: block;
                          margin-left: auto;
                          margin-right: auto;
                          width: 25%;
                          text-align:center;
                          }}


                    body {{
                        display: flex;
                        flex-direction:column;
                        min-height:100vh;
                        vertical-align:middle;
            
                    }}
 
                      
                    </style>
                     <title> {imageToBeDisplayedTitle} </title>
                </head>
                <body>
                   
                    <h1 class = ""center"">{imageToBeDisplayedTitle}</h1>
                    <img src=""data:image/jpeg;base64,{imageString}"" class=""center w3-circle"">
                    <br>
                    <input type=""button"" class = ""center btn btn-primary ""value=""Back"" onclick=""history.back()""/> 
                </body>
            </html>";

        return Results.Text(viewHtml, "text/html");

    }
    var viewHtmlEmpty = $@"<html><html>";
        return Results.Text(viewHtmlEmpty, "text/html"); ;
 
});

// <img src=""data:image/jpeg;base64,{{imageString}}"" alt = """"{{imageToBeDisplayedTitle}}""class= ""w3-circle"">
/*var viewHtml = $@"<html>
    <body>
     <img src=""data:image/jpeg;base64,{imageToBeDisplayed}"">

    <body>

    </html>
    ";
    return Results.Text(viewHtml, "text/html");

}
);*/


app.Run();
