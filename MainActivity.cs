using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;
using System.IO.Compression;

namespace LauncherSV;

[Activity(Label = "Mi Servidor", MainLauncher = true)]
public class MainActivity : Activity
{
    // ====== CONFIGURA ESTO ======
    const string URL_VERSION = "https://tuhosting.com/version.txt";
    const string URL_ZIP     = "https://tuhosting.com/resources.zip";
    const string PAQUETE     = "com.paquete.del.juego";
    const string IP          = "123.45.67.89";
    const int    PUERTO      = 7777;
    // ============================

    TextView estado;
    EditText nick;
    Button jugar;

    string CarpetaJuego => System.IO.Path.Combine(
        Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Android/data", PAQUETE, "files");

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
        layout.SetPadding(40, 80, 40, 40);

        estado = new TextView(this) { Text = "Listo" };
        nick = new EditText(this) { Hint = "Nombre_Apellido" };
        jugar = new Button(this) { Text = "JUGAR" };
        var actualizar = new Button(this) { Text = "ACTUALIZAR ARCHIVOS" };

        layout.AddView(estado);
        layout.AddView(nick);
        layout.AddView(actualizar);
        layout.AddView(jugar);
        SetContentView(layout);

        nick.Text = Preferences().GetString("nick", "");

        actualizar.Click += async (s, e) => await Actualizar();
        jugar.Click += (s, e) => Jugar();

        PedirPermiso();
    }

    ISharedPreferences Preferences() => GetSharedPreferences("launcher", FileCreationMode.Private);

    void PedirPermiso()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R && !Android.OS.Environment.IsExternalStorageManager)
        {
            var i = new Intent(Settings.ActionManageAppAllFilesAccessPermission,
                Android.Net.Uri.Parse("package:" + PackageName));
            StartActivity(i);
        }
    }

    async Task Actualizar()
    {
        try
        {
            using var http = new HttpClient();
            estado.Text = "Buscando actualizaciones...";

            var remota = (await http.GetStringAsync(URL_VERSION)).Trim();
            var local = Preferences().GetString("version", "0");
            if (remota == local) { estado.Text = "Ya estas actualizado"; return; }

            estado.Text = "Descargando...";
            var zip = System.IO.Path.Combine(CacheDir.AbsolutePath, "resources.zip");
            var bytes = await http.GetByteArrayAsync(URL_ZIP);
            await File.WriteAllBytesAsync(zip, bytes);

            estado.Text = "Instalando...";
            Directory.CreateDirectory(CarpetaJuego);
            ZipFile.ExtractToDirectory(zip, CarpetaJuego, true);
            File.Delete(zip);

            Preferences().Edit().PutString("version", remota).Apply();
            estado.Text = "Actualizado a la version " + remota;
        }
        catch (Exception ex)
        {
            estado.Text = "Error: " + ex.Message;
        }
    }

    void Jugar()
    {
        if (string.IsNullOrWhiteSpace(nick.Text))
        {
            Toast.MakeText(this, "Escribe tu nick", ToastLength.Short).Show();
            return;
        }
        Preferences().Edit().PutString("nick", nick.Text).Apply();

        var intent = PackageManager.GetLaunchIntentForPackage(PAQUETE);
        if (intent == null)
        {
            Toast.MakeText(this, "Juego no instalado", ToastLength.Long).Show();
            return;
        }

        intent.PutExtra("nickname", nick.Text);
        intent.PutExtra("ip", IP);
