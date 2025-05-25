using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RedeMonitor
{
    static class Program
    {
        static bool IsConexaoEstranha(string linha)
        {
            if (Regex.IsMatch(linha, @"\s185\."))
                return true;
            var match = Regex.Match(linha, @":(\d{4,5})\s");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int porta) && porta > 9000)
                return true;
            return false;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form form = new Form
            {
                Text = "Monitor de Rede slycooperbr80",
                ClientSize = new Size(1280, 770),
                BackColor = Color.RoyalBlue,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false
            };

            Label label = new Label
            {
                Text = "feito por slycooperbr80",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.Transparent
            };
            form.Controls.Add(label);

            // Botão para listar interfaces
            Button btnListar = new Button
            {
                Text = "Listar Interfaces",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Size = new Size(200, 45),
                Location = new Point(20, 20),
                BackColor = Color.LightSteelBlue,
                ForeColor = Color.Black
            };
            btnListar.Click += async (s, e) =>
            {
                try
                {
                    string interfaces = await Task.Run(() =>
                    {
                        // Comando para listar interfaces no Windows
                        ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface show interface")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process p = Process.Start(psi))
                        {
                            return p.StandardOutput.ReadToEnd();
                        }
                    });

                    MessageBox.Show(interfaces, "Interfaces de Rede", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao listar interfaces: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            form.Controls.Add(btnListar);

            System.Windows.Forms.Timer monitorTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // 5 segundos
            };

            monitorTimer.Tick += async (s, e) =>
            {
                try
                {
                    string netstat = await Task.Run(() =>
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("netstat", "-n")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process p = Process.Start(psi))
                        {
                            return p.StandardOutput.ReadToEnd();
                        }
                    });
                    var linhas = netstat.Split('\n');
                    foreach (var linha in linhas)
                    {
                        if (IsConexaoEstranha(linha))
                        {
                            monitorTimer.Stop();
                            MessageBox.Show("Conexão de rede estranha detectada!\n\n" + linha.Trim(),
                                "ALERTA DE REDE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            monitorTimer.Start();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erro ao monitorar conexões: " + ex.Message);
                }
            };

            form.Shown += (s, e) =>
            {
                monitorTimer.Start();
            };

            Application.Run(form);
        }
    }
}