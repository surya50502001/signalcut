import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  Sparkles,
  Search,
  Video,
  Coins,
  ArrowRight,
  TrendingUp,
  Clock,
  Layers,
  CheckCircle2,
  Share2,
  ShieldCheck
} from 'lucide-react';
import apiClient from '../api/client';
import { useAuth } from '../context/AuthContext';
import { PaymentModal } from '../components/PaymentModal';

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, organization } = useAuth();
  const [query, setQuery] = useState('');
  const [sources, setSources] = useState<any[]>([]);
  const [clips, setClips] = useState<any[]>([]);
  const [showPayment, setShowPayment] = useState(false);

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        const [sRes, cRes] = await Promise.all([
          apiClient.get('/api/v1/sources?pageSize=4'),
          apiClient.get('/api/v1/clips?pageSize=3'),
        ]);
        setSources(sRes.data?.data || []);
        setClips(cRes.data?.data || []);
      } catch {
        // fallback
      }
    };
    loadDashboard();
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;
    navigate(`/search?q=${encodeURIComponent(query.trim())}`);
  };

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      {/* Welcome Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-gradient-to-r from-purple-950/40 via-slate-900 to-indigo-950/40 border border-purple-800/30 rounded-3xl p-6 shadow-xl">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold text-white">
            Welcome back, {user?.fullName || 'Creator'} 👋
          </h1>
          <p className="text-xs text-slate-400">
            Workspace: <span className="text-purple-300 font-semibold">{organization?.name}</span> • Ready to transform conversations into publishable media.
          </p>
        </div>

        <button
          onClick={() => setShowPayment(true)}
          className="self-start sm:self-center px-4 py-2 bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs rounded-xl shadow-lg shadow-purple-900/40 transition flex items-center space-x-2"
        >
          <Coins className="w-4 h-4 text-amber-300" />
          <span>Add Credits ({organization?.availableCredits ?? 0} Avail)</span>
        </button>
      </div>

      {/* Hero Quick Search Box */}
      <div className="bg-slate-900 border border-slate-800 rounded-3xl p-5 shadow-xl space-y-3">
        <div className="text-xs font-bold text-purple-400 uppercase tracking-wider flex items-center space-x-1.5">
          <Sparkles className="w-3.5 h-3.5" />
          <span>Start With a Topic</span>
        </div>
        <form onSubmit={handleSearchSubmit} className="flex flex-col sm:flex-row gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
            <input
              type="text"
              placeholder="What do you want to find? (e.g. What are CEOs saying about AI agents?)"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-2xl pl-12 pr-4 py-3 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>
          <button
            type="submit"
            className="px-6 py-3 bg-purple-600 hover:bg-purple-500 text-white font-semibold text-sm rounded-2xl shadow-lg transition flex items-center justify-center space-x-2"
          >
            <span>Discover</span>
            <ArrowRight className="w-4 h-4" />
          </button>
        </form>
      </div>

      {/* Metrics Row */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: 'Credit Balance', val: organization?.availableCredits ?? 0, sub: 'Pay-as-you-go', icon: Coins, color: 'text-amber-400' },
          { label: 'Discovered Sources', val: sources.length, sub: 'Aggregated & Ranked', icon: Sparkles, color: 'text-purple-400' },
          { label: 'Generated Clips', val: clips.length, sub: '9:16 Vertical Video', icon: Video, color: 'text-indigo-400' },
          { label: 'Social Channels', val: 4, sub: 'YouTube, TikTok, Reels, LinkedIn', icon: Share2, color: 'text-emerald-400' },
        ].map((item, i) => {
          const Icon = item.icon;
          return (
            <div key={i} className="p-5 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-lg">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-slate-400">{item.label}</span>
                <Icon className={`w-4 h-4 ${item.color}`} />
              </div>
              <div className="text-2xl font-black text-white">{item.val}</div>
              <div className="text-[11px] text-slate-500">{item.sub}</div>
            </div>
          );
        })}
      </div>

      {/* Recent Discoveries & Recent Clips */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Recent Discoveries */}
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-bold text-white flex items-center space-x-2">
              <Sparkles className="w-4 h-4 text-purple-400" />
              <span>Recent Discovered Sources</span>
            </h2>
            <Link to="/discoveries" className="text-xs text-purple-400 hover:underline">
              View All
            </Link>
          </div>

          <div className="space-y-3">
            {sources.length === 0 ? (
              <div className="p-8 text-center bg-slate-950/40 border border-slate-800/60 rounded-2xl space-y-1">
                <div className="text-xs text-slate-300 font-medium">No sources discovered yet</div>
                <p className="text-[11px] text-slate-500">Search for a topic above to discover relevant discussions.</p>
              </div>
            ) : (
              sources.map((s) => (
                <div
                  key={s.id}
                  onClick={() => navigate(`/moments?sourceId=${s.id}`)}
                  className="p-3.5 bg-slate-950/60 hover:bg-slate-800/50 border border-slate-800 rounded-2xl cursor-pointer transition flex items-center justify-between"
                >
                  <div className="space-y-0.5 max-w-[70%]">
                    <div className="font-bold text-xs text-slate-200 truncate">{s.title}</div>
                    <div className="text-[11px] text-slate-400">{s.creator} • {s.provider}</div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-xs font-bold text-emerald-400">
                      {Math.round((s.relevanceScore || 0.95) * 100)}%
                    </span>
                    <ArrowRight className="w-3.5 h-3.5 text-slate-500" />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Recent Clips */}
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-bold text-white flex items-center space-x-2">
              <Video className="w-4 h-4 text-purple-400" />
              <span>Recent Clips</span>
            </h2>
            <Link to="/clips" className="text-xs text-purple-400 hover:underline">
              View All
            </Link>
          </div>

          <div className="space-y-3">
            {clips.length === 0 ? (
              <div className="p-8 text-center bg-slate-950/40 border border-slate-800/60 rounded-2xl space-y-1">
                <div className="text-xs text-slate-300 font-medium">No clips rendered yet</div>
                <p className="text-[11px] text-slate-500">Select high-signal moments to render your vertical shorts.</p>
              </div>
            ) : (
              clips.map((c) => (
                <div
                  key={c.id}
                  onClick={() => navigate(`/editor?clipId=${c.id}`)}
                  className="p-3.5 bg-slate-950/60 hover:bg-slate-800/50 border border-slate-800 rounded-2xl cursor-pointer transition flex items-center justify-between"
                >
                  <div className="space-y-0.5 max-w-[70%]">
                    <div className="font-bold text-xs text-slate-200 truncate">{c.title}</div>
                    <div className="text-[11px] text-purple-300 truncate">"{c.hook}"</div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400">
                      {c.renderStatus || 'Ready'}
                    </span>
                    <ArrowRight className="w-3.5 h-3.5 text-slate-500" />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      <PaymentModal isOpen={showPayment} onClose={() => setShowPayment(false)} />
    </div>
  );
};

export const PricingPage: React.FC = () => {
  const [showPayment, setShowPayment] = useState(false);

  return (
    <div className="p-8 max-w-5xl mx-auto space-y-12">
      <div className="text-center space-y-3 max-w-2xl mx-auto">
        <h1 className="text-3xl sm:text-4xl font-extrabold text-white">
          Simple, Predictable Credit Pricing
        </h1>
        <p className="text-sm text-slate-400 leading-relaxed">
          Never get locked into an overpriced monthly subscription. Buy credits whenever you need to discover, analyze, and render high-signal clips.
        </p>
      </div>

      <div className="grid sm:grid-cols-3 gap-6">
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-5 flex flex-col justify-between">
          <div className="space-y-4">
            <h3 className="font-bold text-slate-200 text-lg">Starter</h3>
            <div className="text-4xl font-extrabold text-white">₹100</div>
            <div className="text-xs font-bold text-amber-400">100 Credits</div>
            <ul className="space-y-2.5 text-xs text-slate-400 pt-2 border-t border-slate-800">
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>Topic Search & Discovery</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>Speech-to-Text Transcription</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>AI Moment Scoring</span>
              </li>
            </ul>
          </div>
          <button
            onClick={() => setShowPayment(true)}
            className="w-full py-2.5 bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-white rounded-xl transition"
          >
            Buy Starter
          </button>
        </div>

        <div className="bg-purple-950/30 border border-purple-600/50 rounded-3xl p-6 shadow-2xl space-y-5 flex flex-col justify-between relative">
          <span className="absolute -top-3 right-6 bg-purple-600 text-white text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">
            Most Popular
          </span>
          <div className="space-y-4">
            <h3 className="font-bold text-white text-lg">Creator Pro</h3>
            <div className="text-4xl font-extrabold text-white">₹500</div>
            <div className="text-xs font-bold text-amber-400">550 Credits (+50 Bonus)</div>
            <ul className="space-y-2.5 text-xs text-slate-300 pt-2 border-t border-slate-800">
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>Everything in Starter</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>1080x1920 9:16 Vertical Video</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>TikTok Pop & Karaoke Captions</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>No Watermark on Output</span>
              </li>
            </ul>
          </div>
          <button
            onClick={() => setShowPayment(true)}
            className="w-full py-2.5 bg-purple-600 hover:bg-purple-500 text-xs font-semibold text-white rounded-xl shadow-lg transition"
          >
            Buy Creator Pro
          </button>
        </div>

        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-5 flex flex-col justify-between">
          <div className="space-y-4">
            <h3 className="font-bold text-slate-200 text-lg">Agency Scale</h3>
            <div className="text-4xl font-extrabold text-white">₹1,000</div>
            <div className="text-xs font-bold text-amber-400">1,200 Credits (+200 Bonus)</div>
            <ul className="space-y-2.5 text-xs text-slate-400 pt-2 border-t border-slate-800">
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>Everything in Creator Pro</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>Multi-Platform Direct Publishing</span>
              </li>
              <li className="flex items-center space-x-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                <span>High Priority Render Queue</span>
              </li>
            </ul>
          </div>
          <button
            onClick={() => setShowPayment(true)}
            className="w-full py-2.5 bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-white rounded-xl transition"
          >
            Buy Agency Scale
          </button>
        </div>
      </div>

      <PaymentModal isOpen={showPayment} onClose={() => setShowPayment(false)} />
    </div>
  );
};
