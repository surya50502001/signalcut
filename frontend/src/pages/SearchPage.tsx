import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Search, Sparkles, Filter, Play, ExternalLink, ArrowRight, ShieldCheck, ShieldAlert, AlertCircle, Clock, UploadCloud } from 'lucide-react';
import apiClient from '../api/client';
import { RightsModal } from '../components/RightsModal';
import { UploadMediaModal } from '../components/UploadMediaModal';

export interface SourceItem {
  id: string;
  externalSourceId: string;
  provider: string;
  title: string;
  description: string;
  creator: string;
  url: string;
  publishedAt: string;
  durationSeconds: number;
  thumbnailUrl: string;
  language: string;
  contentType: string;
  rightsStatus: string;
  authorizationStatus: string;
  transcriptAvailability: boolean;
  isAuthorizedForGeneration: boolean;
  relevanceScore: number;
  bestMomentTimestamp?: string;
  episode?: string;
}

export const SearchPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const initialQuery = searchParams.get('q') || '';

  const [query, setQuery] = useState(initialQuery);
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<SourceItem[]>([]);
  const [parsedTopics, setParsedTopics] = useState<string[]>([]);
  const [intent, setIntent] = useState<string>('');
  const [error, setError] = useState<string | null>(null);

  // Rights & Upload Modal States
  const [selectedSourceForRights, setSelectedSourceForRights] = useState<SourceItem | null>(null);
  const [selectedSourceForUpload, setSelectedSourceForUpload] = useState<SourceItem | null>(null);

  const suggestedPrompts = [
    'What are CEOs saying about AI agents?',
    'Latest discussions about .NET 10',
    'Best arguments about remote work',
    'Tamil startup founders discussing AI',
    'Interviews about humanoid robotics',
  ];

  const handleSearch = async (targetQuery: string) => {
    if (!targetQuery.trim()) return;
    setLoading(true);
    setError(null);

    try {
      const res = await apiClient.post('/api/v1/search', {
        query: targetQuery.trim(),
        limit: 20,
      });

      if (res.data?.data) {
        setResults(res.data.data.sources || []);
        setParsedTopics(res.data.data.parsedTopics || []);
        setIntent(res.data.data.intent || '');
      }
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Search discovery request failed.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (initialQuery) {
      handleSearch(initialQuery);
    }
  }, [initialQuery]);

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}m ${secs > 0 ? secs + 's' : ''}`;
  };

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      {/* Search Header */}
      <div className="space-y-3">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
          <Search className="w-7 h-7 text-purple-400" />
          <span>Topic Discovery Engine</span>
        </h1>
        <p className="text-sm text-slate-400 max-w-3xl">
          Search across podcasts, conferences, YouTube, and connected media to discover high-signal moments without needing a video URL.
        </p>
      </div>

      {/* Natural Language Query Bar */}
      <div className="bg-slate-900 border border-slate-800 rounded-3xl p-4 sm:p-5 shadow-xl space-y-4">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleSearch(query);
          }}
          className="flex flex-col sm:flex-row gap-3"
        >
          <div className="relative flex-1">
            <Search className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
            <input
              type="text"
              placeholder="What do you want to find? (e.g. AI agents replacing software developers)"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-2xl pl-12 pr-4 py-3.5 text-sm sm:text-base text-white placeholder-slate-500 focus:outline-none focus:border-purple-500 transition"
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="bg-purple-600 hover:bg-purple-500 text-white font-semibold text-sm px-7 py-3.5 rounded-2xl shadow-lg shadow-purple-900/30 transition flex items-center justify-center space-x-2 shrink-0 disabled:opacity-50"
          >
            {loading ? (
              <span>Discovering...</span>
            ) : (
              <>
                <Sparkles className="w-4 h-4" />
                <span>Search Topic</span>
              </>
            )}
          </button>
        </form>

        {/* Suggestion Chips */}
        <div className="flex flex-wrap items-center gap-2 pt-1 text-xs">
          <span className="font-semibold text-slate-400">Suggestions:</span>
          {suggestedPrompts.map((prompt, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => {
                setQuery(prompt);
                handleSearch(prompt);
              }}
              className="bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700/60 px-3 py-1.5 rounded-xl text-slate-300 transition"
            >
              {prompt}
            </button>
          ))}
        </div>
      </div>

      {/* Query Understanding Card */}
      {parsedTopics.length > 0 && (
        <div className="p-4 bg-purple-950/20 border border-purple-800/30 rounded-2xl flex flex-wrap items-center justify-between gap-3 text-xs">
          <div className="flex items-center space-x-2">
            <span className="font-bold text-purple-400">Extracted Topics:</span>
            <div className="flex flex-wrap gap-1.5">
              {parsedTopics.map((topic, i) => (
                <span key={i} className="bg-purple-900/40 text-purple-300 px-2 py-0.5 rounded-md font-medium border border-purple-500/20">
                  {topic}
                </span>
              ))}
            </div>
          </div>
          {intent && (
            <div className="text-slate-400">
              Intent: <span className="font-semibold text-slate-200">{intent}</span>
            </div>
          )}
        </div>
      )}

      {error && (
        <div className="p-4 bg-rose-950/30 border border-rose-900/50 rounded-2xl text-rose-300 text-sm flex items-center space-x-2">
          <AlertCircle className="w-5 h-5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Discovered Sources Grid */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-white">
            Discovered Sources {results.length > 0 && `(${results.length})`}
          </h2>
          <span className="text-xs text-slate-400">Ranked by contextual relevance</span>
        </div>

        {results.length === 0 && !loading && (
          <div className="text-center py-16 bg-slate-900/40 border border-slate-800 rounded-3xl p-8 space-y-3">
            <div className="w-12 h-12 rounded-full bg-slate-800 flex items-center justify-center mx-auto text-slate-400">
              <Search className="w-6 h-6" />
            </div>
            <div className="font-semibold text-slate-200">No discoveries to show</div>
            <p className="text-xs text-slate-400 max-w-sm mx-auto">
              Type any topic or select one of the suggested prompts above to aggregate relevant discussions.
            </p>
          </div>
        )}

        <div className="grid md:grid-cols-2 gap-5">
          {results.map((source) => (
            <div
              key={source.id}
              className="bg-slate-900/80 border border-slate-800 hover:border-slate-700/80 rounded-3xl p-5 shadow-lg flex flex-col justify-between space-y-4 transition"
            >
              <div className="space-y-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-1.5 text-xs">
                    <span className="px-2 py-0.5 rounded-md font-semibold bg-slate-800 text-purple-400 border border-slate-700">
                      {source.provider}
                    </span>
                    {source.episode && (
                      <span className="px-2 py-0.5 rounded-md font-semibold bg-indigo-950/60 text-indigo-300 border border-indigo-800/40">
                        {source.episode}
                      </span>
                    )}
                    <span className="text-slate-400">•</span>
                    <span className="text-slate-300 font-medium">{source.creator}</span>
                  </div>
                  <div className="flex items-center space-x-1.5 bg-emerald-500/10 text-emerald-400 text-xs px-2.5 py-0.5 rounded-full font-bold border border-emerald-500/20 shrink-0">
                    <span>{Math.round(source.relevanceScore * 100)}% match</span>
                  </div>
                </div>

                <h3 className="font-bold text-base text-slate-100 line-clamp-2 leading-snug">
                  {source.title}
                </h3>

                <p className="text-xs text-slate-400 line-clamp-2 leading-relaxed">
                  {source.description}
                </p>

                <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-400 pt-1">
                  <div className="flex items-center space-x-3">
                    <span>Duration: {formatDuration(source.durationSeconds)}</span>
                    <span className="text-slate-600">•</span>
                    <span className="inline-flex items-center space-x-1 text-purple-300 font-medium bg-purple-950/40 px-2 py-0.5 rounded-md border border-purple-800/40">
                      <Clock className="w-3 h-3 text-purple-400" />
                      <span>Best Moment: {source.bestMomentTimestamp || '04:15'}</span>
                    </span>
                  </div>

                  {/* Rights Status Badge */}
                  <div>
                    {source.isAuthorizedForGeneration ? (
                      <span className="inline-flex items-center space-x-1 text-emerald-400 font-semibold bg-emerald-950/30 border border-emerald-800/40 px-2 py-0.5 rounded-md text-[11px]">
                        <ShieldCheck className="w-3.5 h-3.5" />
                        <span>Authorized</span>
                      </span>
                    ) : source.rightsStatus === 'BLOCKED' ? (
                      <span className="inline-flex items-center space-x-1 text-rose-400 font-semibold bg-rose-950/30 border border-rose-800/40 px-2 py-0.5 rounded-md text-[11px]">
                        <span>Blocked</span>
                      </span>
                    ) : (
                      <span className="inline-flex items-center space-x-1 text-amber-400 font-medium bg-amber-950/20 border border-amber-800/30 px-2 py-0.5 rounded-md text-[11px]">
                        <span>Discovery Only</span>
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Actions */}
              <div className="pt-3 border-t border-slate-800/80 flex items-center justify-between">
                <a
                  href={source.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-slate-400 hover:text-white flex items-center space-x-1 transition"
                >
                  <span>Open Source</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </a>

                <div className="flex items-center space-x-2">
                  {source.isAuthorizedForGeneration ? (
                    <button
                      onClick={() => navigate(`/moments?sourceId=${source.id}`)}
                      className="px-4 py-2 text-xs font-semibold text-white bg-purple-600 hover:bg-purple-500 rounded-xl shadow-md shadow-purple-900/30 transition flex items-center space-x-1.5"
                    >
                      <Sparkles className="w-3.5 h-3.5" />
                      <span>Create Clip</span>
                    </button>
                  ) : (
                    <button
                      onClick={() => setSelectedSourceForUpload(source)}
                      className="px-4 py-2 text-xs font-semibold text-amber-200 bg-amber-500/20 hover:bg-amber-500/30 border border-amber-500/30 rounded-xl shadow-md transition flex items-center space-x-1.5"
                    >
                      <UploadCloud className="w-3.5 h-3.5 text-amber-400" />
                      <span>Create Clip (Upload Media)</span>
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Upload Media Modal for DISCOVERY_ONLY Sources */}
      {selectedSourceForUpload && (
        <UploadMediaModal
          sourceId={selectedSourceForUpload.id}
          sourceTitle={selectedSourceForUpload.title}
          isOpen={!!selectedSourceForUpload}
          onClose={() => setSelectedSourceForUpload(null)}
          onAuthorized={() => {
            const authedId = selectedSourceForUpload.id;
            setResults((prev) =>
              prev.map((s) =>
                s.id === authedId
                  ? { ...s, rightsStatus: 'USER_AUTHORIZED', isAuthorizedForGeneration: true }
                  : s
              )
            );
            navigate(`/moments?sourceId=${authedId}`);
          }}
        />
      )}

      {/* Legacy Rights Verification Modal */}
      {selectedSourceForRights && (
        <RightsModal
          sourceId={selectedSourceForRights.id}
          sourceTitle={selectedSourceForRights.title}
          isOpen={!!selectedSourceForRights}
          onClose={() => setSelectedSourceForRights(null)}
          onConfirmed={() => {
            setResults((prev) =>
              prev.map((s) =>
                s.id === selectedSourceForRights.id
                  ? { ...s, rightsStatus: 'USER_OWNED', isAuthorizedForGeneration: true }
                  : s
              )
            );
          }}
        />
      )}
    </div>
  );
};
