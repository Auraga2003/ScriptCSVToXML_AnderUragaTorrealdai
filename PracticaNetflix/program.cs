using System.Globalization;
using System.Text;
using System.Xml.Linq;

class Program
{
    
    static string rutaCsv = "NetflixOriginals.csv";
    static string rutaXml = "NetflixOriginals.xml";

    
    const int GENERO = 1, PREMIERE = 2, IDIOMA = 5;

    static void Main()
    {
        int opcion;
        do
        {
            Console.WriteLine("\n=== MENÚ PRINCIPAL ===");
            Console.WriteLine("1. Elegir ruta del CSV (actual: " + rutaCsv + ")");
            Console.WriteLine("2. Elegir ruta del XML (actual: " + rutaXml + ")");
            Console.WriteLine("3. De CSV a XML");
            Console.WriteLine("4. De XML a CSV");
            Console.WriteLine("0. Salir");
            Console.Write("Opción: ");
            int.TryParse(Console.ReadLine(), out opcion);

            switch (opcion)
            {
                case 1:
                    Console.Write("Ruta del CSV: ");
                    rutaCsv = Console.ReadLine() ?? string.Empty;
                    break;
                case 2:
                    Console.Write("Ruta del XML: ");
                    rutaXml = Console.ReadLine() ?? string.Empty;
                    break;
                case 3:
                    Convertir(true);
                    break;
                case 4:
                    Convertir(false);
                    break;
            }
        } while (opcion != 0);
    }

    
    static void Convertir(bool csvAXml)
    {
        string origen;
        string destino;

        if (csvAXml)
        {
            origen = rutaCsv;
            destino = rutaXml;
        }
        else
        {
            
            origen = rutaXml;
            destino = Path.ChangeExtension(rutaXml, ".csv");
        }

        if (!File.Exists(origen))
        {
            Console.WriteLine("No existe el fichero: " + origen);
            return;
        }

        List<string[]> peliculas;

        if (csvAXml)
        {
            peliculas = LeerCsv(origen);
        }
        else
        {
            peliculas = LeerXml(origen);
        }

        Console.WriteLine("\n1. Exportar todo el documento");
        Console.WriteLine("2. Exportar por género");
        Console.WriteLine("3. Exportar por año");
        Console.WriteLine("4. Exportar por lenguaje");
        Console.Write("Opción: ");
        int.TryParse(Console.ReadLine(), out int opcion);

        switch (opcion)
        {
            case 1:
                break;
            case 2:
                string genero = ElegirValor(peliculas, GENERO);
                peliculas = peliculas.Where(p => p[GENERO] == genero).ToList();
                break;
            case 3:
                Console.Write("Año: ");
                int.TryParse(Console.ReadLine(), out int anio);
                peliculas = peliculas.Where(p => ObtenerAnio(p[PREMIERE]) >= anio).ToList();
                break;
            case 4:
                string idioma = ElegirValor(peliculas, IDIOMA);
                peliculas = peliculas.Where(p => p[IDIOMA] == idioma).ToList();
                break;
            default:
                Console.WriteLine("Opción no válida.");
                return;
        }

        if (csvAXml)
        {
            GuardarXml(peliculas, destino);
        }
        else
        {
            GuardarCsv(peliculas, destino);
        }

        Console.WriteLine(peliculas.Count + " películas exportadas a " + destino);
    }

     
    static string ElegirValor(List<string[]> peliculas, int campo)
    {
        List<string> valores = peliculas.Select(p => p[campo]).Distinct().OrderBy(v => v).ToList();
        for (int i = 0; i < valores.Count; i++)
            Console.WriteLine((i + 1) + ". " + valores[i]);

        int n;
        do
        {
            Console.Write("Elige un número: ");
        } while (!int.TryParse(Console.ReadLine(), out n) || n < 1 || n > valores.Count);

        return valores[n - 1];
    }

    static int ObtenerAnio(string fecha)
    {
        if (DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            return d.Year;
        return 0;
    }


    static List<string[]> LeerCsv(string ruta)
    {
        List<string[]> lista = new List<string[]>();
        string[] lineas = File.ReadAllLines(ruta, Encoding.UTF8);

        for (int i = 1; i < lineas.Length; i++) 
        {
            if (lineas[i].Trim() == "") continue;
            lista.Add(SepararLinea(lineas[i]));
        }
        return lista;
    }

   
    static string[] SepararLinea(string linea)
    {
        List<string> campos = new List<string>();
        StringBuilder campo = new StringBuilder();
        bool entreComillas = false;

        foreach (char c in linea)
        {
            if (c == '"') entreComillas = !entreComillas;
            else if (c == ',' && !entreComillas)
            {
                campos.Add(campo.ToString());
                campo.Clear();
            }
            else campo.Append(c);
        }
        campos.Add(campo.ToString());
        return campos.ToArray();
    }

    static List<string[]> LeerXml(string ruta)
    {
        XDocument doc = XDocument.Load(ruta);
        return doc.Root.Elements("Pelicula").Select(p => new string[]
        {
            (string)p.Element("Title"),
            (string)p.Element("Genre"),
            (string)p.Element("Premiere"),
            (string)p.Element("Runtime"),
            (string)p.Element("IMDBScore"),
            (string)p.Element("Language")
        }).ToList();
    }

    static void GuardarXml(List<string[]> peliculas, string ruta)
    {
        XElement raiz = new XElement("Peliculas");
        foreach (string[] p in peliculas)
        {
            raiz.Add(new XElement("Pelicula",
                new XElement("Title", p[0]),
                new XElement("Genre", p[1]),
                new XElement("Premiere", p[2]),
                new XElement("Runtime", p[3]),
                new XElement("IMDBScore", p[4]),
                new XElement("Language", p[5])));
        }
        new XDocument(new XDeclaration("1.0", "utf-8", null), raiz).Save(ruta);
    }

    static void GuardarCsv(List<string[]> peliculas, string ruta)
    {
        List<string> lineas = new List<string>();
        lineas.Add("Title,Genre,Premiere,Runtime,IMDB Score,Language");

        foreach (string[] p in peliculas)
        {
            string[] campos = new string[p.Length];

            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Contains(","))
                {
                    campos[i] = "\"" + p[i] + "\"";
                }
                else
                {
                    campos[i] = p[i];
                }
            }

            lineas.Add(string.Join(",", campos));
        }
        File.WriteAllLines(ruta, lineas, Encoding.UTF8);
    }
}