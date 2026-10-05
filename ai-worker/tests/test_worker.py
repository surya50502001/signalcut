from fastapi.testclient import TestClient
import sys
import os

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "app")))
from main import app

client = TestClient(app)

def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "Healthy"
    assert "ffmpegInstalled" in data

def test_transcription():
    response = client.post("/api/v1/transcription", json={
        "sourceUrl": "https://example.com/audio.mp3",
        "language": "en"
    })
    assert response.status_code == 200
    data = response.json()
    assert "fullText" in data
    assert len(data["chunks"]) > 0

def test_moment_detection():
    response = client.post("/api/v1/moments", json={
        "transcriptText": "Sample text regarding AI automation and workflow pipelines.",
        "topicQuery": "AI automation",
        "objective": "Educational"
    })
    assert response.status_code == 200
    data = response.json()
    assert len(data) >= 1
    assert "clipScore" in data[0]
    assert "suggestedHook" in data[0]

def test_video_rendering_mock():
    response = client.post("/api/v1/render", json={
        "clipId": "test-clip-123",
        "sourceVideoUrl": "https://example.com/video.mp4",
        "startTime": 0.0,
        "endTime": 15.0,
        "aspectRatio": "9:16",
        "width": 1080,
        "height": 1920,
        "hasWatermark": True,
        "captionStyle": "TIKTOK_POP",
        "captions": [
            {
                "chunkIndex": 0,
                "startTime": 0.0,
                "endTime": 5.0,
                "text": "SignalCut AI Test Clip"
            }
        ]
    })
    assert response.status_code == 200
    data = response.json()
    assert data["success"] is True
    assert "storageUrl" in data

def test_youtube_search():
    response = client.get("/api/v1/search/youtube?query=AI+agents&limit=2")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    assert len(data) >= 1
    assert "url" in data[0]
    assert "title" in data[0]
    assert data[0]["provider"] == "YouTube"

