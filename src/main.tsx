import React from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './styles.css';
import './components.css';
import './components/ui/controls.css';
import { ProfilesProvider } from './contexts/ProfilesContext';

createRoot(document.getElementById('root')!).render(
  <React.StrictMode><ProfilesProvider><App /></ProfilesProvider></React.StrictMode>,
);
