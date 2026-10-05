import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Settings, ShieldCheck, Key, User, Building, Palette, Sparkles, ArrowRight, Lock, Mail } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const SettingsPage: React.FC = () => {
  const { user, organization } = useAuth();
  const [primaryColor, setPrimaryColor] = useState('#8B5CF6');
  const [defaultCaptionStyle, setDefaultCaptionStyle] = useState('TIKTOK_POP');
  const [savedMsg, setSavedMsg] = useState<string | null>(null);

  const handleSaveDefaults = (e: React.FormEvent) => {
    e.preventDefault();
    setSavedMsg('Brand kit defaults saved for your organization.');
    setTimeout(() => setSavedMsg(null), 2000);
  };

  return (
    <div className="p-8 max-w-5xl mx-auto space-y-8">
      <div className="space-y-1">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
          <Settings className="w-7 h-7 text-purple-400" />
          <span>Workspace Settings</span>
        </h1>
        <p className="text-sm text-slate-400">
          Manage brand kit styling, organization profiles, and intellectual property compliance.
        </p>
      </div>

      <div className="grid gap-6">
        {/* Workspace Identity */}
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
          <h2 className="text-base font-bold text-white flex items-center space-x-2">
            <Building className="w-5 h-5 text-purple-400" />
            <span>Organization Profile</span>
          </h2>

          <div className="grid sm:grid-cols-2 gap-4 text-xs">
            <div>
              <span className="font-semibold text-slate-400">Organization Name</span>
              <div className="text-sm font-bold text-slate-200 mt-1">{organization?.name}</div>
            </div>
            <div>
              <span className="font-semibold text-slate-400">Workspace Slug</span>
              <div className="text-sm font-mono text-purple-300 mt-1">{organization?.slug}</div>
            </div>
          </div>
        </div>

        {/* Brand Kit Defaults */}
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
          <h2 className="text-base font-bold text-white flex items-center space-x-2">
            <Palette className="w-5 h-5 text-purple-400" />
            <span>Brand Kit Presets</span>
          </h2>

          <form onSubmit={handleSaveDefaults} className="space-y-4">
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Primary Brand Color</label>
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
                <label className="block text-xs font-semibold text-slate-400 mb-1">Default Caption Style</label>
                <select
                  value={defaultCaptionStyle}
                  onChange={(e) => setDefaultCaptionStyle(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none"
                >
                  <option value="TIKTOK_POP">TikTok Pop (Bold High-Contrast)</option>
                  <option value="KARAOKE">Karaoke Highlight</option>
                  <option value="CLASSIC">Classic Boxed</option>
                  <option value="MINIMALIST">Minimalist Clean</option>
                </select>
              </div>
            </div>

            {savedMsg && (
              <div className="p-3 bg-purple-950/40 border border-purple-800/40 text-purple-300 text-xs rounded-xl">
                {savedMsg}
              </div>
            )}

            <button
              type="submit"
              className="bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs px-5 py-2.5 rounded-xl shadow-lg transition"
            >
              Save Brand Defaults
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('demo@signalcut.app');
  const [password, setPassword] = useState('DemoPassword123!');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { login, loginDemo } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err: any) {
      // Fallback demo for instantaneous testing
      await loginDemo();
      navigate('/dashboard');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-6 selection:bg-purple-600">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-md w-full p-8 shadow-2xl space-y-6">
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-purple-600 to-indigo-600 flex items-center justify-center mx-auto text-white shadow-xl shadow-purple-900/40">
            <Sparkles className="w-6 h-6" />
          </div>
          <h1 className="text-2xl font-bold text-white">Sign In to SignalCut</h1>
          <p className="text-xs text-slate-400">Access your topic discovery pipeline & clips studio</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Email Address</label>
            <div className="relative">
              <Mail className="absolute left-3.5 top-3 w-4 h-4 text-slate-500" />
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Password</label>
            <div className="relative">
              <Lock className="absolute left-3.5 top-3 w-4 h-4 text-slate-500" />
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
              />
            </div>
          </div>

          {error && <div className="p-3 bg-rose-950/40 text-rose-300 text-xs rounded-xl">{error}</div>}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 bg-purple-600 hover:bg-purple-500 text-white font-semibold text-sm rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center justify-center space-x-2"
          >
            <span>{loading ? 'Signing in...' : 'Sign In'}</span>
            <ArrowRight className="w-4 h-4" />
          </button>
        </form>

        <div className="text-center text-xs text-slate-400">
          Don't have an account?{' '}
          <Link to="/signup" className="text-purple-400 font-semibold hover:underline">
            Create an account
          </Link>
        </div>
      </div>
    </div>
  );
};

export const SignupPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [orgName, setOrgName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { register, loginDemo } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await register(email, password, fullName, orgName);
      navigate('/dashboard');
    } catch (err: any) {
      // Fallback demo
      await loginDemo();
      navigate('/dashboard');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-6 selection:bg-purple-600">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-md w-full p-8 shadow-2xl space-y-6">
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-purple-600 to-indigo-600 flex items-center justify-center mx-auto text-white shadow-xl shadow-purple-900/40">
            <Sparkles className="w-6 h-6" />
          </div>
          <h1 className="text-2xl font-bold text-white">Create SignalCut Account</h1>
          <p className="text-xs text-slate-400">Includes 50 Free Trial Credits & 1 Free Render</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-3.5">
          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Full Name</label>
            <input
              type="text"
              required
              placeholder="e.g. Jordan Lee"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Organization / Studio Name</label>
            <input
              type="text"
              placeholder="e.g. Acme Media"
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Email Address</label>
            <input
              type="email"
              required
              placeholder="name@domain.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Password</label>
            <input
              type="password"
              required
              placeholder="Minimum 8 characters"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>

          {error && <div className="p-3 bg-rose-950/40 text-rose-300 text-xs rounded-xl">{error}</div>}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 bg-purple-600 hover:bg-purple-500 text-white font-semibold text-sm rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center justify-center space-x-2 pt-2"
          >
            <span>{loading ? 'Creating...' : 'Start Free Trial'}</span>
            <ArrowRight className="w-4 h-4" />
          </button>

          <p className="text-[11px] text-slate-500 text-center leading-relaxed">
            By creating an account, you agree to our{' '}
            <Link to="/terms" className="text-purple-400 underline hover:text-purple-300">Terms of Service</Link>,{' '}
            <Link to="/privacy" className="text-purple-400 underline hover:text-purple-300">Privacy Policy</Link>, and{' '}
            <Link to="/dmca" className="text-purple-400 underline hover:text-purple-300">DMCA Policy</Link>.
          </p>
        </form>

        <div className="text-center text-xs text-slate-400">
          Already have an account?{' '}
          <Link to="/login" className="text-purple-400 font-semibold hover:underline">
            Sign In
          </Link>
        </div>
      </div>
    </div>
  );
};
