import React from 'react';
import { Link } from 'react-router-dom';
import { ShieldCheck, Scale, FileText, AlertTriangle, Mail, ArrowLeft } from 'lucide-react';

export const TermsOfServicePage: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-purple-600">
      <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-5xl mx-auto px-6 h-16 flex items-center justify-between">
          <Link to="/" className="flex items-center space-x-2 text-slate-400 hover:text-white transition">
            <ArrowLeft className="w-4 h-4" />
            <span className="text-sm font-semibold">Back to SignalCut</span>
          </Link>
          <div className="flex items-center space-x-2">
            <Scale className="w-5 h-5 text-purple-400" />
            <span className="font-bold text-sm text-white">Terms of Service</span>
          </div>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-6 py-12 space-y-10">
        <div className="space-y-3">
          <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-purple-950/60 border border-purple-800/50 text-purple-300 text-xs font-semibold">
            <Scale className="w-3.5 h-3.5" />
            <span>Legal Agreement</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight">Terms of Service</h1>
          <p className="text-sm text-slate-400">Effective Date: January 1, 2026 • Last Updated: October 5, 2026</p>
        </div>

        <div className="p-5 rounded-2xl bg-amber-950/30 border border-amber-800/40 text-amber-200 text-xs sm:text-sm leading-relaxed space-y-2">
          <div className="flex items-center space-x-2 font-bold text-amber-300">
            <AlertTriangle className="w-5 h-5" />
            <span>MANDATORY INDEMNITY & CONTENT OWNERSHIP NOTICE</span>
          </div>
          <p>
            Please read these Terms carefully. By accessing or using SignalCut, you acknowledge and agree that you bear sole legal responsibility for any media content, audio, or video you import, process, render, or distribute. You expressly warrant that you own or have obtained all required third-party licenses, clearances, and authorizations.
          </p>
        </div>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">1. Acceptance of Terms</h2>
          <p>
            By creating an account, connecting workspace media, purchasing subscription credits, or accessing the SignalCut platform, you agree to be bound by these Terms of Service. If you are entering into these Terms on behalf of an entity or studio, you represent that you possess the authority to bind such entity.
          </p>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">2. Permitted Use & Source Authorization</h2>
          <p>
            SignalCut provides automated AI-assisted content discovery, transcript indexing, semantic moment extraction, and vertical format rendering. You agree to use the Service exclusively for lawful purposes and in strict accordance with the following requirements:
          </p>
          <ul className="list-disc pl-6 space-y-2 text-slate-300">
            <li>
              <strong className="text-white">Authorized Content Only:</strong> You may only process media files, recordings, or video feeds that you own, that you have been explicitly authorized by the copyright holder to repurpose, or that are published under an open Creative Commons or Public Domain license.
            </li>
            <li>
              <strong className="text-white">Discovery vs. Generation Separation:</strong> Search results, topics, and public web content marked as <code className="px-1.5 py-0.5 bg-slate-800 text-purple-300 rounded font-mono text-xs">DISCOVERY_ONLY</code> are indexed strictly for informational discovery. Entering such media into video re-encoding or clipping requires affirmative certification of your authorization.
            </li>
            <li>
              <strong className="text-white">No Automated Fair Use Defense:</strong> SignalCut does not evaluate or guarantee statutory Fair Use. Fair Use is an affirmative legal defense evaluated on a case-by-case basis by courts. You assume all legal determinations regarding your use.
            </li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed p-6 rounded-2xl bg-purple-950/20 border border-purple-900/40">
          <h2 className="text-xl font-bold text-white flex items-center space-x-2">
            <ShieldCheck className="w-5 h-5 text-purple-400" />
            <span>3. User Representations, Warranties & Indemnification</span>
          </h2>
          <p>
            <strong className="text-white">3.1 Your Representations:</strong> You represent and warrant that (a) you are the sole and exclusive owner of all rights, title, and interest in any media processed, or you have obtained valid, fully paid-up, perpetual licenses and permissions from all copyright owners, performers, and trademark holders; and (b) your use, editing, clipping, and distribution of the content does not and will not infringe, misappropriate, or violate any copyright, patent, trademark, trade secret, or rights of privacy or publicity.
          </p>
          <p>
            <strong className="text-white">3.2 Indemnification:</strong> You agree to defend, indemnify, and hold harmless SignalCut, its officers, directors, employees, contractors, and licensors from and against any and all claims, demands, liabilities, damages, losses, costs, and expenses (including reasonable attorneys fees and court costs) arising out of or in any way connected with:
          </p>
          <ul className="list-disc pl-6 space-y-1.5 text-slate-300">
            <li>Any content or media imported, rendered, edited, or distributed through your account;</li>
            <li>Any false statement, misrepresentation, or breach of your confirmation during the Rights Verification Gate;</li>
            <li>Any claim of copyright infringement, trademark dilution, or unfair competition brought by third parties;</li>
            <li>Your violation of any applicable law, rule, or third-party platform terms (including YouTube, TikTok, Instagram, or Meta).</li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">4. Credit Ledger, Billing & Consumption</h2>
          <p>
            SignalCut operates an immutable credit ledger system. Credits are required to run automated AI moment detection, audio transcription, and vertical video rendering.
          </p>
          <ul className="list-disc pl-6 space-y-2">
            <li><strong className="text-white">Reservation Mechanics:</strong> When a rendering job begins, the required credit amount is temporarily reserved. Credits are permanently committed only upon successful video delivery.</li>
            <li><strong className="text-white">Automatic Refund on System Fault:</strong> If a job fails due to an internal rendering or worker fault, reserved credits are automatically returned to your available balance.</li>
            <li><strong className="text-white">No Cash Redemption:</strong> Purchased credits possess no monetary value outside of the Service, are non-refundable once consumed, and cannot be transferred between unrelated organizations.</li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">5. Repeat Infringer Policy & Termination</h2>
          <p>
            SignalCut enforces a strict <strong className="text-white">Repeat Infringer Policy</strong> in compliance with the Digital Millennium Copyright Act (17 U.S.C. § 512). We reserve the right, in our sole discretion, to suspend or terminate accounts, without notice or refund of unused credits, if a user is determined to have repeatedly uploaded, clipped, or distributed infringing media.
          </p>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">6. Limitation of Liability</h2>
          <p>
            TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, SIGNALCUT SHALL NOT BE LIABLE FOR ANY INDIRECT, INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES, INCLUDING LOSS OF PROFITS, DATA, USE, GOODWILL, OR BUSINESS INTERRUPTION, ARISING FROM OR RELATED TO YOUR USE OF THE SERVICE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.
          </p>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">7. Contact Information</h2>
          <p>
            For legal inquiries, copyright questions, or contractual terms, please contact:
          </p>
          <div className="p-4 bg-slate-900 border border-slate-800 rounded-xl space-y-1 font-mono text-xs text-purple-300">
            <div>SignalCut Legal & Compliance Team</div>
            <div>Email: legal@signalcut.app</div>
            <div>DMCA Notices: dmca@signalcut.app</div>
          </div>
        </section>
      </main>
    </div>
  );
};

