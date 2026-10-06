import React, { useState, useEffect } from 'react';
import {
  Coins,
  ArrowUpRight,
  ArrowDownLeft,
  Clock,
  ShieldCheck,
  TrendingUp,
  BarChart3,
  Calendar,
  Layers,
  Sparkles,
  CheckCircle2
} from 'lucide-react';
import apiClient from '../api/client';
import { useAuth } from '../context/AuthContext';
import { PaymentModal } from '../components/PaymentModal';

export const CreditsPage: React.FC = () => {
  const { organization, refreshWallet } = useAuth();
  const [wallet, setWallet] = useState<any>(null);
  const [transactions, setTransactions] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showPaymentModal, setShowPaymentModal] = useState<boolean>(false);

  const fetchWalletData = async () => {
    setLoading(true);
    try {
      const [wRes, txRes] = await Promise.all([
        apiClient.get('/api/v1/credits/wallet'),
        apiClient.get('/api/v1/credits/transactions'),
      ]);
      setWallet(wRes.data?.data);
      setTransactions(txRes.data?.data || []);
    } catch {
      // fallback
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWalletData();
  }, []);

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
            <Coins className="w-7 h-7 text-amber-400" />
            <span>Credit Wallet & Ledger</span>
          </h1>
          <p className="text-sm text-slate-400">
            Pay-as-you-go credit management. Complete, immutable ledger of every reservation and consumption.
          </p>
        </div>

        <button
          onClick={() => setShowPaymentModal(true)}
          className="bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs px-5 py-2.5 rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center space-x-2"
        >
          <Coins className="w-4 h-4 text-amber-300" />
          <span>Purchase Credits</span>
        </button>
      </div>

      {/* Credit Summary Cards */}
      <div className="grid sm:grid-cols-3 gap-5">
        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Available Balance</div>
          <div className="text-3xl font-extrabold text-white flex items-center space-x-2">
            <span>{wallet?.availableBalance ?? organization?.availableCredits ?? 0}</span>
            <span className="text-xs font-bold text-purple-400 uppercase tracking-wider">Credits</span>
          </div>
          <div className="text-[11px] text-emerald-400 flex items-center space-x-1 pt-1">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Ready for rendering & analysis</span>
          </div>
        </div>

        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Currently Reserved</div>
          <div className="text-3xl font-extrabold text-amber-400 flex items-center space-x-2">
            <span>{wallet?.reservedBalance ?? 0}</span>
            <span className="text-xs font-bold text-amber-500/80 uppercase tracking-wider">Credits</span>
          </div>
          <div className="text-[11px] text-slate-400 pt-1">
            Held during active rendering jobs (refunded on failure)
          </div>
        </div>

        <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
          <div className="text-xs font-semibold text-slate-400">Lifetime Consumption</div>
          <div className="text-3xl font-extrabold text-slate-300 flex items-center space-x-2">
            <span>{wallet?.lifetimeSpent ?? 0}</span>
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Credits</span>
          </div>
          <div className="text-[11px] text-slate-400 pt-1">
            Total credits consumed across workspace
          </div>
        </div>
      </div>

      {/* Immutable Ledger Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-3xl overflow-hidden shadow-xl space-y-4 p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-bold text-white">Immutable Transaction Ledger</h2>
          <span className="text-xs text-slate-400">All ledger mutations are cryptographically auditable</span>
        </div>

        {loading ? (
          <div className="py-12 text-center text-xs text-slate-400">Loading ledger records...</div>
        ) : transactions.length === 0 ? (
          <div className="py-12 text-center text-xs text-slate-400">No transactions recorded yet.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-slate-300">
              <thead className="bg-slate-950/60 text-slate-400 uppercase text-[10px] tracking-wider border-b border-slate-800">
                <tr>
                  <th className="py-3 px-4">Date (UTC)</th>
                  <th className="py-3 px-4">Type</th>
                  <th className="py-3 px-4">Amount</th>
                  <th className="py-3 px-4">Balance After</th>
                  <th className="py-3 px-4">Description</th>
                  <th className="py-3 px-4">Idempotency Key</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-mono">
                {transactions.map((tx) => (
                  <tr key={tx.id} className="hover:bg-slate-800/30 transition">
                    <td className="py-3 px-4 text-slate-400 font-sans text-xs">
                      {new Date(tx.createdAt).toLocaleDateString()} {new Date(tx.createdAt).toLocaleTimeString()}
                    </td>
                    <td className="py-3 px-4">
                      <span
                        className={`px-2 py-0.5 rounded text-[10px] font-bold font-sans ${
                          tx.type === 'PURCHASE' || tx.type === 'BONUS'
                            ? 'bg-emerald-500/20 text-emerald-400'
                            : tx.type === 'REFUND'
                            ? 'bg-blue-500/20 text-blue-400'
                            : tx.type === 'RESERVATION'
                            ? 'bg-amber-500/20 text-amber-400'
                            : 'bg-rose-500/20 text-rose-400'
                        }`}
                      >
                        {tx.type}
                      </span>
                    </td>
                    <td
                      className={`py-3 px-4 font-bold ${
                        tx.amount > 0 ? 'text-emerald-400' : 'text-slate-300'
                      }`}
                    >
                      {tx.amount > 0 ? `+${tx.amount}` : tx.amount}
                    </td>
                    <td className="py-3 px-4 text-slate-200">{tx.balanceAfter}</td>
                    <td className="py-3 px-4 text-slate-300 font-sans max-w-xs truncate">{tx.description}</td>
                    <td className="py-3 px-4 text-[11px] text-slate-500 truncate max-w-[140px]">
                      {tx.idempotencyKey || '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <PaymentModal
        isOpen={showPaymentModal}
        onClose={() => {
          setShowPaymentModal(false);
          fetchWalletData();
        }}
      />
    </div>
  );
};

export const AnalyticsPage: React.FC = () => {
  const [analytics, setAnalytics] = useState<any>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const fetchAnalytics = async () => {
      try {
        const res = await apiClient.get('/api/v1/analytics/overview');
        setAnalytics(res.data?.data);
      } catch {
        // fallback
      } finally {
        setLoading(false);
      }
    };
    fetchAnalytics();
  }, []);

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-8">
      <div className="space-y-1">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-white flex items-center space-x-3">
          <BarChart3 className="w-7 h-7 text-purple-400" />
          <span>Workspace Analytics</span>
        </h1>
        <p className="text-sm text-slate-400">
          Measure content throughput, saved production hours, and credit consumption efficiency.
        </p>
      </div>

      {/* Metrics Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {[
          { label: 'Topic Searches', value: analytics?.totalSearches ?? 0, change: 'Lifetime query count' },
          { label: 'Sources Discovered', value: analytics?.totalSourcesDiscovered ?? 0, change: 'Rights compliance tracked' },
          { label: 'Clips Generated', value: analytics?.totalClipsGenerated ?? 0, change: '1080x1920 9:16' },
          { label: 'Hours Saved', value: `${analytics?.estimatedTimeSavedHours ?? 0}h`, change: 'Estimated manual edit time' },
        ].map((stat, i) => (
          <div key={i} className="p-6 bg-slate-900 border border-slate-800 rounded-3xl space-y-2 shadow-xl">
            <div className="text-xs font-semibold text-slate-400">{stat.label}</div>
            <div className="text-3xl font-extrabold text-white">{stat.value}</div>
            <div className="text-[11px] text-purple-400 font-medium">{stat.change}</div>
          </div>
        ))}
      </div>

      {/* Repurposing Throughput Chart Simulation */}
      <div className="p-6 bg-slate-900 border border-slate-800 rounded-3xl shadow-xl space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-bold text-white">Repurposing Pipeline Velocity</h2>
          <span className="text-xs text-slate-400">Last 30 Days</span>
        </div>
        <div className="h-48 flex items-end justify-between gap-2 pt-6 px-2">
          {[25, 40, 30, 65, 80, 50, 95, 70, 85, 110, 90, 130].map((val, idx) => (
            <div key={idx} className="flex-1 flex flex-col items-center gap-2">
              <div
                className="w-full bg-gradient-to-t from-purple-800 to-indigo-500 rounded-t-lg transition-all hover:brightness-125"
                style={{ height: `${(val / 140) * 100}%` }}
              />
              <span className="text-[10px] text-slate-500">W{idx + 1}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
