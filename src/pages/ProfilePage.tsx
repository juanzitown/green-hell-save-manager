import { useMemo, useRef, useState } from "react";
import { ExternalLink, HardDriveDownload, Search, Upload } from "lucide-react";
import { invoke } from "@tauri-apps/api/core";
import { toast } from "sonner";
import { Avatar } from "../components/ui/Avatar";
import { Button } from "../components/ui/Button";
import { IconButton } from "../components/ui/IconButton";
import { useProfiles } from "../contexts/ProfilesContext";
import type { SaveFile } from "../types";

function timeAgo(timestamp: number) {
  const minutes = Math.max(1, Math.floor((Date.now() - timestamp) / 60000));
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  return hours < 24 ? `${hours}h ago` : `${Math.floor(hours / 24)}d ago`;
}

export function ProfilePage() {
  const { selected: profile, refreshProfile, openEditProfile } = useProfiles();
  const [query, setQuery] = useState("");
  const fileInput = useRef<HTMLInputElement>(null);
  const saves = useMemo(
    () =>
      profile.saves
        .filter((save) => save.name.toLowerCase().includes(query.toLowerCase()))
        .sort((a, b) => b.modified - a.modified),
    [profile.saves, query],
  );

  async function importFiles(files: FileList | null) {
    if (!files) return;
    let uploaded = 0;
    for (const file of Array.from(files)) {
      if (!file.name.toLowerCase().endsWith(".sav")) continue;
      try {
        const bytes = Array.from(new Uint8Array(await file.arrayBuffer()));
        await invoke("upload_save_to_profile", {
          profileId: profile.id,
          name: file.name,
          bytes,
        });
        uploaded += 1;
      } catch (error) {
        console.error("Save upload failed", error);
        toast.error(`Could not upload ${file.name}`);
      }
    }
    await refreshProfile(profile.id);
    if (uploaded)
      toast.success(
        uploaded === 1
          ? "Save imported successfully"
          : `${uploaded} saves imported successfully`,
      );
  }

  function downloadSave(save: SaveFile) {
    void invoke<string>("export_save_file", { path: save.id })
      .then(() => toast.success("Save downloaded successfully"))
      .catch(() => toast.error("Could not download this save file"));
  }

  function copySave(save: SaveFile) {
    void invoke("copy_profile_save_to_steam", {
      profileId: profile.id,
      name: save.name,
    })
      .then(() => toast.success("Save copied successfully"))
      .catch(() => toast.error(`Could not copy ${save.name} to Steam`));
  }

  function openFolder() {
    if (profile.folderPath)
      void invoke("open_folder", { path: profile.folderPath }).catch(() =>
        toast.error("Could not open the save folder"),
      );
  }

  return (
    <div className="content-wrap">
      <div className="profile-heading">
        {profile.id === "my" && <Avatar size="lg" />}
        <div>
          <div className="eyebrow">
            {profile.id === "my" ? "MY PROFILE" : "BACKUP PROFILE"}
          </div>
          <h1>{profile.name}</h1>
          <p>
            {profile.saves.length} saved file
            {profile.saves.length === 1 ? "" : "s"}
          </p>
        </div>
        <div className="more-button">
          <IconButton
            icon="pencil"
            tooltip="Edit profile"
            size="sm"
            onClick={() => openEditProfile(profile.id)}
          />
        </div>
      </div>
      <div className="toolbar">
        <div className="search-box">
          <Search size={16} />
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search saves"
          />
        </div>
        <Button
          label="Import saves"
          icon="upload"
          onClick={() => fileInput.current?.click()}
        />
        <input
          ref={fileInput}
          onChange={(event) => {
            void importFiles(event.target.files);
            event.target.value = "";
          }}
          type="file"
          multiple
          accept=".sav"
          hidden
        />
      </div>
      <div className="folder-status">
        <HardDriveDownload size={14} />
        {profile.folderPath ? (
          <>
            <button
              className="folder-link"
              onClick={openFolder}
              title="Open in file explorer"
            >
              {profile.folderPath}
              <ExternalLink size={12} />
            </button>
            <span>
              · {profile.saves.length} save
              {profile.saves.length === 1 ? "" : "s"}
            </span>
          </>
        ) : (
          <span>Save folder not found</span>
        )}
      </div>
      <div className="save-area">
        {saves.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">
              <Upload size={22} />
            </div>
            <h2>No saves yet</h2>
            <p>
              Import or drag Green Hell <code>.sav</code> files into this
              profile.
            </p>
            <Button
              label="Import saves"
              icon="upload"
              variant="outline"
              onClick={() => fileInput.current?.click()}
            />
          </div>
        ) : (
          <div className="file-grid">
            {saves.map((save) => (
              <article className="file-card" key={save.id}>
                <div className="file-icon">
                  <span>.sav</span>
                </div>
                <div className="file-info">
                  <strong>{save.name}</strong>
                  <span>
                    {save.size} · {timeAgo(save.modified)}
                  </span>
                  <small>{save.source}</small>
                </div>
                <div className="file-actions">
                  {profile.id !== "my" && (
                    <IconButton
                      icon="hardDriveDownload"
                      tooltip="Copy save to Steam"
                      size="sm"
                      onClick={() => copySave(save)}
                    />
                  )}
                  <IconButton
                    icon="download"
                    tooltip="Download save"
                    size="sm"
                    onClick={() => downloadSave(save)}
                  />
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
      <div className="drop-hint">
        <Upload size={15} /> Drop files anywhere in the window to add them to{" "}
        <strong>{profile.name}</strong>
      </div>
    </div>
  );
}
