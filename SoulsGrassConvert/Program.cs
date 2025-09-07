using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using PPlus;
using SharpGLTF.Schema2;
using SoulsFormats;

namespace SoulsGrassConvert;

class Program
{
    public static bool IsDebug()
    {
        return Debugger.IsAttached;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "crash.log"),
            exception?.InnerException?.ToString() ?? exception?.ToString());
        throw exception;
    }

    private static void RunProgram(string[] args)
    {
        PromptPlus.DoubleDash("SoulsGrassConvert");

        string? filePath = null;
        bool automatic;
        if (args.Any())
        {
            automatic = true;
            filePath = args.First();
            if (filePath == null || !File.Exists(filePath))
            {
                PromptPlus.WriteLine("File not found.");
                return;
            }
        }
        else
        {
            automatic = false;
            PromptPlus.WriteLine(
                "Hi! This is a quick tool for turning a GRASS file into a GLTF file, and vice versa.");
            PromptPlus.WriteLine();
            PromptPlus.KeyPress("In the next dialog, please select the file you wish to process.").Run();

            while (filePath == null || !File.Exists(filePath))
            {
                var picker = NativeFileDialogSharp.Dialog.FileOpen();
                if (!picker.IsOk)
                {
                    PromptPlus.KeyPress("Invalid file path! Try again...").Run();
                    continue;
                }

                filePath = picker.Path;
                break;
            }
        }

        var filename = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath).ToLower();
        if (ext == ".grass")
        {
            PromptPlus.WriteLine($"Reading GRASS file at {filePath}...");
            var grass = GRASS.Read(filePath);

            PromptPlus.WriteLine($"Converting GRASS to GLTF...");
            var gltf = ModelRoot.CreateModel();
            var scene = gltf.DefaultScene = gltf.UseScene($"{filename}_scene");
            var node = scene.CreateNode($"{filename}_node");
            var mesh = node.Mesh = gltf.CreateMesh($"{filename}_mesh");
            var material = gltf
                .CreateMaterial($"{filename}_material")
                .WithDoubleSide(true);

            var indices = grass.Faces.Select(a => new[] { a.VertexIndexA, a.VertexIndexB, a.VertexIndexC })
                .SelectMany(a => a).ToList().AsReadOnly();
            var prim = mesh.CreatePrimitive()
                .WithVertexAccessor("POSITION",
                    grass.Vertices.Select(a => a.Position with { X = -a.Position.X }).ToArray())
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);
            for (int i = 0; i < 6; i++)
            {
                prim.WithVertexAccessor($"COLOR_{i}",
                    grass.Vertices.Select(a => new Vector4(a.GrassDensities[i], a.GrassDensities[i], a.GrassDensities[i], 1))
                        .ToArray());
            }

            var glbPath = Path.Combine(Path.GetDirectoryName(filePath)!, $"{Path.GetFileNameWithoutExtension(filePath)}.glb");
            if (File.Exists(glbPath))
            {
                PromptPlus.WriteLine($"Backing up existing file at {glbPath}...");
                File.Copy(glbPath, $"{glbPath}.bak", true);
            }
            PromptPlus.WriteLine($"Writing GLB to {glbPath}...");
            gltf.SaveGLB(glbPath);

            var boundingPath = Path.Combine(Path.GetDirectoryName(filePath)!, $"{Path.GetFileNameWithoutExtension(filePath)}.json");
            if (File.Exists(boundingPath))
            {
                PromptPlus.WriteLine($"Backing up existing file at {boundingPath}...");
                File.Copy(boundingPath, $"{boundingPath}.bak", true);
            }
            PromptPlus.WriteLine($"Writing bounding boxes to {boundingPath}...");
            var boundingVolumes = JsonSerializer.Serialize(grass.BoundingVolumeHierarchy);
            File.WriteAllText(boundingPath, boundingVolumes);

            if (automatic)
                PromptPlus.WriteLine($"Wrote GLB to {glbPath}.");
            else
                PromptPlus.KeyPress($"Wrote GLB to {glbPath}. Press any key to exit...").Run();
            return;
        }
        else if (ext == ".gltf" || ext == ".glb")
        {
            PromptPlus.WriteLine($"Reading GLTF file at {filePath}...");
            var loaded = SharpGLTF.Schema2.ModelRoot.Load(filePath)!;
            var boundingPath = Path.Combine(Path.GetDirectoryName(filePath)!,
                $"{Path.GetFileNameWithoutExtension(filePath)}.json");
            if (!File.Exists(boundingPath))
            {
                if (automatic)
                    PromptPlus.WriteLine($"Could not find bounding box data at {boundingPath}.");
                else
                    PromptPlus.KeyPress($"Could not find bounding box data at {boundingPath}. Press any key to exit...").Run();
                return;
            }
            PromptPlus.WriteLine($"Reading bounding box data at {boundingPath}...");
            var boundingJson = File.ReadAllText(boundingPath);
            List<GRASS.Volume>? bounding = JsonSerializer.Deserialize<List<GRASS.Volume>>(boundingJson);
            if (bounding == null)
            {
                if (automatic)
                    PromptPlus.WriteLine($"Invalid bounding box data provided.");
                else
                    PromptPlus.KeyPress($"Invalid bounding box data provided. Press any key to exit...").Run();
                return;
            }
            var loadedMesh = loaded.LogicalMeshes.First();
            var loadedPrim = loadedMesh.Primitives.First();
            var vertexPos = loadedPrim.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
            var faceIndices = loadedPrim.GetIndexAccessor().AsIndicesArray().ToArray();
            var vertices = vertexPos.Select(a => new GRASS.Vertex() { Position = a with { X = -a.X } }).ToList();
            var faces = faceIndices.Select((x, i) => new { x, i }).GroupBy(g => g.i / 3).Select(a =>
            {
                var face = new GRASS.Face();
                var v = a.ToArray().Select(a => a.x).ToArray();
                face.VertexIndexA = (int)v[0];
                face.VertexIndexB = (int)v[1];
                face.VertexIndexC = (int)v[2];
                return face;
            }).ToList();
            var newGrass = new GRASS();
            for (int j = 0; j < 6; j++)
            {
                var color = loadedPrim.GetVertexAccessor($"COLOR_{j}").AsVector4Array().ToArray();
                for (int i = 0; i < vertices.Count; i++)
                {
                    var vertex = vertices[i];
                    vertex.GrassDensities[j] = color[i].X;
                }
            }

            // Merge vertices
            var samePosGroups = vertices.GroupBy(v => v.Position).ToList();
            var newVertices = samePosGroups.Select(g => g.First()).ToList();
            foreach (GRASS.Face face in faces)
            {
                var aVertex = vertices[face.VertexIndexA];
                var aGroup = samePosGroups.First(a => a.Contains(aVertex));
                var aFirstIdx = newVertices.IndexOf(aGroup.First());
                face.VertexIndexA = aFirstIdx;

                var bVertex = vertices[face.VertexIndexB];
                var bGroup = samePosGroups.First(a => a.Contains(bVertex));
                var bFirstIdx = newVertices.IndexOf(bGroup.First());
                face.VertexIndexB = bFirstIdx;

                var cVertex = vertices[face.VertexIndexC];
                var cGroup = samePosGroups.First(a => a.Contains(cVertex));
                var cFirstIdx = newVertices.IndexOf(cGroup.First());
                face.VertexIndexC = cFirstIdx;
            }

            newGrass.Vertices = newVertices;
            newGrass.Faces = faces;
            newGrass.BoundingVolumeHierarchy = bounding;
            newGrass.BoundingVolumeHierarchy = new();
            newGrass.BoundingVolumeHierarchy.Add(new GRASS.Volume()
            {
                BoundingBox = new GRASS.BoundingBox() { Min = new()
                {
                    X = newVertices.Min(v => v.Position.X),
                    Y = newVertices.Min(v => v.Position.Y),
                    Z = newVertices.Min(v => v.Position.Z)
                }, Max = new()
                {
                    X = newVertices.Max(v => v.Position.X),
                    Y = newVertices.Max(v => v.Position.Y),
                    Z = newVertices.Max(v => v.Position.Z)
                } },
                Unk10 = 31,
                StartChildIndex = 0,
                EndChildIndex = 0,
                StartFaceIndex = 0,
                EndFaceIndex = faces.Count
            });

            var grassPath = Path.Combine(Path.GetDirectoryName(filePath)!, $"{Path.GetFileNameWithoutExtension(filePath)}.grass");
            if (File.Exists(grassPath))
            {
                PromptPlus.WriteLine($"Backing up existing file at {grassPath}...");
                File.Copy(grassPath, $"{grassPath}.bak", true);
            }
            PromptPlus.WriteLine($"Writing GRASS to {grassPath}...");
            newGrass.Write(grassPath);

            if (automatic)
                PromptPlus.WriteLine($"Wrote GRASS to {grassPath}.");
            else
                PromptPlus.KeyPress($"Wrote GRASS to {grassPath}. Press any key to exit...").Run();
            return;
        }
        else
        {
            if (automatic)
                PromptPlus.WriteLine("Provided file is not a GLTF or a GRASS file.");
            else
                PromptPlus.KeyPress("Provided file is not a GLTF or a GRASS file. Press any key to exit...").Run();
            return;
        }
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");
        PromptPlus.IgnoreColorTokens = true;

        if (!IsDebug())
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            RunProgram(args);
        }
        catch (Exception e) when (!IsDebug())
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "crash.log"),
                e?.InnerException?.ToString() ?? e?.ToString());
            PromptPlus.Error.WriteLine(@$"
There was an exception:

{e?.InnerException?.ToString() ?? e?.ToString()}

This error message has also been saved to crash.log in the program directory.

Press any key to exit...");
            PromptPlus.ReadKey();
        }
    }
}