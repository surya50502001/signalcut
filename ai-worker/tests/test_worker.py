from fastapi.testclient import TestClient
import sys
import os
import tempfile
import subprocess
import shutil

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "app")))
from main import app, FFMPEG_BIN

client = TestClient(app)
client.headers.update({"X-API-Key": "dev_ai_worker_internal_secret_key_2025"})

def test_health_check():
    # Health check must be publicly accessible without authentication
    unauth_client = TestClient(app)
    response = unauth_client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "Healthy"
    assert "ffmpegInstalled" in data

def test_unauthenticated_request_returns_401():
    unauth_client = TestClient(app)
    endpoints = [
        ("GET", "/api/v1/search/youtube?query=tech"),
        ("POST", "/api/v1/transcription"),
        ("POST", "/api/v1/moments"),
        ("POST", "/api/v1/render")
    ]
    for method, path in endpoints:
        if method == "GET":
            resp = unauth_client.get(path)
        else:
            resp = unauth_client.post(path, json={})
        assert resp.status_code == 401
        assert "Unauthorized" in resp.json()["detail"]

def test_transcription_rejects_arbitrary_remote_urls():
    # Arbitrary remote URLs (like YouTube, Vimeo, podcasts, external mp3s) must be rejected with 403
    for arbitrary_url in [
        "https://example.com/unauthorized_audio.mp3",
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "https://podcasts.example.com/episode.mp3"
    ]:
        response = client.post("/api/v1/transcription", json={
            "sourceUrl": arbitrary_url,
            "language": "en"
        })
        assert response.status_code == 403
        data = response.json()
        assert "detail" in data
        assert "not permitted" in data["detail"].lower() or "blocked" in data["detail"].lower()

def test_transcription_clean_failure_for_silent_local_media():
    # Authorized local file with no speech or missing transcript must fail cleanly with 422
    with tempfile.NamedTemporaryFile(suffix=".mp4", delete=False) as f:
        silent_video = f.name
    try:
        cmd = [
            FFMPEG_BIN, "-y",
            "-f", "lavfi", "-i", "color=c=black:s=320x240:d=1",
            "-f", "lavfi", "-i", "anullsrc=r=44100:cl=mono",
            "-c:v", "libx264", "-c:a", "aac", "-t", "1",
            silent_video
        ]
        subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        resp = client.post("/api/v1/transcription", json={"sourceUrl": silent_video, "language": "en"})
        assert resp.status_code == 422
        assert "Real media transcript or caption data is required" in resp.json()["detail"]
    finally:
        if os.path.exists(silent_video):
            os.remove(silent_video)

def test_moment_detection_with_real_transcript():
    # Realistic transcript containing over 15 words
    real_transcript = (
        "Welcome back to the show. Today we are examining artificial intelligence workflows and how "
        "autonomous developer agents change software engineering productivity forever. The primary "
        "bottleneck is no longer manual generation, but curation and high signal distribution."
    )
    response = client.post("/api/v1/moments", json={
        "transcriptText": real_transcript,
        "topicQuery": "AI automation",
        "objective": "Educational"
    })
    assert response.status_code == 200
    data = response.json()
    assert len(data) >= 1
    assert "clipScore" in data[0]
    assert "suggestedHook" in data[0]

def test_moment_detection_empty_or_short_fails():
    # Empty transcript must return 400
    empty_resp = client.post("/api/v1/moments", json={
        "transcriptText": "",
        "topicQuery": "AI",
        "objective": "Educational"
    })
    assert empty_resp.status_code == 400

    # Short transcript under 15 words must return 422
    short_resp = client.post("/api/v1/moments", json={
        "transcriptText": "Too short snippet.",
        "topicQuery": "AI",
        "objective": "Educational"
    })
    assert short_resp.status_code == 422

