using System.Configuration;

namespace FTFlash;

public partial class Form1 : Form
{
    FtdiSharp.Protocols.SPI? SpiComm = null;

    public Form1()
    {
        InitializeComponent();
        Disconnect();
        btnConnect_Click(this, EventArgs.Empty);
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        if (SpiComm is null)
        {
            ScanAndConnect();
        }
        else
        {
            Disconnect();
        }
    }

    void Disconnect()
    {
        SpiComm?.Close();
        SpiComm = null;
        lblConnection.Text = "Disconnected";
        btnConnect.Text = "Scan for FT232H Devices";
    }

    void ScanAndConnect()
    {
        System.Diagnostics.Debug.WriteLine("Scanning for FTDI devices...");
        var devices = FtdiSharp.FtdiDevices.Scan();
        foreach (var device in devices)
        {
            System.Diagnostics.Debug.WriteLine($"Found: {device}");
            if (device.Type == "232H")
            {
                lblConnection.Text = $"FT232H ({device.ID}) connecting...";
                SpiComm = new(device, spiMode: 0, slowDownFactor: 500);
                lblConnection.Text = $"FT232H ({device.ID}) connected";
                btnConnect.Text = "Disconnect";
                return;
            }
        }

        lblConnection.Text = $"No FT232H found...";
    }

    private void btnReadIDs_Click(object sender, EventArgs e)
    {
        if (SpiComm is null)
            return;

        SpiComm.CsLow();
        SpiComm.CsHigh();

        SpiComm.CsLow();
        foreach (byte b in new byte[] { 0x90, 0, 0, 0 })
            SpiComm.Write(b);
        byte[] ids1 = SpiComm.ReadWrite(new byte[] { 0, 0 });
        SpiComm.CsHigh();

        lblID1.Text = $"Manufacturer ID: 0x{ids1[0]:X}";
        lblID2.Text = $"Device ID: 0x{ids1[1]:X}";

        SpiComm.CsLow();
        foreach (byte b in new byte[] { 0x4B, 0, 0, 0, 0 })
            SpiComm.Write(b);
        byte[] ids2 = SpiComm.ReadBytes(8);
        SpiComm.CsHigh();

        lblID3.Text = "Device ID: " + string.Join("", ids2.Select(x => $"{x:X2}")).ToString();
    }

    private void btnWritePage_Click(object sender, EventArgs e)
    {
        if (SpiComm is null)
            return;

        byte firstByte = (byte)Random.Shared.Next(256);
        lblWrite.Text = $"First byte: {firstByte}";

        byte[] bytes = Enumerable.Range(0, 256).Select(x => (byte)(firstByte + x)).ToArray();

        SpiComm.CsLow();
        SpiComm.Write(6);
        SpiComm.CsHigh();

        SpiComm.CsLow();
        foreach (byte b in new byte[] { 2, 0, 0, 0 })
            SpiComm.Write(b);
        foreach (byte b in bytes)
            SpiComm.Write(b);
        SpiComm.CsHigh();
    }

    private void btnReadPage_Click(object sender, EventArgs e)
    {
        if (SpiComm is null)
            return;

        SpiComm.CsLow();
        foreach (byte b in new byte[] { 3, 0, 0, 0 })
            SpiComm.Write(b);
        System.Threading.Thread.Sleep(1);
        byte[] bytes = SpiComm.ReadBytes(256);
        SpiComm.CsHigh();

        richTextBox1.Text = string.Join(", ", bytes.Select(x => $"{x}")).ToString();
    }

    private void btnErase_Click(object sender, EventArgs e)
    {
        if (SpiComm is null)
            return;

        SpiComm.CsLow();
        SpiComm.Write(6);
        SpiComm.CsHigh();

        SpiComm.CsLow();
        SpiComm.Write(0xC7);
        SpiComm.CsHigh();
    }
}
