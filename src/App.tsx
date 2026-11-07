import { Gamepad2 } from 'lucide-react';
import { Toaster } from 'sonner';
import { ProfileDialogs } from './components/ProfileDialogs';
import { VerticalMenu } from './components/VerticalMenu';
import { IconButton } from './components/ui/IconButton';
import { useProfiles } from './contexts/ProfilesContext';
import { AccountPage } from './pages/AccountPage';
import { ProfilePage } from './pages/ProfilePage';

export function App() {
  const { steamAccounts, selectedSteamAccount, showAccountPicker, openAccountPicker } = useProfiles();
  return <>
    <Toaster position="top-center" theme="dark" richColors closeButton />
    <div className="app-shell">
      {!showAccountPicker && <VerticalMenu />}
      <main className="main-content">
        <header className="topbar"><div className="brand-with-account">{!showAccountPicker && steamAccounts.length > 0 && <IconButton icon="arrowLeft" tooltip="View Steam accounts" size="sm" onClick={openAccountPicker} />}<div className="brand-compact"><div className="brand-mark"><Gamepad2 size={19} /></div><div className="brand-copy"><strong>Green Hell</strong>{selectedSteamAccount && !showAccountPicker && <small>Steam ID {selectedSteamAccount.account_id}</small>}</div></div></div></header>
        {showAccountPicker ? <AccountPage /> : <ProfilePage />}
      </main>
      {!showAccountPicker && <ProfileDialogs />}
    </div>
  </>;
}
