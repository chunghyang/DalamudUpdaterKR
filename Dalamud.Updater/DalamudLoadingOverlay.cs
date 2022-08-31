using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using XIVLauncher.Common.Dalamud;

namespace Dalamud.Updater
{
    internal class DalamudLoadingOverlay : IDalamudLoadingOverlay
    {
        public delegate void progressBar(int value);
        public delegate void statusLabel(string value);
        public delegate void setVisible(bool value);
        public event progressBar OnProgressBar;
        public event statusLabel OnStatusLabel;
        public event setVisible OnSetVisible;
        public DalamudLoadingOverlay(FormMain form)
        {
            //this.progressBar = form.toolStripProgressBar1;
            //this.statusLabel = form.toolStripStatusLabel1;
        }
        public DalamudLoadingOverlay()
        {
            //this.progressBar = form.toolStripProgressBar1;
            //this.statusLabel = form.toolStripStatusLabel1;
        }
        public void ReportProgress(long? size, long downloaded, double? progress)
        {
            size = size ?? 0;
            progress = progress ?? 0;
            OnProgressBar?.Invoke((int)progress.Value);
        }

        public void SetInvisible()
        {
            OnSetVisible?.Invoke(false);
        }

        public void SetStep(IDalamudLoadingOverlay.DalamudUpdateStep progress)
        {
            switch (progress)
            {
                // 文本太长会一个字都不显示
                case IDalamudLoadingOverlay.DalamudUpdateStep.Dalamud:
                    OnStatusLabel?.Invoke("Core Update");
                    break;

                case IDalamudLoadingOverlay.DalamudUpdateStep.Assets:
                    OnStatusLabel?.Invoke("Resource Update");
                    break;

                case IDalamudLoadingOverlay.DalamudUpdateStep.Runtime:
                    OnStatusLabel?.Invoke("Library Update");
                    break;

                case IDalamudLoadingOverlay.DalamudUpdateStep.Unavailable:
                    OnStatusLabel?.Invoke("Update Fail");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(progress), progress, null);
            }
        }

        public void SetVisible()
        {
            OnSetVisible?.Invoke(true);
        }
    }
}
