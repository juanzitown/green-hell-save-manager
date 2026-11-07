import { Gamepad2 } from "lucide-react";
import { Toaster } from "sonner";
import { ProfileDialogs } from "./components/ProfileDialogs";
import { VerticalMenu } from "./components/VerticalMenu";
import { ProfilePage } from "./pages/ProfilePage";

export function App() {
  return (
    <>
      <Toaster position="top-center" theme="dark" richColors closeButton />
      <div className="app-shell">
        <VerticalMenu />
        <main className="main-content">
          <header className="topbar">
            <div className="brand-compact">
              <div className="brand-mark">
                <Gamepad2 size={19} />
              </div>
              <strong>Green Hell</strong>
            </div>
          </header>
          <ProfilePage />
        </main>
        <ProfileDialogs />
      </div>
    </>
  );
}
