using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GisHelper.Controllers;

[Route("convert")]
public class ConvertController : ControllerBase
{
    public ConvertController()
    {
    }

    [HttpPost]
    [Route("/geojson")]
    public async Task<IActionResult> ConvertToGeoJson(
        IFormFile file,
        int x, int y, int z,
        int sourceSrid = 3857,
        int targetSrid = 4326,
        string layerName = "feature",
        CancellationToken ct = default)
    {
        string workDirectory = AppDomain.CurrentDomain.BaseDirectory + "temp";
        string inputFileName = file.FileName;
        string inputFilePath = workDirectory + "\\" + inputFileName;
        string outputFileName = inputFileName + ".geojson";
        string outputFilePath = workDirectory + "\\" + outputFileName;

        try
        {
            using (var stream = System.IO.File.OpenWrite(inputFilePath))
            {
                await file.CopyToAsync(stream);
            }

            var args = $"-s_srs EPSG:{sourceSrid} -t_srs EPSG:{targetSrid} \"{workDirectory}\\{outputFileName}\" \"{workDirectory}\\{inputFileName}\" -oo x={x} -oo y={y} -oo z={z} {layerName}";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ogr2ogr",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = proc.StandardError.ReadToEnd();
            Console.WriteLine(err);
            await proc.WaitForExitAsync();

            // для запуска на хостовой машине
            // var args = $"run --rm -v \"{workDirectory}:/tiles\" ghcr.io/osgeo/gdal:alpine-small-latest ogr2ogr -s_srs EPSG:3857 -t_srs EPSG:4326 \"/tiles/{outputFileName}\" \"/tiles/{inputFileName}\" -oo x={x} -oo y={y} -oo z={z} {layerName}";
            // FileName = "docker";

            var waitingTimeout = DateTime.Now.AddSeconds(10);

            while (true)
            {
                if (!System.IO.File.Exists(outputFilePath))
                    await Task.Delay(1000);
                else break;
                if (DateTime.Now > waitingTimeout)
                    throw new TimeoutException();
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(outputFilePath);
            return File(fileBytes, "application/json", outputFileName);
        }
        finally
        {
            System.IO.File.Delete(outputFilePath);
            System.IO.File.Delete(inputFilePath);
        }
    }
}
