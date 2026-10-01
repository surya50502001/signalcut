import React, { useState } from 'react';
import { ShieldCheck, AlertTriangle, X, CheckCircle, FileText } from 'lucide-react';
import apiClient from '../api/client';

interface RightsModalProps {
  sourceId: string;
  sourceTitle: string;
  isOpen: boolean;
  onClose: () => void;
  onConfirmed: () => void;
}

export const RightsModal: React.FC<RightsModalProps> = ({
  sourceId,
  sourceTitle,
  isOpen,
  onClose,
  onConfirmed,
}) => {
  const [claimedStatus, setClaimedStatus] = useState<string>('USER_OWNED');
  const [statementChecked, setStatementChecked] = useState<boolean>(false);
  const [proofUrl, setProofUrl] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleConfirm = async () => {
    if (!statementChecked) {
      setError('You must affirmatively agree to the rights confirmation statement.');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await apiClient.post('/api/v1/sources/confirm-rights', {
        sourceId,
        claimedRightsStatus: claimedStatus,
        confirmationStatement: 'I confirm that I own or have permission to use this content.',
        proofDocumentUrl: proofUrl || null,
      });

      onConfirmed();
      onClose();
    } catch (err: any) {
      setError(
        err.response?.data?.error?.message ||
        'Failed to confirm rights. Please ensure all legal criteria are satisfied.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-lg w-full p-6 shadow-2xl space-y-5 animate-in fade-in zoom-in-95 duration-200">
        <div className="flex items-start justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-2xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white">Rights & Permission Verification</h3>
              <p className="text-xs text-slate-400">Production IP compliance guarantee</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="bg-slate-800/40 border border-slate-700/60 rounded-2xl p-4 text-xs space-y-2">
          <div className="font-semibold text-slate-200 truncate">
            Target: <span className="text-purple-400">{sourceTitle}</span>
          </div>
          <p className="text-slate-400 leading-relaxed">
            SignalCut does not scrape or commercially republish unauthorized media. To transform this source into publish-ready clips, you must verify your legal authorization.
          </p>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
              Select Your Rights Status
            </label>
            <div className="grid grid-cols-2 gap-2.5">
              {[
                { id: 'USER_OWNED', label: 'User Owned', desc: 'You created this content' },
                { id: 'USER_AUTHORIZED', label: 'Authorized / Client', desc: 'Direct client consent' },
                { id: 'LICENSED', label: 'Licensed Content', desc: 'Commercial license acquired' },
                { id: 'PUBLIC_DOMAIN', label: 'Public Domain', desc: 'No active copyright' },
              ].map((tier) => (
                <button
                  key={tier.id}
                  type="button"
                  onClick={() => setClaimedStatus(tier.id)}
                  className={`p-3 rounded-xl border text-left transition ${
                    claimedStatus === tier.id
                      ? 'border-purple-500 bg-purple-500/10 text-white'
                      : 'border-slate-800 bg-slate-800/30 text-slate-400 hover:border-slate-700'
                  }`}
                >
                  <div className="font-semibold text-xs text-slate-200">{tier.label}</div>
                  <div className="text-[11px] text-slate-400 mt-0.5">{tier.desc}</div>
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
              Proof / License Document URL (Optional)
            </label>
            <input
              type="url"
              placeholder="https://drive.google.com/... or license link"
              value={proofUrl}
              onChange={(e) => setProofUrl(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
            />
          </div>

          <div className="bg-amber-950/20 border border-amber-900/40 rounded-xl p-3 flex items-start space-x-2.5">
            <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
            <div className="text-[11px] text-amber-300 leading-relaxed">
              We do not automatically assert generic "fair use". False claims may lead to immediate account suspension and revocation of generation privileges.
            </div>
          </div>

          <label className="flex items-start space-x-3 cursor-pointer pt-1">
            <input
              type="checkbox"
              checked={statementChecked}
              onChange={(e) => setStatementChecked(e.target.checked)}
              className="mt-0.5 rounded border-slate-700 text-purple-600 focus:ring-purple-500 bg-slate-950"
            />
            <span className="text-xs text-slate-200 font-medium leading-snug">
              "I confirm that I own or have permission to use this content."
            </span>
          </label>
        </div>

        {error && (
          <div className="p-3 bg-rose-950/30 border border-rose-900/50 text-rose-300 text-xs rounded-xl">
            {error}
          </div>
        )}

        <div className="flex items-center justify-end space-x-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-xs font-medium text-slate-400 hover:text-white transition"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={loading || !statementChecked}
            onClick={handleConfirm}
            className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold text-xs px-5 py-2.5 rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center space-x-2"
          >
            {loading ? <span>Verifying...</span> : <span>Confirm & Authorize</span>}
          </button>
        </div>
      </div>
    </div>
  );
};
