import { useState } from 'react';
import { Button } from './ui/Button';
import { IconButton } from './ui/IconButton';

type Props = { onClose: () => void; onSubmit: (name: string) => void; initialName?: string };

export function ProfileFormModal({ onClose, onSubmit, initialName = '' }: Props) {
  const [name, setName] = useState(initialName);
  const editing = Boolean(initialName);
  const submit = () => { if (name.trim()) onSubmit(name.trim()); };
  return <div className="modal-backdrop"><div className="modal">
    <div className="close-button"><IconButton icon="close" tooltip="Close" size="sm" onClick={onClose} /></div><div className="eyebrow">{editing ? 'EDIT PROFILE' : 'NEW PROFILE'}</div><h2>{editing ? 'Edit profile' : 'Create a profile'}</h2><p>{editing ? 'Update this profile name.' : 'Each profile has its own isolated Green Hell saves.'}</p>
    <div className="profile-form-row"><input autoFocus value={name} onChange={(event) => setName(event.target.value)} onKeyDown={(event) => event.key === 'Enter' && submit()} placeholder="Profile name" /></div>
    <div className="modal-actions"><Button label="Cancel" variant="outline" onClick={onClose} /><Button label={editing ? 'Save changes' : 'Create profile'} onClick={submit} disabled={!name.trim()} /></div>
  </div></div>;
}
