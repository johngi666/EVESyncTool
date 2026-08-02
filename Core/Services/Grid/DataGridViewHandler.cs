using EVESyncTool.Core.Services.Backup;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Sync;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services.Grid
{
    public class DataGridViewHandler
    {
        private readonly BackupService _backupService;
        private readonly SyncService _syncService;
        private readonly Func<string> _getCurrentFolder;
        private readonly Func<List<UserFileItem>> _getUserFileItems;
        private readonly Func<List<CharacterFileItem>> _getCharFileItems;
        private readonly Func<List<BackupItem>> _getBackupItems;
        private readonly Action<UserFileItem> _showUserSyncDialog;
        private readonly Action<CharacterFileItem> _showCharSyncDialog;

        public DataGridViewHandler(
            BackupService backupService,
            SyncService syncService,
            Func<string> getCurrentFolder,
            Func<List<UserFileItem>> getUserFileItems,
            Func<List<CharacterFileItem>> getCharFileItems,
            Func<List<BackupItem>> getBackupItems,
            Action<UserFileItem> showUserSyncDialog,
            Action<CharacterFileItem> showCharSyncDialog)
        {
            _backupService = backupService;
            _syncService = syncService;
            _getCurrentFolder = getCurrentFolder;
            _getUserFileItems = getUserFileItems;
            _getCharFileItems = getCharFileItems;
            _getBackupItems = getBackupItems;
            _showUserSyncDialog = showUserSyncDialog;
            _showCharSyncDialog = showCharSyncDialog;
        }

        public void OnUserFileCellClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

            var items = _getUserFileItems?.Invoke();
            if (items == null || e.RowIndex >= items.Count) return;

            var item = items[e.RowIndex];

            if (e.ColumnIndex == 2)
            {
                _backupService.BackupSingleFile(item.FilePath);
            }
            else if (e.ColumnIndex == 3)
            {
                _showUserSyncDialog?.Invoke(item);
            }
        }

        public void OnCharFileCellClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 3) return;

            var items = _getCharFileItems?.Invoke();
            if (items == null || e.RowIndex >= items.Count) return;

            var item = items[e.RowIndex];

            if (e.ColumnIndex == 3)
            {
                _backupService.BackupSingleFile(item.FilePath);
            }
            else if (e.ColumnIndex == 4)
            {
                _showCharSyncDialog?.Invoke(item);
            }
        }

        public void OnBackupCellClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

            var items = _getBackupItems?.Invoke();
            if (items == null || e.RowIndex >= items.Count) return;

            var item = items[e.RowIndex];

            if (e.ColumnIndex == 2)
            {
                _backupService.ShowBackupInExplorer(item);
            }
            else if (e.ColumnIndex == 3)
            {
                _backupService.RestoreBackup(item);
            }
            else if (e.ColumnIndex == 4)
            {
                _backupService.DeleteBackup(item);
            }
        }
    }
}