def test_render_blocks_youtube_url():
    # Rendering directly from YouTube or scraping URLs must be blocked with 403
    response = client.post("/api/v1/render", json={
        "clipId": "test-clip-yt",
        "sourceVideoUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "startTime": 0.0,
        "endTime": 15.0
    })
    assert response.status_code == 403
    data = response.json()
    assert "not permitted" in data["detail"].lower()

def test_video_rendering_with_local_file():
    # Create a small valid test mp4 video using ffmpeg
    with tempfile.NamedTemporaryFile(suffix=".mp4", delete=False) as f:
        test_video_path = f.name

    try:
        cmd = [
            FFMPEG_BIN, "-y",
            "-f", "lavfi", "-i", "color=c=black:s=1280x720:d=3",
            "-f", "lavfi", "-i", "sine=f=1000:d=3",
            "-c:v", "libx264", "-c:a", "aac",
            test_video_path
        ]
        subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        response = client.post("/api/v1/render", json={
            "clipId": "test-local-clip",
            "sourceVideoUrl": test_video_path,
            "startTime": 0.0,
            "endTime": 2.0,
            "aspectRatio": "9:16",
            "width": 1080,
            "height": 1920,
            "hasWatermark": False,
            "captionStyle": "TIKTOK_POP",
            "captions": [
                {
                    "chunkIndex": 0,
                    "startTime": 0.0,
                    "endTime": 2.0,
                    "text": "SignalCut Local Media Test"
                }
            ]
        })
        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        assert "storageUrl" in data
    finally:
        if os.path.exists(test_video_path):
            os.remove(test_video_path)

def test_youtube_search():
    response = client.get("/api/v1/search/youtube?query=AI+agents&limit=2")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    assert len(data) >= 1
    assert "url" in data[0]
    assert "title" in data[0]
    assert data[0]["provider"] == "YouTube"
    assert data[0]["rightsStatus"] == "DISCOVERY_ONLY"
    assert data[0]["isAuthorizedForGeneration"] is False

def test_transcription_ssrf_blocked():
    # Loopback, private networks, and cloud metadata must be rejected
    for malicious_url in [
        "http://127.0.0.1:8080/admin",
        "http://localhost:5000/secret",
        "http://169.254.169.254/latest/meta-data/",
        "http://10.0.0.1/internal-audio.mp3"
    ]:
        resp = client.post("/api/v1/transcription", json={"sourceUrl": malicious_url, "language": "en"})
        assert resp.status_code in (400, 403)
        detail = resp.json()["detail"].lower()
        assert "blocked" in detail or "not permitted" in detail

def test_render_rejects_arbitrary_remote_urls():
    for arbitrary_url in [
        "https://example.com/arbitrary_video.mp4",
        "https://external-site.org/stream.m3u8"
    ]:
        resp = client.post("/api/v1/render", json={
            "clipId": "render-arb-url",
            "sourceVideoUrl": arbitrary_url,
            "startTime": 0.0,
            "endTime": 5.0
        })
        assert resp.status_code == 403
        assert "not permitted" in resp.json()["detail"].lower()

def test_render_blocks_other_external_platforms():
    # Platforms like Vimeo, Dailymotion, Twitch must also be blocked
    for platform_url in [
        "https://vimeo.com/12345678",
        "https://www.dailymotion.com/video/x7tgad0",
        "https://twitch.tv/videos/123456"
    ]:
        resp = client.post("/api/v1/render", json={
            "clipId": "ext-test",
            "sourceVideoUrl": platform_url,
            "startTime": 0.0,
            "endTime": 10.0
        })
        assert resp.status_code == 403
        assert "not permitted" in resp.json()["detail"].lower()

def test_render_missing_media_fails_cleanly_without_synthetic_video():
    # If media file does not exist, worker must fail with 422 and NOT generate a fake color-bar fallback
    resp = client.post("/api/v1/render", json={
        "clipId": "missing-media-test",
        "sourceVideoUrl": "/non/existent/path/video.mp4",
        "startTime": 0.0,
        "endTime": 10.0
    })
    assert resp.status_code == 422
    assert "not found or could not be sliced" in resp.json()["detail"]


