import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Video,
  Play,
  Pause,
  Sliders,
  Type,
  Palette,
  Sparkles,
  Download,
  Share2,
  Check,
  RefreshCw,
  Clock,
  Layers,
  Crop,
  ShieldCheck,
  Coins
} from 'lucide-react';
import apiClient from '../api/client';
import { useAuth } from '../context/AuthContext';
import { RightsModal } from '../components/RightsModal';

export const EditorPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const clipId = searchParams.get('clipId');
  const { refreshWallet, organization } = useAuth();

  const [clip, setClip] = useState<any>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [saving, setSaving] = useState<boolean>(false);
  const [rendering, setRendering] = useState<boolean>(false);
  const [renderJob, setRenderJob] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  // Playback preview state
  const [isPlaying, setIsPlaying] = useState<boolean>(false);
  const [currentTime, setCurrentTime] = useState<number>(0);

  // Editor states
  const [startTime, setStartTime] = useState<number>(0);
  const [endTime, setEndTime] = useState<number>(30);
  const [aspectRatio, setAspectRatio] = useState<string>('9:16');
  const [captionStyle, setCaptionStyle] = useState<string>('TIKTOK_POP');
  const [fontName, setFontName] = useState<string>('Inter');
  const [fontSize, setFontSize] = useState<number>(42);
  const [primaryColor, setPrimaryColor] = useState<string>('#FFFFFF');
  const [highlightColor, setHighlightColor] = useState<string>('#FACC15');
  const [showProgressBar, setShowProgressBar] = useState<boolean>(true);

  // AI Copy states
  const [title, setTitle] = useState<string>('');
  const [hook, setHook] = useState<string>('');
  const [caption, setCaption] = useState<string>('');
  const [hashtags, setHashtags] = useState<string>('');
  const [cta, setCta] = useState<string>('');

  // Rights gate modal
  const [showRightsModal, setShowRightsModal] = useState<boolean>(false);

  const fetchClip = async () => {
    if (!clipId) return;
    setLoading(true);
    try {
      const res = await apiClient.get(`/api/v1/clips/${clipId}`);
      const data = res.data?.data;
      if (data) {
        setClip(data);
        setStartTime(data.startTime);
        setEndTime(data.endTime);
        setAspectRatio(data.aspectRatio || '9:16');
        setCaptionStyle(data.captionStyle || 'TIKTOK_POP');
        setFontName(data.fontName || 'Inter');
        setFontSize(data.fontSize || 42);
        setPrimaryColor(data.primaryColorHex || '#FFFFFF');
        setHighlightColor(data.highlightColorHex || '#FACC15');
        setShowProgressBar(data.showProgressBar ?? true);
        setTitle(data.title || '');
        setHook(data.hook || '');
        setCaption(data.caption || '');
        setCta(data.callToAction || '');

        try {
          const tags = JSON.parse(data.hashtagsJson);
          setHashtags(Array.isArray(tags) ? tags.join(' ') : '');
        } catch {
          setHashtags(data.hashtagsJson || '');
        }
      }
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to load clip.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (clipId) {
      fetchClip();
    }
  }, [clipId]);

  // Video preview scrub timer simulation
  useEffect(() => {
    let interval: any;
    if (isPlaying) {
      interval = setInterval(() => {
        setCurrentTime((prev) => {
          if (prev >= endTime - startTime) {
            return 0;
          }
          return prev + 0.1;
        });
      }, 100);
    }
    return () => clearInterval(interval);
  }, [isPlaying, startTime, endTime]);

  const handleSaveEdits = async () => {
    if (!clipId) return;
    setSaving(true);
    setError(null);
    try {
      const tagsArray = hashtags
        .split(' ')
        .map((t) => t.trim())
        .filter((t) => t.length > 0);

      const res = await apiClient.put(`/api/v1/clips/${clipId}`, {
        startTime,
        endTime,
        aspectRatio,
        captionStyle,
        fontName,
        fontSize,
        primaryColorHex: primaryColor,
        highlightColorHex: highlightColor,
        backgroundColorHex: '#000000',
        showProgressBar,
        captionsJson: JSON.stringify([{ start: startTime, end: endTime, text: hook }]),
        title,
        hook,
        caption,
        description: caption,
        hashtagsJson: JSON.stringify(tagsArray),
        callToAction: cta,
      });

      if (res.data?.data) {
        setClip(res.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to save changes.');
    } finally {
      setSaving(false);
    }
  };

  const handleQueueRender = async () => {
    if (!clipId) return;
    setRendering(true);
    setError(null);
    try {
      await handleSaveEdits();
      const res = await apiClient.post(`/api/v1/clips/${clipId}/render`);
      setRenderJob(res.data?.data);
      await refreshWallet();

      // Poll job progress
      const pollInterval = setInterval(async () => {
        try {
          const jobRes = await apiClient.get(`/api/v1/jobs/${res.data?.data?.id}`);
          const jobData = jobRes.data?.data;
          setRenderJob(jobData);
          if (jobData?.status === 'COMPLETED' || jobData?.status === 'FAILED') {
            clearInterval(pollInterval);
            setRendering(false);
            await fetchClip();
            await refreshWallet();
          }
        } catch {
          clearInterval(pollInterval);
          setRendering(false);
        }
      }, 1500);
    } catch (err: any) {
      setRendering(false);
      if (err.response?.data?.error?.code === 'UNAUTHORIZED_MEDIA') {
        setShowRightsModal(true);
      } else {
        setError(err.response?.data?.error?.message || 'Failed to queue render.');
      }
    }
  };

  if (loading) {
    return <div className="p-12 text-center text-sm text-slate-400">Loading clip editor...</div>;
  }

  const duration = Math.max(1, Math.round(endTime - startTime));

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-800">
        <div>
          <div className="flex items-center space-x-2 text-xs text-purple-400 font-semibold uppercase tracking-wider">
            <Video className="w-4 h-4" />
            <span>Short-Form Clip Studio</span>
          </div>
          <h1 className="text-xl sm:text-2xl font-bold text-white truncate max-w-lg">
            {title || 'Clip Customization Studio'}
          </h1>
        </div>

        <div className="flex items-center space-x-3">
          <button
            onClick={handleSaveEdits}
            disabled={saving}
            className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold rounded-xl border border-slate-700 transition"
          >
            {saving ? 'Saving...' : 'Save Draft'}
          </button>
          <button
            onClick={handleQueueRender}
            disabled={rendering}
            className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold text-xs px-5 py-2.5 rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center space-x-2"
          >
            <Sparkles className="w-4 h-4" />
            <span>{rendering ? 'Rendering Server-Side...' : 'Render 1080p Clip'}</span>
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-950/30 border border-rose-900/50 rounded-2xl text-rose-300 text-xs flex items-center justify-between">
          <span>{error}</span>
          {error.includes('UNAUTHORIZED_MEDIA') && (
            <button
              onClick={() => setShowRightsModal(true)}
              className="px-3 py-1 bg-rose-600 text-white font-bold rounded-lg ml-3 text-[11px]"
            >
              Verify Rights
            </button>
          )}
        </div>
      )}

      {/* Render Progress Bar */}
      {rendering && renderJob && (
        <div className="bg-purple-950/30 border border-purple-800/40 p-4 rounded-2xl space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-semibold text-purple-300">{renderJob.currentStep || 'Processing clip...'}</span>
            <span className="font-bold text-white">{renderJob.progressPercentage}%</span>
          </div>
          <div className="w-full bg-slate-900 h-2 rounded-full overflow-hidden">
            <div
              className="bg-purple-500 h-full transition-all duration-300"
              style={{ width: `${renderJob.progressPercentage}%` }}
            />
          </div>
        </div>
      )}

      {/* Editor Grid: Left = 9:16 Video Preview, Right = Customization Panels */}
      <div className="grid lg:grid-cols-12 gap-8 items-start">
        {/* Left: Video Preview Simulator */}
        <div className="lg:col-span-5 flex flex-col items-center space-y-4">
          <div
            className={`relative rounded-3xl overflow-hidden border border-slate-800 bg-slate-950 shadow-2xl flex items-center justify-center transition-all ${
              aspectRatio === '9:16'
                ? 'w-[320px] h-[568px]'
                : aspectRatio === '1:1'
                ? 'w-[360px] h-[360px]'
                : 'w-[480px] h-[270px]'
            }`}
          >
            {/* Background color / media */}
            <div className="absolute inset-0 bg-gradient-to-b from-slate-900 via-slate-950 to-black opacity-90" />

            {/* Video content representation */}
            <div className="relative z-10 p-6 text-center space-y-4 flex flex-col justify-between h-full py-12">
              {/* Free tier watermark */}
              {clip?.hasWatermark && (
                <div className="self-end px-2.5 py-1 rounded bg-black/60 border border-white/20 text-[10px] text-white/80 font-bold uppercase tracking-wider backdrop-blur-sm">
                  SignalCut Free
                </div>
              )}

              <div className="my-auto space-y-3">
                {/* Dynamic Captions overlay styled */}
                <div
                  className={`inline-block px-4 py-2 rounded-xl text-lg font-black tracking-tight shadow-2xl transition ${
                    captionStyle === 'TIKTOK_POP'
                      ? 'bg-black/75 uppercase text-yellow-400 border border-yellow-400/30'
                      : captionStyle === 'KARAOKE'
                      ? 'bg-purple-900/80 text-white'
                      : captionStyle === 'MINIMALIST'
                      ? 'text-white drop-shadow-md'
                      : 'bg-black/90 text-white'
                  }`}
                  style={{ color: primaryColor, fontFamily: fontName }}
                >
                  {hook || 'High-signal moment hook text appears here...'}
                </div>
              </div>

              {/* Progress bar overlay */}
              {showProgressBar && (
                <div className="w-full bg-white/20 h-1.5 rounded-full overflow-hidden">
                  <div
                    className="h-full transition-all"
                    style={{
                      backgroundColor: highlightColor,
                      width: `${((currentTime % duration) / duration) * 100}%`,
                    }}
                  />
                </div>
              )}
            </div>

            {/* Rendered Video Link indicator if completed */}
            {clip?.renderStatus === 'COMPLETED' && clip?.renderedVideoUrl && (
              <div className="absolute top-4 left-4 z-20 bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-2 py-0.5 rounded text-[10px] font-bold flex items-center space-x-1">
                <Check className="w-3 h-3" />
                <span>Render Ready</span>
              </div>
            )}
          </div>

          {/* Playback & Scrub Controls */}
          <div className="w-full max-w-[320px] bg-slate-900/90 border border-slate-800 rounded-2xl p-3 flex items-center space-x-3 text-xs">
            <button
              onClick={() => setIsPlaying(!isPlaying)}
              className="p-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white transition"
            >
              {isPlaying ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
            </button>
            <div className="flex-1 space-y-1">
              <input
                type="range"
                min="0"
                max={duration}
                step="0.1"
                value={currentTime}
                onChange={(e) => setCurrentTime(parseFloat(e.target.value))}
                className="w-full accent-purple-500"
              />
              <div className="flex justify-between text-[10px] text-slate-400">
                <span>{currentTime.toFixed(1)}s</span>
                <span>{duration}s</span>
              </div>
            </div>
          </div>
        </div>

        {/* Right: Customization & Copywriting Panels */}
        <div className="lg:col-span-7 space-y-6">
          {/* Trims & Formatting */}
          <div className="bg-slate-900 border border-slate-800 rounded-3xl p-5 space-y-4">
            <h3 className="text-sm font-bold text-white flex items-center space-x-2">
              <Crop className="w-4 h-4 text-purple-400" />
              <span>Trim & Layout</span>
            </h3>

            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">
                  Start Time: {startTime}s
                </label>
                <input
                  type="range"
                  min="0"
                  max={Math.max(0, endTime - 5)}
                  value={startTime}
                  onChange={(e) => setStartTime(parseFloat(e.target.value))}
                  className="w-full accent-purple-500"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">
                  End Time: {endTime}s (Duration: {duration}s)
                </label>
                <input
                  type="range"
                  min={startTime + 5}
                  max={Math.max(120, endTime + 30)}
                  value={endTime}
                  onChange={(e) => setEndTime(parseFloat(e.target.value))}
                  className="w-full accent-purple-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-400 mb-2">Aspect Ratio</label>
              <div className="grid grid-cols-3 gap-2 text-xs">
                {['9:16', '1:1', '16:9'].map((ratio) => (
                  <button
                    key={ratio}
                    onClick={() => setAspectRatio(ratio)}
                    className={`py-2 rounded-xl border font-semibold transition ${
                      aspectRatio === ratio
                        ? 'border-purple-500 bg-purple-500/10 text-white'
                        : 'border-slate-800 bg-slate-800/40 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    {ratio} {ratio === '9:16' && '(Shorts/Reels)'}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Caption Style & Brand Kit */}
          <div className="bg-slate-900 border border-slate-800 rounded-3xl p-5 space-y-4">
            <h3 className="text-sm font-bold text-white flex items-center space-x-2">
              <Palette className="w-4 h-4 text-purple-400" />
              <span>Captions & Brand Styling</span>
            </h3>

            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5">Caption Style</label>
                <select
                  value={captionStyle}
                  onChange={(e) => setCaptionStyle(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none"
                >
                  <option value="TIKTOK_POP">TikTok Pop (Bold High-Contrast)</option>
                  <option value="KARAOKE">Karaoke Highlight</option>
                  <option value="CLASSIC">Classic Boxed Subtitles</option>
                  <option value="MINIMALIST">Clean Minimalist</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5">Font Family</label>
                <select
                  value={fontName}
                  onChange={(e) => setFontName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none"
                >
                  <option value="Inter">Inter (Modern Clean)</option>
                  <option value="Montserrat">Montserrat (Geometric Bold)</option>
                  <option value="Bebas Neue">Bebas Neue (Impact Condensed)</option>
                  <option value="Poppins">Poppins (Friendly Bold)</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5">Primary Text Color</label>
                <div className="flex items-center space-x-2">
                  <input
                    type="color"
                    value={primaryColor}
                    onChange={(e) => setPrimaryColor(e.target.value)}
                    className="w-8 h-8 rounded-lg bg-transparent cursor-pointer border border-slate-700"
                  />
                  <input
                    type="text"
                    value={primaryColor}
                    onChange={(e) => setPrimaryColor(e.target.value)}
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs text-white"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5">Highlight Color</label>
                <div className="flex items-center space-x-2">
                  <input
                    type="color"
                    value={highlightColor}
                    onChange={(e) => setHighlightColor(e.target.value)}
                    className="w-8 h-8 rounded-lg bg-transparent cursor-pointer border border-slate-700"
                  />
                  <input
                    type="text"
                    value={highlightColor}
                    onChange={(e) => setHighlightColor(e.target.value)}
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs text-white"
                  />
                </div>
              </div>
            </div>

            <label className="flex items-center space-x-2 cursor-pointer pt-1">
              <input
                type="checkbox"
                checked={showProgressBar}
                onChange={(e) => setShowProgressBar(e.target.checked)}
                className="rounded border-slate-700 text-purple-600 focus:ring-purple-500 bg-slate-950"
              />
              <span className="text-xs text-slate-300 font-medium">Show video duration progress bar</span>
            </label>
          </div>

          {/* AI Copywriting Suite */}
          <div className="bg-slate-900 border border-slate-800 rounded-3xl p-5 space-y-4">
            <h3 className="text-sm font-bold text-white flex items-center space-x-2">
              <Sparkles className="w-4 h-4 text-purple-400" />
              <span>Publishing Metadata & Copy</span>
            </h3>

            <div className="space-y-3">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Hook / First 3 Seconds Text</label>
                <input
                  type="text"
                  value={hook}
                  onChange={(e) => setHook(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-purple-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Video Title</label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-purple-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Caption / Description</label>
                <textarea
                  rows={3}
                  value={caption}
                  onChange={(e) => setCaption(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-purple-500"
                />
              </div>

              <div className="grid sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 mb-1">Hashtags</label>
                  <input
                    type="text"
                    value={hashtags}
                    onChange={(e) => setHashtags(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 mb-1">Call to Action (CTA)</label>
                  <input
                    type="text"
                    value={cta}
                    onChange={(e) => setCta(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-purple-500"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Rights Gate Modal */}
      {clip && (
        <RightsModal
          sourceId={clip.momentId}
          sourceTitle={clip.title}
          isOpen={showRightsModal}
          onClose={() => setShowRightsModal(false)}
          onConfirmed={handleQueueRender}
        />
      )}
    </div>
  );
};
