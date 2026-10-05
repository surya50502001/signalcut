import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Sparkles, ShieldCheck, ExternalLink, Play, Filter, RefreshCw, UploadCloud, Clock } from 'lucide-react';
import apiClient from '../api/client';
import { SourceItem } from './SearchPage';
import { RightsModal } from '../components/RightsModal';
import { UploadMediaModal } from '../components/UploadMediaModal';

export const DiscoveriesPage: React.FC = () => {
  const navigate = useNavigate();
  const [sources, setSources] = useState<SourceItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [filterRights, setFilterRights] = useState<string>('ALL');
  const [selectedSourceForRights, setSelectedSourceForRights] = useState<SourceItem | null>(null);
  const [selectedSourceForUpload, setSelectedSourceForUpload] = useState<SourceItem | null>(null);

  const fetchSources = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get('/api/v1/sources');
      setSources(res.data?.data || []);
    } catch {
      // fallback sample
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSources();
  }, []);

  const filteredSources = sources.filter((s) => {
    if (filterRights === 'AUTHORIZED') return s.isAuthorizedForGeneration;
    if (filterRights === 'DISCOVERY_ONLY') return !s.isAuthorizedForGeneration;
    return true;
  });

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
            <Sparkles className="w-7 h-7 text-purple-400" />
            <span>Discovered Sources</span>
          </h1>
          <p className="text-sm text-slate-400">
            Catalog of sources uncovered across your workspace topic searches.
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <div className="flex items-center bg-slate-900 border border-slate-800 rounded-xl p-1 text-xs">
            {['ALL', 'AUTHORIZED', 'DISCOVERY_ONLY'].map((tab) => (
              <button
                key={tab}
                onClick={() => setFilterRights(tab)}
                className={`px-3 py-1.5 rounded-lg font-medium transition ${
                  filterRights === tab
                    ? 'bg-purple-600 text-white font-semibold'
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                {tab.replace('_', ' ')}
              </button>
            ))}
          </div>

          <button
            onClick={fetchSources}
            className="p-2.5 bg-slate-900 border border-slate-800 hover:bg-slate-800 text-slate-300 rounded-xl transition"
            title="Refresh"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
        </div>
      </div>

      {loading ? (
        <div className="py-20 text-center text-sm text-slate-400">Loading discoveries...</div>
      ) : filteredSources.length === 0 ? (
        <div className="p-12 text-center bg-slate-900/50 border border-slate-800 rounded-3xl space-y-4">
          <div className="font-semibold text-slate-200">No sources found</div>
          <p className="text-xs text-slate-400 max-w-sm mx-auto">
            Use the Topic Search to discover relevant conversations and videos.
          </p>
          <button
            onClick={() => navigate('/search')}
            className="px-5 py-2.5 bg-purple-600 hover:bg-purple-500 text-white text-xs font-semibold rounded-xl shadow-lg shadow-purple-900/30 transition"
          >
            Start Topic Search
          </button>
        </div>
      ) : (
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredSources.map((source) => (
            <div
              key={source.id}
              className="bg-slate-900/80 border border-slate-800 rounded-3xl p-5 flex flex-col justify-between space-y-4 shadow-lg hover:border-slate-700/80 transition"
            >
              <div className="space-y-2.5">
                <div className="flex items-center justify-between text-xs">
                  <div className="flex items-center space-x-1.5">
                    <span className="font-semibold px-2 py-0.5 rounded bg-slate-800 text-purple-400 border border-slate-700">
                      {source.provider}
                    </span>
                    {source.episode && (
                      <span className="font-semibold px-2 py-0.5 rounded bg-indigo-950/60 text-indigo-300 border border-indigo-800/40 text-[10px]">
                        {source.episode}
                      </span>
                    )}
                  </div>
                  {source.isAuthorizedForGeneration ? (
                    <span className="text-emerald-400 font-bold flex items-center space-x-1 text-[11px]">
                      <ShieldCheck className="w-3.5 h-3.5" />
                      <span>Authorized</span>
                    </span>
                  ) : (
                    <span className="text-amber-400 text-[11px] font-medium">Discovery Only</span>
                  )}
                </div>

                <h3 className="font-bold text-sm text-slate-100 line-clamp-2 leading-snug">
                  {source.title}
                </h3>
                <p className="text-xs text-slate-400 line-clamp-2 leading-relaxed">
                  {source.description}
                </p>

                <div className="text-[11px] text-slate-400 pt-1 flex items-center justify-between">
                  <span>By {source.creator}</span>
                  <div className="flex items-center space-x-2">
                    {source.bestMomentTimestamp && (
                      <span className="inline-flex items-center space-x-1 text-purple-300 font-medium bg-purple-950/40 px-1.5 py-0.5 rounded border border-purple-800/40 text-[10px]">
                        <Clock className="w-2.5 h-2.5 text-purple-400" />
                        <span>{source.bestMomentTimestamp}</span>
                      </span>
                    )}
                    <span className="font-semibold text-emerald-400">
                      {Math.round(source.relevanceScore * 100)}% match
                    </span>
                  </div>
                </div>

                {!source.isAuthorizedForGeneration && (
                  <div className="bg-amber-950/20 border border-amber-900/30 rounded-xl p-2.5 text-[11px] text-amber-300/90 leading-relaxed mt-1">
                    Discovery only — SignalCut cannot retrieve this media. Upload a copy you have permission to use to create clips.
                  </div>
                )}
              </div>

              <div className="pt-3 border-t border-slate-800 flex items-center justify-between">
                <a
                  href={source.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-slate-400 hover:text-white flex items-center space-x-1"
                >
                  <span>Link</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </a>

                <div className="flex items-center space-x-2">
                  {source.isAuthorizedForGeneration ? (
                    <button
                      onClick={() => navigate(`/moments?sourceId=${source.id}`)}
                      className="px-3 py-1.5 text-xs font-semibold text-white bg-purple-600 hover:bg-purple-500 rounded-xl transition flex items-center space-x-1 shadow-md shadow-purple-900/30"
                    >
                      <Sparkles className="w-3 h-3" />
                      <span>Create Clip</span>
                    </button>
                  ) : (
                    <button
                      onClick={() => setSelectedSourceForUpload(source)}
                      className="px-3 py-1.5 text-xs font-semibold text-amber-200 bg-amber-500/20 hover:bg-amber-500/30 border border-amber-500/30 rounded-xl transition flex items-center space-x-1"
                    >
                      <UploadCloud className="w-3 h-3 text-amber-400" />
                      <span>Create Clip (Upload Media)</span>
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedSourceForUpload && (
        <UploadMediaModal
          sourceId={selectedSourceForUpload.id}
          sourceTitle={selectedSourceForUpload.title}
          isOpen={!!selectedSourceForUpload}
          onClose={() => setSelectedSourceForUpload(null)}
          onAuthorized={() => {
            fetchSources();
            navigate(`/moments?sourceId=${selectedSourceForUpload.id}`);
          }}
        />
      )}

      {selectedSourceForRights && (
        <RightsModal
          sourceId={selectedSourceForRights.id}
          sourceTitle={selectedSourceForRights.title}
          isOpen={!!selectedSourceForRights}
          onClose={() => setSelectedSourceForRights(null)}
          onConfirmed={fetchSources}
        />
      )}
    </div>
  );
};