export const DMCAPolicyPage: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-purple-600">
      <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-5xl mx-auto px-6 h-16 flex items-center justify-between">
          <Link to="/" className="flex items-center space-x-2 text-slate-400 hover:text-white transition">
            <ArrowLeft className="w-4 h-4" />
            <span className="text-sm font-semibold">Back to SignalCut</span>
          </Link>
          <div className="flex items-center space-x-2">
            <ShieldCheck className="w-5 h-5 text-purple-400" />
            <span className="font-bold text-sm text-white">DMCA & Copyright Policy</span>
          </div>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-6 py-12 space-y-10">
        <div className="space-y-3">
          <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-indigo-950/60 border border-indigo-800/50 text-indigo-300 text-xs font-semibold">
            <ShieldCheck className="w-3.5 h-3.5" />
            <span>Digital Millennium Copyright Act Compliance</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight">DMCA & Copyright Policy</h1>
          <p className="text-sm text-slate-400">Notice and Takedown Procedure Under 17 U.S.C. § 512</p>
        </div>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">1. Our Commitment to Copyright Protection</h2>
          <p>
            SignalCut respects the intellectual property rights of content creators, filmmakers, broadcasters, podcasters, and rights holders. In accordance with Title II of the Digital Millennium Copyright Act of 1998 (17 U.S.C. § 512), SignalCut expeditiously investigates and responds to notifications of claimed copyright infringement regarding material hosted, processed, or distributed through our service.
          </p>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed p-6 rounded-2xl bg-slate-900 border border-slate-800">
          <h2 className="text-xl font-bold text-white flex items-center space-x-2">
            <Mail className="w-5 h-5 text-purple-400" />
            <span>2. Designated Copyright Agent Contact</span>
          </h2>
          <p>
            All formal notifications of claimed infringement should be sent to our Designated Copyright Agent:
          </p>
          <div className="p-4 bg-slate-950 border border-slate-800 rounded-xl space-y-1 font-mono text-xs text-slate-300">
            <div><strong>Designated Agent:</strong> SignalCut Copyright Compliance Officer</div>
            <div><strong>Email Address:</strong> <a href="mailto:dmca@signalcut.app" className="text-purple-400 hover:underline">dmca@signalcut.app</a></div>
            <div><strong>Subject Line:</strong> DMCA Copyright Infringement Notice</div>
            <div><strong>Address:</strong> SignalCut Legal Operations, Suite 400, Wilmington, DE 19801</div>
          </div>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">3. How to Submit a Valid DMCA Notice</h2>
          <p>
            To be effective under 17 U.S.C. § 512(c)(3), your notification must be in writing and must include the following statutory elements:
          </p>
          <ol className="list-decimal pl-6 space-y-2">
            <li>A physical or electronic signature of a person authorized to act on behalf of the copyright owner;</li>
            <li>Identification of the copyrighted work claimed to have been infringed (e.g., URL to original video, title, or registration number);</li>
            <li>Identification of the specific material that is claimed to be infringing and that is to be removed or disabled, including sufficient information to enable SignalCut to locate the material (such as the clip URL or unique identifier);</li>
            <li>Your contact information, including your full legal name, mailing address, telephone number, and email address;</li>
            <li>A statement that you have a good-faith belief that use of the material in the manner complained of is not authorized by the copyright owner, its agent, or the law;</li>
            <li>A statement that the information in the notification is accurate, and under penalty of perjury, that you are authorized to act on behalf of the owner of an exclusive right that is allegedly infringed.</li>
          </ol>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">4. Expedited Takedown & Response Process</h2>
          <p>
            Upon receipt of a valid and complete DMCA notice, SignalCut will:
          </p>
          <ul className="list-disc pl-6 space-y-2">
            <li>Promptly disable access to or permanently remove the identified infringing content from our cloud storage and CDN nodes;</li>
            <li>Notify the user who imported or generated the content, informing them of the removal and providing a copy of the notice;</li>
            <li>Log the event in our security and rights audit ledger.</li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed p-6 rounded-2xl bg-rose-950/20 border border-rose-900/40">
          <h2 className="text-xl font-bold text-white flex items-center space-x-2">
            <AlertTriangle className="w-5 h-5 text-rose-400" />
            <span>5. Repeat Infringer Policy (Two-Strike Rule)</span>
          </h2>
          <p>
            SignalCut maintains an active policy providing for the termination in appropriate circumstances of subscribers and account holders who are repeat infringers:
          </p>
          <ul className="list-disc pl-6 space-y-1.5 text-slate-300">
            <li><strong className="text-white">Strike One:</strong> The offending clip is permanently removed, and the account receives a formal written copyright strike warning.</li>
            <li><strong className="text-white">Strike Two:</strong> If an account is the subject of a second substantiated DMCA takedown notice within a 12-month period, the account is <strong className="text-rose-400">permanently terminated</strong>, and all remaining credits are forfeited.</li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">6. Counter-Notification Procedure</h2>
          <p>
            If you believe that your content was removed or disabled as a result of mistake or misidentification (e.g., you possess a valid license or client release), you may submit a written Counter-Notification to our Designated Agent at <code className="text-purple-300">dmca@signalcut.app</code> in compliance with 17 U.S.C. § 512(g).
          </p>
        </section>
      </main>
    </div>
  );
};

