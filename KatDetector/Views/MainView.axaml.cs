using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace KatDetector.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Console.WriteLine(">>>> LA APP DE MATIAS ESTA CARGADA <<<<");
    }

    private byte[]? _currentImageBytes = null;

    public async void OnSelectImageClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Selecciona una imagen",
            AllowMultiple = false,
            FileTypeFilter = new[] { Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll }
        });

        if (files.Count > 0)
        {
            // 1. Abrimos el stream del archivo
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _currentImageBytes = memoryStream.ToArray();
            
            // 3. Importante: Resetear la posición al inicio para que el Bitmap pueda leerlo
            memoryStream.Position = 0;

            // 4. Creamos el Bitmap desde la memoria local
            ImagenMostrada.Source = new Bitmap(memoryStream);

            BtnProcesar.IsEnabled = true;

            TxtEstado.Text = $"Archivo cargado: {files[0].Name}";
            Console.WriteLine($">>>> Éxito: {files[0].Name} cargado en memoria.");
        }
    }

    private async void OnProcessClick(object sender, RoutedEventArgs e)
    {
        if (_currentImageBytes == null) return;

        TxtEstado.Text = "Enviando al server...";
        BtnProcesar.IsEnabled = false;

        try
        {
            using var client = new System.Net.Http.HttpClient();
            var content = new System.Net.Http.MultipartFormDataContent();
            var imageContent = new System.Net.Http.ByteArrayContent(_currentImageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            
            content.Add(imageContent, "file", "image.png");

            // Cambia la URL por la que te dé tu API al correr (ej: http://localhost:5000)
            var response = await client.PostAsync("http://localhost:8000/process-image", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                using var ms = new MemoryStream(responseBytes);
                ImagenProcesada.Source = new Bitmap(ms);
                TxtEstado.Text = "Procesamiento completado por OpenCV";
            }
            else 
            {
                TxtEstado.Text = "Error en el servidor: " + response.StatusCode;
            }
        }
        catch (Exception ex)
        {
            TxtEstado.Text = "Error de conexión: " + ex.Message;
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            BtnProcesar.IsEnabled = true;
        }
        
    }
}