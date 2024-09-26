using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Environment;

namespace WinEnvManager
{
    internal class EnvManager
    {
        public static string BASE_FOLDER = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "WinEnvManager";

        public static void Init()
        {
            if(!Directory.Exists(BASE_FOLDER))
                Directory.CreateDirectory(BASE_FOLDER);
        }

        // Charger les variables depuis un fichier JSON
        public static List<EnvVariable> LoadVariablesFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<EnvVariable>();
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<EnvVariable>>(json) ?? new List<EnvVariable>();
        }

        public static void ApplyEnv()
        {
            // List of environment types
            string[] environments = Directory.GetFiles(BASE_FOLDER, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();

            // Show a menu to choose an environment
            Console.WriteLine("Choose an environment:");
            for (int i = 0; i < environments.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {environments[i]}");
            }

            int choice;
            while (true)
            {
                Console.Write("Enter the number of your choice: ");
                if (int.TryParse(Console.ReadLine(), out choice) && choice > 0 && choice <= environments.Length)
                {
                    break;
                }
                Console.WriteLine("Invalid choice. Please try again.");
            }

            string selectedEnv = environments[choice - 1];
            ApplyEnv(selectedEnv);
        }
        public static void ApplyEnv(string profileName)
        {
            string envFile = Path.Combine(BASE_FOLDER, profileName + ".json");

            // Check if the file exists
            if (!File.Exists(envFile))
            {
                Console.WriteLine($"Error: The .env file for '{profileName}' does not exist.");
                return;
            }

            // Read the file and split it into lines
            var variables = LoadVariablesFromJson(envFile);
            ApplyEnv(variables);
        }
        public static void ApplyEnv(List<EnvVariable> variables)
        {
            var tasks = new List<Task>();
            foreach (var var in variables)
            {
                if (!string.IsNullOrEmpty(var.Name))
                {
                    string varName = var.Name.Trim();
                    string varValue = !string.IsNullOrEmpty(var.Value) ? var.Value.Trim() : "";

                    // Set the environment variable for the user
                    tasks.Add(Task.Run(() => Environment.SetEnvironmentVariable(varName, varValue, EnvironmentVariableTarget.User)));
                    Console.WriteLine($"Set {varName}={varValue}");
                }
            }

            Task.WhenAll(tasks).Wait();

            Console.WriteLine("Environment variables updated successfully.");
        }
    }
}