export const PrivacyPolicyPage: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-purple-600">
      <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-5xl mx-auto px-6 h-16 flex items-center justify-between">
          <Link to="/" className="flex items-center space-x-2 text-slate-400 hover:text-white transition">
            <ArrowLeft className="w-4 h-4" />
            <span className="text-sm font-semibold">Back to SignalCut</span>
          </Link>
          <div className="flex items-center space-x-2">
            <FileText className="w-5 h-5 text-purple-400" />
            <span className="font-bold text-sm text-white">Privacy Policy</span>
          </div>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-6 py-12 space-y-10">
        <div className="space-y-3">
          <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-purple-950/60 border border-purple-800/50 text-purple-300 text-xs font-semibold">
            <FileText className="w-3.5 h-3.5" />
            <span>Data Protection & Privacy</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight">Privacy Policy</h1>
          <p className="text-sm text-slate-400">GDPR & CCPA Compliant • Last Updated: October 5, 2026</p>
        </div>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">1. Information We Collect</h2>
          <p>
            When you register, import media, or generate video clips with SignalCut, we collect:
          </p>
          <ul className="list-disc pl-6 space-y-2">
            <li><strong className="text-white">Account Information:</strong> Your name, email address, password hash (encrypted via BCrypt), and organization name.</li>
            <li><strong className="text-white">Media & Transcripts:</strong> Audio transcripts, speech timestamps, generated captions, video aspect ratio parameters, and rendered MP4 files.</li>
            <li><strong className="text-white">Audit & Rights Records:</strong> Timestamps, IP addresses, and affirmative confirmation statements submitted through the Rights Verification Gate.</li>
            <li><strong className="text-white">Encrypted OAuth Tokens:</strong> Access tokens for connected YouTube, TikTok, Instagram, or LinkedIn accounts are encrypted at rest using AES-256 and never exposed in client API responses.</li>
          </ul>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">2. How We Use AI Models (Google Gemini)</h2>
          <p>
            To provide moment scoring, hook detection, and viral copywriting, transcript segments are processed through Google Gemini AI models. We do not use your private workspace transcripts or proprietary media to train public foundation models. Data transmitted for inference is processed according to enterprise confidentiality standards.
          </p>
        </section>

        <section className="space-y-4 text-sm text-slate-300 leading-relaxed">
          <h2 className="text-xl font-bold text-white">3. Data Retention & Deletion Rights</h2>
          <p>
            You have the right at any time to request export or complete deletion of your account, media files, and credit transactions in compliance with the General Data Protection Regulation (GDPR) and California Consumer Privacy Act (CCPA). Contact <code className="text-purple-300">privacy@signalcut.app</code> to initiate an expedited data erasure request.
          </p>
        </section>
      </main>
    </div>
  );
};
