import { useProfiles } from '../contexts/ProfilesContext';
import { ProfileFormModal } from './ProfileFormModal';

export function ProfileDialogs() {
  const { profiles, dialog, closeProfileDialog, createProfile, updateProfile } = useProfiles();
  if (!dialog) return null;
  if (dialog.mode === 'create') return <ProfileFormModal onClose={closeProfileDialog} onSubmit={createProfile} />;
  const profile = profiles.find((item) => item.id === dialog.profileId);
  return profile ? <ProfileFormModal initialName={profile.name} onClose={closeProfileDialog} onSubmit={updateProfile} /> : null;
}
