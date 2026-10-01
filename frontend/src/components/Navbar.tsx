import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Sparkles, Coins, LogOut, ShieldCheck, User } from 'lucide-react';

interface NavbarProps {
  onOpenPayment?: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onOpenPayment }) => {
  const { user, organization, isAuthenticated, logout, loginDemo } = useAuth();
  const navigate = useNavigate();

  return (
    <header className="h-16 border-b border-slate-800 bg-slate-900/80 backdrop-blur-md px-6 flex items-center justify-between sticky top-0 z-40">
      <div className="flex items-center space-x-3">
        <Link to="/" className="flex items-center space-x-2.5">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-purple-600 via-indigo-600 to-purple-400 flex items-center justify-center shadow-lg shadow-purple-900/30">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <span className="text-xl font-bold tracking-tight text-white flex items-center">
            Signal<span className="text-purple-400">Cut</span>
            <span className="ml-2 px-1.5 py-0.5 text-[10px] uppercase font-semibold bg-purple-500/10 text-purple-400 border border-purple-500/20 rounded">
              Beta
            </span>
          </span>
        </Link>
      </div>

      <div className="flex items-center space-x-4">
        {isAuthenticated ? (
          <>
            <button
              onClick={onOpenPayment}
              className="flex items-center space-x-2 bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 px-3.5 py-1.5 rounded-full transition text-sm font-medium text-slate-200 shadow-sm"
              title="Click to add credits"
            >
              <Coins className="w-4 h-4 text-amber-400 animate-pulse" />
              <span>{organization?.availableCredits ?? 0} Credits</span>
              <span className="text-xs bg-purple-600/30 text-purple-300 px-1.5 py-0.5 rounded font-bold">+ Top Up</span>
            </button>

            <div className="flex items-center space-x-2.5 pl-2 border-l border-slate-800">
              <div className="w-8 h-8 rounded-full bg-purple-900/50 border border-purple-500/30 flex items-center justify-center text-purple-300 font-medium text-sm">
                {user?.fullName?.charAt(0) || 'U'}
              </div>
              <div className="hidden md:block text-left text-xs">
                <div className="font-semibold text-slate-200">{user?.fullName}</div>
                <div className="text-slate-400 truncate max-w-[120px]">{organization?.name}</div>
              </div>
              <button
                onClick={() => {
                  logout();
                  navigate('/login');
                }}
                className="p-1.5 text-slate-400 hover:text-rose-400 hover:bg-slate-800 rounded-lg transition"
                title="Log out"
              >
                <LogOut className="w-4 h-4" />
              </button>
            </div>
          </>
        ) : (
          <div className="flex items-center space-x-3">
            <button
              onClick={async () => {
                await loginDemo();
                navigate('/dashboard');
              }}
              className="text-xs font-semibold px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
            >
              Instant Demo
            </button>
            <Link
              to="/login"
              className="text-sm font-medium text-slate-300 hover:text-white transition"
            >
              Sign In
            </Link>
            <Link
              to="/signup"
              className="text-sm font-medium bg-purple-600 hover:bg-purple-500 text-white px-4 py-2 rounded-lg shadow-md shadow-purple-900/30 transition"
            >
              Try it free
            </Link>
          </div>
        )}
      </div>
    </header>
  );
};
