import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Sparkles,
  Flame,
  ArrowRight,
  ShieldAlert,
  Sliders,
  Copy,
  Check,
  Play,
  RotateCw,
  Lightbulb,
  AlertTriangle,
  UploadCloud
} from 'lucide-react';
import apiClient from '../api/client';
import { RightsModal } from '../components/RightsModal';
import { UploadMediaModal } from '../components/UploadMediaModal';
import { useAuth } from '../context/AuthContext';

export interface MomentDto {
  id: string;
  sourceId: string;
  startTime: number;
  endTime: number;
  durationSeconds: number;
  transcriptSnippet: string;
  reason: string;
  topicRelevance: number;
  hookStrength: number;
  informationDensity: number;
  clipScore: number;
  speaker: string;
  objective: string;
  suggestedHook: string;
  suggestedTitle: string;
  suggestedCaption: string;
  suggestedDescription: string;
  suggestedHashtags: string[];
  suggestedCta: string;
}

export const MomentsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const sourceId = searchParams.get('sourceId');
  const { refreshWallet } = useAuth();

  const [moments, setMoments] = useState<MomentDto[]>([]);
  const [source, setSource] = useState<any>(null);
  const [objective, setObjective] = useState<string>('Educational');
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  // Rights & Upload modal
  const [showRightsModal, setShowRightsModal] = useState<boolean>(false);
  const [showUploadModal, setShowUploadModal] = useState<boolean>(false);

  const objectives = [
    'Educational',
    'Controversial',
    'Newsworthy',
    'Emotional',
    'Inspirational',
    'Technical',
    'Promotional',
  ];

  const fetchMoments = async () => {
    if (!sourceId) return;
    setLoading(true);
    setError(null);
    try {
      // Fetch source details
      const sourceRes = await apiClient.get(`/api/v1/sources/${sourceId}`);
      setSource(sourceRes.data?.data);

      // Fetch moments
      const res = await apiClient.get(`/api/v1/moments/source/${sourceId}`);
      if (res.data?.data && res.data.data.length > 0) {
        setMoments(res.data.data);
      } else {
        // Auto-detect if empty
        await handleDetectMoments();
      }
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to load moments.');
    } finally {
      setLoading(false);
    }
  };

  const handleDetectMoments = async () => {
    if (!sourceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await apiClient.post('/api/v1/moments/detect', {
        sourceId,
        objective,
      });
      setMoments(res.data?.data || []);
      await refreshWallet();
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to analyze source.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (sourceId) {
      fetchMoments();
    }
  }, [sourceId]);

  const handleCreateClip = async (moment: MomentDto) => {
    // Assert rights before navigating to editor
    if (source && !source.isAuthorizedForGeneration) {
      setShowUploadModal(true);
      return;
    }

    try {
      const res = await apiClient.post('/api/v1/clips', {
        momentId: moment.id,
        startTime: moment.startTime,
        endTime: moment.endTime,
        aspectRatio: '9:16',
        captionStyle: 'TIKTOK_POP',
      });

      if (res.data?.data) {
        navigate(`/editor?clipId=${res.data.data.id}`);
      }
    } catch (err: any) {
      if (err.response?.data?.error?.code === 'UNAUTHORIZED_MEDIA') {
        setShowUploadModal(true);
      } else {
        setError(err.response?.data?.error?.message || 'Failed to create clip.');
      }
    }
  };

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 1500);
  };

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center space-x-2 text-xs text-purple-400 font-semibold uppercase tracking-wider">
            <Sparkles className="w-4 h-4" />
            <span>AI Moment Detection</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white">
            High-Signal Moments
          </h1>
          {source && (
            <p className="text-xs text-slate-400 truncate max-w-xl">
              Source: <span className="text-slate-200 font-medium">{source.title}</span> ({source.provider})
            </p>
          )}
        </div>

        {/* Objective Selector */}
        <div className="flex items-center space-x-3">
          <div className="flex items-center space-x-2 bg-slate-900 border border-slate-800 rounded-2xl px-3 py-1.5 text-xs">
            <Sliders className="w-3.5 h-3.5 text-slate-400" />
            <span className="text-slate-400">Objective:</span>
            <select
              value={objective}
              onChange={(e) => setObjective(e.target.value)}
              className="bg-transparent text-white font-semibold focus:outline-none cursor-pointer"
            >
              {objectives.map((obj) => (
                <option key={obj} value={obj} className="bg-slate-900 text-white">
                  {obj}
                </option>
              ))}
            </select>
          </div>

          <button
            onClick={handleDetectMoments}
            disabled={loading}
            className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold text-xs px-4 py-2 rounded-xl transition flex items-center space-x-1.5 shadow-md shadow-purple-900/30"
          >
            <RotateCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Rerun Analysis</span>
          </button>
        </div>
      </div>

      {/* Discovery-Only Mandatory Banner */}
      {source && !source.isAuthorizedForGeneration && (
        <div className="bg-amber-950/25 border border-amber-900/40 rounded-2xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 text-xs">
          <div className="flex items-start space-x-3">
            <AlertTriangle className="w-5 h-5 text-amber-400 shrink-0 mt-0.5" />
            <div>
              <div className="font-bold text-amber-200">Discovery-Only Source</div>
              <p className="text-amber-300/90 leading-relaxed mt-0.5">
                This source is available for discovery, but SignalCut does not have an authorized way to retrieve the media. Upload the video/audio you have permission to use to continue.
              </p>
            </div>
          </div>
          <button
            onClick={() => setShowUploadModal(true)}
            className="px-4 py-2 bg-amber-500/20 hover:bg-amber-500/30 text-amber-200 border border-amber-500/40 rounded-xl font-semibold shrink-0 transition flex items-center space-x-1.5"
          >
            <UploadCloud className="w-4 h-4 text-amber-400" />
            <span>Upload Media</span>
          </button>
        </div>
      )}

      {error && (
        <div className="p-4 bg-rose-950/30 border border-rose-900/50 rounded-2xl text-rose-300 text-xs flex items-center justify-between">
          <span>{error}</span>
          {error.includes('UNAUTHORIZED_MEDIA') && (
            <button
              onClick={() => setShowRightsModal(true)}
              className="px-3 py-1 bg-rose-600 text-white font-bold rounded-lg ml-3 text-[11px]"
            >
              Confirm Rights
            </button>
          )}
        </div>
      )}

      {/* Moments List */}
      {loading ? (
        <div className="py-20 text-center text-sm text-slate-400 space-y-2">
          <div className="animate-pulse font-semibold text-purple-400">Analyzing transcript density...</div>
          <p className="text-xs text-slate-500">Evaluating hooks, contrarian statements, and quote density.</p>
        </div>
      ) : moments.length === 0 ? (
        <div className="p-12 text-center bg-slate-900/40 border border-slate-800 rounded-3xl space-y-3">
          <Lightbulb className="w-8 h-8 text-amber-400 mx-auto" />
          <div className="font-semibold text-slate-200">No moments generated yet</div>
          <p className="text-xs text-slate-400 max-w-sm mx-auto">
            Click "Rerun Analysis" to extract high-signal sections tailored to the {objective} objective.
          </p>
        </div>
      ) : (
        <div className="grid gap-6">
          {moments.map((m, idx) => (
            <div
              key={m.id || idx}
              className="bg-slate-900/90 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-5 hover:border-slate-700/80 transition"
            >
              {/* Header metrics bar */}
              <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-slate-800/80">
                <div className="flex items-center space-x-3">
                  <div className="flex items-center space-x-1.5 bg-gradient-to-r from-purple-600 to-indigo-600 text-white text-xs px-3 py-1 rounded-full font-extrabold shadow-sm">
                    <Flame className="w-3.5 h-3.5 text-amber-300 fill-amber-300" />
                    <span>Signal Score: {Math.round(m.clipScore)}</span>
                  </div>
                  <span className="text-xs font-semibold text-slate-400">
                    {Math.floor(m.startTime)}s - {Math.floor(m.endTime)}s ({Math.floor(m.durationSeconds)}s duration)
                  </span>
                </div>

                <div className="flex items-center space-x-4 text-xs text-slate-400">
                  <span>Hook: <b className="text-slate-200">{Math.round(m.hookStrength * 100)}%</b></span>
                  <span>Density: <b className="text-slate-200">{Math.round(m.informationDensity * 100)}%</b></span>
                  <span>Relevance: <b className="text-slate-200">{Math.round(m.topicRelevance * 100)}%</b></span>
                </div>
              </div>

              {/* Transcript & Rationale */}
              <div className="space-y-2">
                <blockquote className="text-sm sm:text-base text-slate-200 leading-relaxed font-medium bg-slate-950/60 p-4 rounded-2xl border border-slate-800/60">
                  "{m.transcriptSnippet}"
                </blockquote>
                <div className="text-xs text-slate-400 flex items-start space-x-2 pt-1">
                  <span className="font-semibold text-purple-400 shrink-0">Analysis:</span>
                  <span>{m.reason}</span>
                </div>
              </div>

              {/* AI Copy Preview */}
              <div className="grid sm:grid-cols-2 gap-4 bg-slate-800/30 border border-slate-700/40 p-4 rounded-2xl text-xs">
                <div className="space-y-1">
                  <div className="font-bold text-slate-300">Generated Hook:</div>
                  <div className="text-purple-300 font-semibold">{m.suggestedHook}</div>
                </div>
                <div className="space-y-1">
                  <div className="font-bold text-slate-300">Optimized Title:</div>
                  <div className="text-slate-200">{m.suggestedTitle}</div>
                </div>
              </div>

              {/* Bottom Actions */}
              <div className="flex items-center justify-between pt-2">
                <button
                  onClick={() => copyToClipboard(m.suggestedCaption, m.id)}
                  className="text-xs text-slate-400 hover:text-white flex items-center space-x-1.5 transition"
                >
                  {copiedId === m.id ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                  <span>{copiedId === m.id ? 'Copied Copy!' : 'Copy Caption & Hashtags'}</span>
                </button>

                <button
                  onClick={() => handleCreateClip(m)}
                  className="bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs px-5 py-2.5 rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center space-x-2"
                >
                  <span>Open in Clip Editor</span>
                  <ArrowRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Upload Media Modal for Discovery-Only Sources */}
      {source && (
        <UploadMediaModal
          sourceId={source.id}
          sourceTitle={source.title}
          isOpen={showUploadModal}
          onClose={() => setShowUploadModal(false)}
          onAuthorized={() => {
            fetchMoments();
          }}
        />
      )}

      {/* Legacy Rights Modal */}
      {source && (
        <RightsModal
          sourceId={source.id}
          sourceTitle={source.title}
          isOpen={showRightsModal}
          onClose={() => setShowRightsModal(false)}
          onConfirmed={fetchMoments}
        />
      )}
    </div>
  );
};
