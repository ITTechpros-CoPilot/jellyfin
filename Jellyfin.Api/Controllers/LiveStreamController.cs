using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LiveStreamController : ControllerBase
    {
        private readonly ILogger<LiveStreamController> _logger;
        private readonly string _liveStreamPath = "/path/to/live/streams";  // Path for live streams (HLS format)
        private readonly string _archivePath = "/path/to/archived/streams"; // Path to archive saved streams
        private readonly string _ffmpegPath = "/path/to/ffmpeg";            // Path to FFmpeg for processing

        public LiveStreamController(ILogger<LiveStreamController> logger)
        {
            _logger = logger;
        }

        // GET: api/livestream/GetAllStreams
        [HttpGet]
        [Route("GetAllStreams")]
        public IActionResult GetAllStreams()
        {
            try
            {
                // Fetch all live streams saved as HLS (M3U8 playlists)
                var streams = Directory.GetFiles(_liveStreamPath, "*.m3u8");
                var streamList = streams.Select(Path.GetFileName).ToList();

                return Ok(streamList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching live streams: {0}", ex.Message);
                return StatusCode(500, "Error fetching live streams");
            }
        }

        // GET: api/livestream/{id}
        [HttpGet("{id}")]
        public IActionResult GetStream(string id)
        {
            try
            {
                var streamPath = Path.Combine(_liveStreamPath, id + ".m3u8");

                if (!System.IO.File.Exists(streamPath))
                {
                    return NotFound("Stream not found");
                }

                return PhysicalFile(streamPath, "application/vnd.apple.mpegurl");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching stream: {0}", ex.Message);
                return StatusCode(500, "Error fetching stream");
            }
        }

        // POST: api/livestream/StartLiveStream
        [HttpPost]
        [Route("StartLiveStream")]
        public IActionResult StartLiveStream(string streamKey)
        {
            try
            {
                // Example: Start FFmpeg command to transcode and serve the live stream
                var outputStream = Path.Combine(_liveStreamPath, streamKey + ".m3u8");

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = $"-i rtmp://localhost/live/{streamKey} -c:v libx264 -c:a aac -f hls -hls_time 2 -hls_list_size 10 {outputStream}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();
                _logger.LogInformation($"Started live stream for stream key: {streamKey}");

                return Ok("Live stream started");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error starting live stream: {0}", ex.Message);
                return StatusCode(500, "Error starting live stream");
            }
        }

        // POST: api/livestream/StopLiveStream
        [HttpPost]
        [Route("StopLiveStream")]
        public IActionResult StopLiveStream(string streamKey)
        {
            try
            {
                // Example: Kill FFmpeg process or stop stream (this could be more complex depending on how streams are handled)
                // You could track the process ID when starting FFmpeg and then kill it using the PID here

// For demo purposes, assume we are stopping by identifying the streamKey
                var streamPath = Path.Combine(_liveStreamPath, streamKey + ".m3u8");
                if (System.IO.File.Exists(streamPath))
                {
                    System.IO.File.Delete(streamPath);
                }

                _logger.LogInformation($"Stopped live stream for stream key: {streamKey}");
                return Ok("Live stream stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error stopping live stream: {0}", ex.Message);
                return StatusCode(500, "Error stopping live stream");
            }
        }

        // POST: api/livestream/SaveStream
        [HttpPost]
        [Route("SaveStream")]
        public IActionResult SaveStream(string streamKey)
        {
            try
            {
                // Move or copy the live stream to the archive folder for later restreaming
                var sourceStream = Path.Combine(_liveStreamPath, streamKey + ".m3u8");
                var destinationStream = Path.Combine(_archivePath, streamKey + ".m3u8");

                if (System.IO.File.Exists(sourceStream))
                {
                    System.IO.File.Copy(sourceStream, destinationStream, true);
                    _logger.LogInformation($"Saved stream {streamKey} to archive");
                    return Ok("Stream saved to archive");
                }
                else
                {
                    return NotFound("Live stream not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving stream: {0}", ex.Message);
                return StatusCode(500, "Error saving stream");
            }
        }

        // GET: api/livestream/GetArchivedStreams
        [HttpGet]
        [Route("GetArchivedStreams")]
        public IActionResult GetArchivedStreams()
        {
            try
            {
                // Fetch archived streams saved as HLS (M3U8)
                var archivedStreams = Directory.GetFiles(_archivePath, "*.m3u8");
                var streamList = archivedStreams.Select(Path.GetFileName).ToList();

                return Ok(streamList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching archived streams: {0}", ex.Message);
                return StatusCode(500, "Error fetching archived streams");
            }
        }
    }
}

