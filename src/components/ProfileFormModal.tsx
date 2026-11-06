import { useState } from 'react';
import { X } from 'lucide-react';

type Props = { onClose: () => void; onSubmit: (name: string, icon: string) => void };

export function ProfileFormModal({ onClose, onSubmit }: Props) {
  const [name, setName] = useState('');
  const [icon, setIcon] = useState('🌱');
  const submit = () => { if (name.trim()) onSubmit(name.trim(), icon); };
  return <div className="modal-backdrop"><div className="modal">
    <button className="close-button" onClick={onClose}><X size={18} /></button><div className="eyebrow">NEW PROFILE</div><h2>Create a profile</h2><p>Each profile has its own isolated Green Hell saves.</p>
    <div className="profile-form-row"><button className="form-icon" onClick={() => setIcon(icon === '🌱' ? '🌿' : '🌱')}>{icon}</button><input autoFocus value={name} onChange={(event) => setName(event.target.value)} onKeyDown={(event) => event.key === 'Enter' && submit()} placeholder="Profile name" /></div>
    <div className="modal-actions"><button className="outline-button" onClick={onClose}>Cancel</button><button className="primary-button" onClick={submit}>Create profile</button></div>
  </div></div>;
}
