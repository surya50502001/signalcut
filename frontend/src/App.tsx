import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { Sidebar } from './components/Sidebar';
import { PaymentModal } from './components/PaymentModal';

import { LandingPage } from './pages/LandingPage';
import { SearchPage } from './pages/SearchPage';
import { DiscoveriesPage } from './pages/DiscoveriesPage';
import { MomentsPage } from './pages/MomentsPage';
import { EditorPage } from './pages/EditorPage';
import { ClipsPage, PublishingPage } from './pages/ClipsAndPublishingPages';
import { CreditsPage, AnalyticsPage } from './pages/CreditsAndAnalyticsPages';
import { AdminPage, ProjectsPage } from './pages/AdminAndProjectsPages';
import { SettingsPage, LoginPage, SignupPage } from './pages/SettingsAndAuthPages';
import { DashboardPage, PricingPage } from './pages/DashboardAndPricingPages';
import { TermsOfServicePage, DMCAPolicyPage, PrivacyPolicyPage } from './pages/LegalPages';

const AppLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  return (
    <div className="min-h-screen flex flex-col bg-slate-950 text-slate-100">
      <Navbar onOpenPayment={() => setShowPaymentModal(true)} />
      <div className="flex-1 flex overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto">
          {children}
        </main>
      </div>
      <PaymentModal
        isOpen={showPaymentModal}
        onClose={() => setShowPaymentModal(false)}
      />
    </div>
  );
};

export function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          {/* Public Pages */}
          <Route path="/" element={<LandingPage />} />
          <Route path="/pricing" element={<AppLayout><PricingPage /></AppLayout>} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignupPage />} />
          <Route path="/terms" element={<TermsOfServicePage />} />
          <Route path="/privacy" element={<PrivacyPolicyPage />} />
          <Route path="/dmca" element={<DMCAPolicyPage />} />

          {/* Authenticated Dashboard Pages */}
          <Route path="/dashboard" element={<AppLayout><DashboardPage /></AppLayout>} />
          <Route path="/search" element={<AppLayout><SearchPage /></AppLayout>} />
          <Route path="/discoveries" element={<AppLayout><DiscoveriesPage /></AppLayout>} />
          <Route path="/projects" element={<AppLayout><ProjectsPage /></AppLayout>} />
          <Route path="/moments" element={<AppLayout><MomentsPage /></AppLayout>} />
          <Route path="/clips" element={<AppLayout><ClipsPage /></AppLayout>} />
          <Route path="/editor" element={<AppLayout><EditorPage /></AppLayout>} />
          <Route path="/publishing" element={<AppLayout><PublishingPage /></AppLayout>} />
          <Route path="/credits" element={<AppLayout><CreditsPage /></AppLayout>} />
          <Route path="/analytics" element={<AppLayout><AnalyticsPage /></AppLayout>} />
          <Route path="/admin" element={<AppLayout><AdminPage /></AppLayout>} />
          <Route path="/settings" element={<AppLayout><SettingsPage /></AppLayout>} />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
