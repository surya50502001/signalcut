import React, { useState, useRef } from 'react';
import { UploadCloud, ShieldAlert, CheckCircle, AlertTriangle, X, FileVideo, FileAudio, ArrowRight } from 'lucide-react';
import apiClient from '../api/client';

interface UploadMediaModalProps {
  sourceId: string;
  sourceTitle: string;
  isOpen: boolean;
  onClose: () => void;
  onAuthorized: () => void;
}

export const UploadMediaModal: React.FC<UploadMediaModalProps> = ({
  sourceId,
  sourceTitle,
  isOpen,
  onClose,
  onAuthorized,
}) => {
  const [file, setFile] = useState<File | null>(null);
  const [claimedStatus, setClaimedStatus] = useState<string>('USER_AUTHORIZED');
  const [statementChecked, setStatementChecked] = useState<boolean>(false);
  const [proofUrl, setProofUrl] = useState<string>('');
  const [uploading, setUploading] = useState<boolean>(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!isOpen) return null;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
      setError(null);
    }
  };

  const handleUploadAndConfirm = async () => {
    if (!file) {
      setError('Please select a media file to upload.');
      return;
    }

    if (!statementChecked) {
      setError('You must affirmatively agree to the authorization confirmation statement.');
      return;
    }

    setUploading(true);
    setUploadProgress(10);
    setError(null);

    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append(
        'confirmationStatement',
        'I confirm that I own this content or have permission to use, edit, and publish it.'
      );
      formData.append('claimedRightsStatus', claimedStatus);
      if (proofUrl) {
        formData.append('proofDocumentUrl', proofUrl);
      }

      setUploadProgress(40);
      await apiClient.post(`/api/v1/sources/${sourceId}/upload-media`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (progressEvent.total) {
            const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            setUploadProgress(Math.min(95, Math.max(40, percent)));
          }
        },
      });

      setUploadProgress(100);
      onAuthorized();
      onClose();
    } catch (err: any) {
      setError(
        err.response?.data?.error?.message ||
        'Failed to upload and authorize media. Please try again.'
      );
    } finally {
      setUploading(false);
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-lg w-full p-6 sm:p-7 shadow-2xl space-y-5 animate-in fade-in zoom-in-95 duration-200">
        <div className="flex items-start justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-2xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white">Media Authorization Required</h3>
              <p className="text-xs text-slate-400 truncate max-w-xs">{sourceTitle}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Required Mandatory Banner */}
        <div className="bg-amber-950/25 border border-amber-900/40 rounded-2xl p-4 text-xs space-y-2">
          <div className="flex items-start space-x-2.5">
            <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
            <p className="text-amber-200 leading-relaxed font-medium">
              This source is available for discovery, but SignalCut does not have an authorized way to retrieve the media. Upload the video/audio you have permission to use to continue.
            </p>
          </div>
        </div>

        {/* File Dropzone / Selector */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
            1. Select Permitted Media File
          </label>
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept="video/mp4,video/quicktime,video/webm,video/x-matroska,audio/mpeg,audio/mp4,audio/wav,audio/aac"
            className="hidden"
          />

          {!file ? (
            <div
              onClick={() => fileInputRef.current?.click()}
              className="border-2 border-dashed border-slate-700 hover:border-purple-500/80 bg-slate-950/60 hover:bg-slate-950 rounded-2xl p-6 text-center cursor-pointer transition space-y-2"
            >
              <div className="w-10 h-10 rounded-full bg-purple-950/40 border border-purple-800/40 flex items-center justify-center mx-auto text-purple-400">
                <UploadCloud className="w-5 h-5" />
              </div>
              <div className="text-xs font-semibold text-slate-200">
                Click to browse video or audio file
              </div>
              <p className="text-[11px] text-slate-500">
                Supports MP4, MOV, WEBM, MKV, MP3, M4A, WAV (up to 500 MB)
              </p>
            </div>
          ) : (
            <div className="bg-slate-950 border border-slate-800 rounded-2xl p-3.5 flex items-center justify-between">
              <div className="flex items-center space-x-3 overflow-hidden">
                <div className="w-9 h-9 rounded-xl bg-purple-950/60 border border-purple-800/40 flex items-center justify-center text-purple-400 shrink-0">
                  {file.type.startsWith('audio') ? (
                    <FileAudio className="w-4 h-4" />
                  ) : (
                    <FileVideo className="w-4 h-4" />
                  )}
                </div>
                <div className="truncate text-xs">
                  <div className="font-semibold text-white truncate">{file.name}</div>
                  <div className="text-slate-400 text-[11px]">{formatFileSize(file.size)}</div>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setFile(null)}
                className="text-xs text-rose-400 hover:text-rose-300 px-2 py-1 transition"
              >
                Change
              </button>
            </div>
          )}
        </div>

        {/* Rights Tier Selection */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
            2. Your Authorization Status
          </label>
          <div className="grid grid-cols-2 gap-2">
            {[
              { id: 'USER_OWNED', label: 'User Owned', desc: 'I created this content' },
              { id: 'USER_AUTHORIZED', label: 'Authorized / Client', desc: 'I have permission from creator' },
              { id: 'LICENSED', label: 'Licensed Content', desc: 'Valid license obtained' },
              { id: 'PUBLIC_DOMAIN', label: 'Public Domain', desc: 'Free of active copyright' },
            ].map((tier) => (
              <button
                key={tier.id}
                type="button"
                onClick={() => setClaimedStatus(tier.id)}
                className={`p-2.5 rounded-xl border text-left transition ${
                  claimedStatus === tier.id
                    ? 'border-purple-500 bg-purple-500/10 text-white'
                    : 'border-slate-800 bg-slate-950/40 text-slate-400 hover:border-slate-700'
                }`}
              >
                <div className="font-semibold text-xs text-slate-200">{tier.label}</div>
                <div className="text-[10px] text-slate-400 mt-0.5">{tier.desc}</div>
              </button>
            ))}
          </div>
        </div>

        {/* Affirmative Confirmation Checkbox */}
        <div className="pt-1">
          <label className="flex items-start space-x-3 cursor-pointer">
            <input
              type="checkbox"
              checked={statementChecked}
              onChange={(e) => setStatementChecked(e.target.checked)}
              className="mt-0.5 w-4 h-4 rounded border-slate-700 text-purple-600 focus:ring-purple-500 focus:ring-offset-slate-900 bg-slate-950"
            />
            <span className="text-xs text-slate-300 leading-snug">
              <strong>I confirm that I own this content or have permission to use, edit, and publish it.</strong>
            </span>
          </label>
        </div>

        {error && (
          <div className="p-3 bg-rose-950/40 border border-rose-900/50 rounded-xl text-xs text-rose-300">
            {error}
          </div>
        )}

        {/* Action Button & Progress */}
        <div className="pt-2">
          {uploading ? (
            <div className="space-y-2">
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className="bg-purple-600 h-2 transition-all duration-300"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
              <p className="text-center text-xs text-slate-400">
                Uploading & verifying media ({uploadProgress}%)...
              </p>
            </div>
          ) : (
            <button
              type="button"
              onClick={handleUploadAndConfirm}
              disabled={!file || !statementChecked}
              className="w-full py-3 bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold text-xs sm:text-sm rounded-xl shadow-lg shadow-purple-900/30 transition flex items-center justify-center space-x-2"
            >
              <UploadCloud className="w-4 h-4" />
              <span>Upload Media & Continue to Clip Creation</span>
              <ArrowRight className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
