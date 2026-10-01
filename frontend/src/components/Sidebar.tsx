import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Compass,
  Search,
  FolderKanban,
  Video,
  Share2,
  Coins,
  BarChart3,
  Settings,
  ShieldAlert,
  Sparkles
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const Sidebar: React.FC = () => {
  const { user } = useAuth();

  const links = [
    { to: '/dashboard', label: 'Dashboard', icon: Compass },
    { to: '/search', label: 'Search & Topic', icon: Search },
    { to: '/discoveries', label: 'Discoveries', icon: Sparkles },
    { to: '/projects', label: 'Projects', icon: FolderKanban },
    { to: '/clips', label: 'Generated Clips', icon: Video },
    { to: '/publishing', label: 'Publishing', icon: Share2 },
    { to: '/credits', label: 'Credits & Billing', icon: Coins },
    { to: '/analytics', label: 'Analytics', icon: BarChart3 },
    { to: '/settings', label: 'Settings', icon: Settings },
  ];

  if (user?.role === 'ADMIN' || user?.role === 'SUPERADMIN') {
    links.push({ to: '/admin', label: 'Admin Panel', icon: ShieldAlert });
  }

  return (
    <aside className="w-64 border-r border-slate-800 bg-slate-900/50 flex flex-col justify-between py-6 px-4">
      <div className="space-y-1">
        <div className="px-3 pb-3 text-xs font-semibold text-slate-400 uppercase tracking-wider">
          Pipeline
        </div>
        {links.map((link) => {
          const Icon = link.icon;
          return (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) =>
                `flex items-center space-x-3 px-3 py-2.5 rounded-xl text-sm font-medium transition ${
                  isActive
                    ? 'bg-purple-600/15 text-purple-400 border border-purple-500/20 font-semibold'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`
              }
            >
              <Icon className="w-4 h-4 shrink-0" />
              <span>{link.label}</span>
            </NavLink>
          );
        })}
      </div>

      <div className="p-3.5 bg-slate-800/50 border border-slate-700/60 rounded-2xl text-xs space-y-2">
        <div className="flex items-center space-x-2 text-purple-400 font-semibold">
          <Sparkles className="w-3.5 h-3.5" />
          <span>Topic-First Engine</span>
        </div>
        <p className="text-slate-400 leading-relaxed text-[11px]">
          Search any concept, question, or niche to discover high-signal moments without manual scrubbing.
        </p>
      </div>
    </aside>
  );
};
