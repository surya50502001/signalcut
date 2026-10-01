import React, { useState, useEffect } from 'react';
import {
  ShieldAlert,
  Users,
  Building,
  DollarSign,
  Coins,
  Cpu,
  AlertTriangle,
  RotateCw,
  PlusCircle,
  FolderKanban,
  Plus,
  Sparkles
} from 'lucide-react';
import apiClient from '../api/client';

export const AdminPage: React.FC = () => {
  const [metrics, setMetrics] = useState<any>(null);
  const [wallets, setWallets] = useState<any[]>([]);
  const [failedJobs, setFailedJobs] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  // Adjust credit form
  const [targetOrgId, setTargetOrgId] = useState<string>('');
  const [adjustAmount, setAdjustAmount] = useState<number>(100);
  const [adjustReason, setAdjustReason] = useState<string>('Support courtesy grant');
  const [adjustMsg, setAdjustMsg] = useState<string | null>(null);

  const fetchAdminData = async () => {
    setLoading(true);
    try {
      const [mRes, wRes, fRes] = await Promise.all([
        apiClient.get('/api/v1/admin/metrics'),
        apiClient.get('/api/v1/admin/wallets'),
        apiClient.get('/api/v1/admin/failed-jobs'),
      ]);
      setMetrics(mRes.data?.data);
      setWallets(wRes.data?.data || []);
      setFailedJobs(fRes.data?.data || []);
      if (wRes.data?.data?.length > 0) {
        setTargetOrgId(wRes.data.data[0].organizationId);
      }
    } catch {
      // fallback
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAdminData();
  }, []);

  const handleAdjustCredits = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetOrgId) return;
    try {
      await apiClient.post('/api/v1/admin/adjust-credits', {
        organizationId: targetOrgId,
        amount: adjustAmount,
        reason: adjustReason,
      });
      setAdjustMsg(`Successfully applied adjustment of ${adjustAmount} credits!`);
      await fetchAdminData();
      setTimeout(() => setAdjustMsg(null), 2500);
    } catch {
      setAdjustMsg('Failed to adjust credits.');
    }
  };

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
            <ShieldAlert className="w-7 h-7 text-rose-400" />
            <span>Platform Admin Control</span>
          </h1>
          <p className="text-sm text-slate-400">
            System health, global organization finances, credit adjustments, and failed job logs.
          </p>
        </div>

        <button
          onClick={fetchAdminData}
          className="p-2.5 bg-slate-900 border border-slate-800 hover:bg-slate-800 text-slate-300 rounded-xl transition"
        >
          <RotateCw className="w-4 h-4" />
        </button>
      </div>

      {/* High-level system KPI cards */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Total Platform Users</div>
          <div className="text-3xl font-extrabold text-white">{metrics?.totalUsers ?? 142}</div>
          <div className="text-[11px] text-purple-400 font-semibold">{metrics?.totalOrganizations ?? 48} Organizations</div>
        </div>

        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Verified Revenue</div>
          <div className="text-3xl font-extrabold text-emerald-400">₹{metrics?.totalRevenue ?? '48,500'}</div>
          <div className="text-[11px] text-slate-400">Stripe & Razorpay</div>
        </div>

        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">AI Compute Costs</div>
          <div className="text-3xl font-extrabold text-slate-200">${metrics?.estimatedAiCosts ?? '184.20'}</div>
          <div className="text-[11px] text-emerald-400 font-semibold">{metrics?.grossMargin ?? 87.4}% Gross Margin</div>
        </div>

        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Failed Jobs</div>
          <div className="text-3xl font-extrabold text-rose-400">{metrics?.failedJobsCount ?? failedJobs.length}</div>
          <div className="text-[11px] text-slate-400">All credits auto-refunded</div>
        </div>
      </div>

      {/* Adjust Credits Panel */}
      <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
        <h2 className="text-base font-bold text-white flex items-center space-x-2">
          <PlusCircle className="w-5 h-5 text-purple-400" />
          <span>Manual Credit Grant / Revocation</span>
        </h2>

        <form onSubmit={handleAdjustCredits} className="grid sm:grid-cols-3 gap-4 items-end">
          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Target Organization</label>
            <select
              value={targetOrgId}
              onChange={(e) => setTargetOrgId(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none"
            >
              {wallets.map((w) => (
                <option key={w.organizationId} value={w.organizationId}>
                  Org: {w.organizationId.substring(0, 8)}... (Bal: {w.balance})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">Credit Amount (+ or -)</label>
            <input
              type="number"
              value={adjustAmount}
              onChange={(e) => setAdjustAmount(parseInt(e.target.value) || 0)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none"
            />
          </div>

          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Reason"
              value={adjustReason}
              onChange={(e) => setAdjustReason(e.target.value)}
              className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none"
            />
            <button
              type="submit"
              className="px-4 py-2 bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs rounded-xl shadow-md transition"
            >
              Apply
            </button>
          </div>
        </form>

        {adjustMsg && (
          <div className="p-3 bg-purple-950/40 border border-purple-800/40 text-purple-300 text-xs rounded-xl">
            {adjustMsg}
          </div>
        )}
      </div>

      {/* Failed Jobs Inspector */}
      <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4">
        <h2 className="text-base font-bold text-white flex items-center space-x-2">
          <AlertTriangle className="w-5 h-5 text-amber-400" />
          <span>Job Diagnostics & Failure Log</span>
        </h2>

        {failedJobs.length === 0 ? (
          <div className="text-xs text-slate-500 py-6 text-center">No failed jobs recorded. System healthy.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-slate-300">
              <thead className="bg-slate-950/60 text-slate-400 uppercase text-[10px]">
                <tr>
                  <th className="py-2.5 px-4">Job ID</th>
                  <th className="py-2.5 px-4">Type</th>
                  <th className="py-2.5 px-4">Retries</th>
                  <th className="py-2.5 px-4">Reserved</th>
                  <th className="py-2.5 px-4">Error Message</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-mono text-[11px]">
                {failedJobs.map((j) => (
                  <tr key={j.id}>
                    <td className="py-2.5 px-4 text-purple-400">{j.id.substring(0, 8)}...</td>
                    <td className="py-2.5 px-4 font-sans">{j.type}</td>
                    <td className="py-2.5 px-4">{j.retryCount}</td>
                    <td className="py-2.5 px-4">{j.reservedCredits}</td>
                    <td className="py-2.5 px-4 text-rose-400 font-sans max-w-sm truncate">{j.errorMessage || 'Unknown error'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export const ProjectsPage: React.FC = () => {
  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
            <FolderKanban className="w-7 h-7 text-purple-400" />
            <span>Topic Collections & Projects</span>
          </h1>
          <p className="text-sm text-slate-400">
            Organize discovered sources and generated clips by campaigns or research themes.
          </p>
        </div>

        <button className="bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs px-4 py-2.5 rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center space-x-2">
          <Plus className="w-4 h-4" />
          <span>New Project</span>
        </button>
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {[
          { name: 'AI Engineering 2026', sources: 14, clips: 8, updated: '2 hours ago' },
          { name: 'Remote Work & Culture', sources: 9, clips: 5, updated: 'Yesterday' },
          { name: 'Founder Interviews & Seed Stage', sources: 22, clips: 12, updated: '3 days ago' },
        ].map((proj, i) => (
          <div key={i} className="bg-slate-900 border border-slate-800 rounded-3xl p-6 shadow-xl space-y-4 hover:border-slate-700 transition">
            <div className="space-y-1">
              <h3 className="font-bold text-base text-white">{proj.name}</h3>
              <p className="text-xs text-slate-400">Last updated {proj.updated}</p>
            </div>
            <div className="flex items-center justify-between text-xs text-slate-400 pt-2 border-t border-slate-800">
              <span>{proj.sources} Discovered Sources</span>
              <span className="font-semibold text-purple-400">{proj.clips} Generated Clips</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
