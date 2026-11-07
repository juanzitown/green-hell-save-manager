import { HardDriveDownload } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { useProfiles } from '../contexts/ProfilesContext';

function latestLabel(modified?: number) {
  return modified ? new Date(modified).toLocaleString() : 'No saves yet';
}

export function AccountPage() {
  const { steamAccounts, selectSteamAccount } = useProfiles();
  return <div className="content-wrap account-page">
    <div className="eyebrow">STEAM ACCOUNTS</div>
    <h1>Choose an account</h1>
    <p className="account-intro">Select a Steam account to view and manage its Green Hell saves.</p>
    <div className="account-grid">{steamAccounts.map((account) => {
      const latest = account.files[0];
      return <article className="account-card" key={account.account_id}>
        <div className="account-card-icon"><HardDriveDownload /></div>
        <div className="account-card-copy"><span>ACCOUNT ID</span><strong>{account.account_id}</strong><small>{account.files.length} save{account.files.length === 1 ? '' : 's'}</small><small>Latest: {latest ? `${latest.name} · ${latestLabel(latest.modified)}` : 'No saves yet'}</small></div>
        <Button label="Enter" size="sm" onClick={() => void selectSteamAccount(account.account_id)} />
      </article>;
    })}</div>
  </div>;
}
