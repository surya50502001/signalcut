import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Sparkles,
  ArrowRight,
  ShieldCheck,
  Zap,
  Layers,
  Video,
  CheckCircle2,
  TrendingUp,
  Cpu,
  Share2
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const LandingPage: React.FC = () => {
  const [topicInput, setTopicInput] = useState('');
  const { loginDemo } = useAuth();
  const navigate = useNavigate();

  const samplePrompts = [
    'What are CEOs saying about AI agents?',
    'Latest discussions about .NET 10',
    'Best arguments about remote work',
    'Tamil startup founders discussing AI',
    'Interviews about humanoid robotics',
  ];

  const handleSearchSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!topicInput.trim()) return;
    await loginDemo();
    navigate(`/search?q=${encodeURIComponent(topicInput.trim())}`);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col selection:bg-purple-600">
      {/* Hero Section */}
      <section className="relative overflow-hidden pt-20 pb-24 md:pt-28 md:pb-32 border-b border-slate-800">
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_80%_80%_at_50%_-20%,rgba(120,119,198,0.15),rgba(255,255,255,0))]" />

        <div className="max-w-5xl mx-auto px-6 relative z-10 text-center space-y-8">
          <div className="inline-flex items-center space-x-2 px-3.5 py-1.5 rounded-full bg-purple-500/10 border border-purple-500/20 text-purple-400 text-xs font-semibold tracking-wide">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Topic-First Content Discovery & Repurposing</span>
          </div>

          <h1 className="text-4xl sm:text-6xl md:text-7xl font-extrabold tracking-tight text-white max-w-4xl mx-auto leading-[1.1]">
            Turn any topic into a{' '}
            <span className="bg-clip-text text-transparent bg-gradient-to-r from-purple-400 via-indigo-300 to-purple-500">
              content pipeline.
            </span>
          </h1>

          <p className="text-lg sm:text-xl text-slate-300 max-w-2xl mx-auto font-normal leading-relaxed">
            Discover the conversations, videos, and ideas that matter — then turn authorized content into publish-ready clips.
          </p>

          {/* Interactive Topic Search Bar */}
          <form onSubmit={handleSearchSubmit} className="max-w-2xl mx-auto pt-4">
            <div className="flex flex-col sm:flex-row items-center bg-slate-900/90 border border-slate-700/80 rounded-2xl p-2 shadow-2xl shadow-purple-950/30 gap-2">
              <input
                type="text"
                placeholder="What do you want to find? (e.g. AI agents replacing software developers)"
                value={topicInput}
                onChange={(e) => setTopicInput(e.target.value)}
                className="w-full bg-transparent px-4 py-3 text-sm sm:text-base text-white placeholder-slate-500 focus:outline-none"
              />
              <button
                type="submit"
                className="w-full sm:w-auto bg-purple-600 hover:bg-purple-500 text-white font-semibold text-sm px-6 py-3 rounded-xl transition flex items-center justify-center space-x-2 shrink-0 shadow-lg shadow-purple-900/40"
              >
                <span>Discover</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            </div>

            {/* Quick Sample Topics */}
            <div className="flex flex-wrap items-center justify-center gap-2 pt-3 text-xs text-slate-400">
              <span className="font-medium text-slate-400">Try searching:</span>
              {samplePrompts.slice(0, 3).map((prompt, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => setTopicInput(prompt)}
                  className="bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700/60 px-2.5 py-1 rounded-lg text-slate-300 transition"
                >
                  "{prompt}"
                </button>
              ))}
            </div>
          </form>

          {/* Primary & Secondary CTAs */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 pt-6">
            <button
              onClick={async () => {
                await loginDemo();
                navigate('/dashboard');
              }}
              className="w-full sm:w-auto px-8 py-3.5 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-semibold rounded-xl shadow-xl shadow-purple-900/30 transition flex items-center justify-center space-x-2"
            >
              <span>Try it free</span>
              <ArrowRight className="w-4 h-4" />
            </button>
            <a
              href="#how-it-works"
              className="w-full sm:w-auto px-7 py-3.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 text-slate-300 hover:text-white font-medium rounded-xl transition text-center"
            >
              See how it works
            </a>
          </div>

          {/* Rights & IP Safety Badge */}
          <div className="inline-flex items-center space-x-2 text-xs text-slate-400 pt-4">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span>Strict rights & permission verification. Never scrapers or arbitrary infringement.</span>
          </div>
        </div>
      </section>

      {/* The Core Difference: Topic vs Raw Video */}
      <section id="how-it-works" className="py-20 border-b border-slate-800 bg-slate-900/30">
        <div className="max-w-6xl mx-auto px-6 space-y-12">
          <div className="text-center max-w-2xl mx-auto space-y-3">
            <h2 className="text-3xl font-bold tracking-tight text-white">
              Not Just Another Video Clipper
            </h2>
            <p className="text-slate-400 text-sm leading-relaxed">
              Standard tools ask you to paste a 2-hour video link and pray for a lucky cut. SignalCut starts with the
              conversation topic itself.
            </p>
          </div>

          <div className="grid md:grid-cols-2 gap-8">
            <div className="p-8 rounded-3xl bg-slate-900/60 border border-slate-800 space-y-4">
              <div className="text-rose-400 font-semibold text-xs uppercase tracking-wider">The Old Way</div>
              <h3 className="text-xl font-bold text-slate-200">Raw URL Ingestion</h3>
              <ul className="space-y-3 text-sm text-slate-400">
                <li className="flex items-start space-x-2">
                  <span className="text-rose-400 font-bold">✕</span>
                  <span>Must manually find, watch, and vet videos beforehand</span>
                </li>
                <li className="flex items-start space-x-2">
                  <span className="text-rose-400 font-bold">✕</span>
                  <span>Unaware of copyright or commercial authorization rules</span>
                </li>
                <li className="flex items-start space-x-2">
                  <span className="text-rose-400 font-bold">✕</span>
                  <span>Optimizes solely for clickbait virality over domain substance</span>
                </li>
              </ul>
            </div>

            <div className="p-8 rounded-3xl bg-purple-950/20 border border-purple-800/40 space-y-4">
              <div className="text-purple-400 font-semibold text-xs uppercase tracking-wider">The SignalCut Way</div>
              <h3 className="text-xl font-bold text-white">Topic → Discovery → Production</h3>
              <ul className="space-y-3 text-sm text-slate-300">
                <li className="flex items-start space-x-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
                  <span>Enter any niche, trend, question, or technology concept</span>
                </li>
                <li className="flex items-start space-x-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
                  <span>Strict rights state gating: USER_OWNED, LICENSED, or PUBLIC_DOMAIN</span>
                </li>
                <li className="flex items-start space-x-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
                  <span>Configurable objectives: Educational, Contrarian, Technical, Newsworthy</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      {/* The 5-Step Pipeline */}
      <section className="py-20 border-b border-slate-800">
        <div className="max-w-6xl mx-auto px-6 space-y-12">
          <div className="text-center space-y-3">
            <h2 className="text-3xl font-bold tracking-tight text-white">The Production Pipeline</h2>
            <p className="text-slate-400 text-sm">From raw idea to multi-platform published short in under 3 minutes.</p>
          </div>

          <div className="grid sm:grid-cols-2 lg:grid-cols-5 gap-4">
            {[
              { step: '01', title: 'Topic Search', desc: 'Input natural language queries, trends, or industry questions.' },
              { step: '02', title: 'Source Discovery', desc: 'Aggregates podcasts, conferences, and verified workspace media.' },
              { step: '03', title: 'Moment AI', desc: 'Identifies high-density insights, contrarian quotes, and hooks.' },
              { step: '04', title: 'Clip Editor', desc: 'Smart 9:16 crop, dynamic captions, brand styling, and trims.' },
              { step: '05', title: 'Publish & Scale', desc: 'Direct scheduling to YouTube Shorts, TikTok, Reels, & LinkedIn.' },
            ].map((p, idx) => (
              <div key={idx} className="p-5 bg-slate-900 border border-slate-800 rounded-2xl space-y-2">
                <div className="text-2xl font-black text-purple-500/50">{p.step}</div>
                <div className="font-bold text-sm text-slate-200">{p.title}</div>
                <p className="text-xs text-slate-400 leading-relaxed">{p.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing Preview */}
      <section className="py-20 border-b border-slate-800 bg-slate-900/20">
        <div className="max-w-4xl mx-auto px-6 text-center space-y-8">
          <div className="space-y-2">
            <h2 className="text-3xl font-bold tracking-tight text-white">Transparent Credit Pricing</h2>
            <p className="text-slate-400 text-sm">Pay only for what you render. No recurring surprise fees.</p>
          </div>

          <div className="grid sm:grid-cols-3 gap-6 text-left">
            <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-4">
              <div className="font-bold text-slate-200">Starter Pack</div>
              <div className="text-3xl font-extrabold text-white">₹100</div>
              <div className="text-xs text-amber-400 font-semibold">100 Credits</div>
              <p className="text-xs text-slate-400">Ideal for topic research and creating your first 3-5 clips.</p>
            </div>
            <div className="p-6 bg-purple-950/30 border border-purple-600/50 rounded-3xl space-y-4 shadow-xl shadow-purple-950/40 relative">
              <span className="absolute -top-3 right-6 bg-purple-600 text-white text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">
                Creator Choice
              </span>
              <div className="font-bold text-white">Creator Pro</div>
              <div className="text-3xl font-extrabold text-white">₹500</div>
              <div className="text-xs text-amber-400 font-semibold">550 Credits (+50 Bonus)</div>
              <p className="text-xs text-slate-300">1080p vertical rendering, animated captions, and priority queue.</p>
            </div>
            <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-4">
              <div className="font-bold text-slate-200">Agency Scale</div>
              <div className="text-3xl font-extrabold text-white">₹1,000</div>
              <div className="text-xs text-amber-400 font-semibold">1,200 Credits (+200 Bonus)</div>
              <p className="text-xs text-slate-400">High volume batch rendering, multi-channel scheduling, and team seats.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-12 border-t border-slate-800 bg-slate-950 text-xs text-slate-500">
        <div className="max-w-6xl mx-auto px-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center space-x-2">
            <Sparkles className="w-4 h-4 text-purple-500" />
            <span className="font-semibold text-slate-300">SignalCut</span>
            <span>— AI-Powered Content Discovery & Repurposing</span>
          </div>
          <div className="flex items-center space-x-6 text-slate-400">
            <Link to="/pricing" className="hover:text-white transition">Pricing</Link>
            <Link to="/login" className="hover:text-white transition">Sign In</Link>
            <Link to="/terms" className="hover:text-white transition">Terms</Link>
            <Link to="/privacy" className="hover:text-white transition">Privacy</Link>
            <Link to="/dmca" className="hover:text-white transition">DMCA</Link>
            <span>© 2026 SignalCut Platform. All rights reserved.</span>
          </div>
        </div>
      </footer>
    </div>
  );
};
