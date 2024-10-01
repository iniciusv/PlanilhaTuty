using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownParser
{
	class Program
	{
		static void Main(string[] args)
		{
			string path = @"C:\Users\vinic\Downloads\TutyTRAMPO\Setembro TX.md";
			List<SubtitleContent> contents = ReadMarkdownFile(path);

			// Exibir os resultados
			foreach (var item in contents)
			{
				Console.WriteLine("Subtítulo: " + item.Subtitle);
				Console.WriteLine("Conteúdo:\n" + item.Content);
				Console.WriteLine("Imagens:");
				foreach (var imageName in item.ImageNames)
				{
					Console.WriteLine(imageName);
				}
				Console.WriteLine(new string('-', 30));
			}
		}

		public static List<SubtitleContent> ReadMarkdownFile(string path)
		{
			List<SubtitleContent> subtitleContents = new List<SubtitleContent>();
			string[] lines = File.ReadAllLines(path);

			SubtitleContent currentSubtitleContent = null;
			StringBuilder contentBuilder = null;

			foreach (string line in lines)
			{
				if (line.StartsWith("###"))
				{
					// Salvar o conteúdo anterior, se existir
					if (currentSubtitleContent != null)
					{
						currentSubtitleContent.Content = contentBuilder.ToString().Trim();
						currentSubtitleContent.ImageNames = ExtractImageNames(currentSubtitleContent.RawContent.ToString());
						subtitleContents.Add(currentSubtitleContent);
					}

					// Iniciar novo SubtitleContent
					currentSubtitleContent = new SubtitleContent();
					currentSubtitleContent.Subtitle = line.Substring(3).Trim(); // Remove o ###
					contentBuilder = new StringBuilder();
					currentSubtitleContent.RawContent = new StringBuilder(); // Armazena o conteúdo bruto incluindo imagens
				}
				else
				{
					if (currentSubtitleContent != null)
					{
						currentSubtitleContent.RawContent.AppendLine(line);

						// Verificar se a linha é uma imagem
						if (!IsImageLine(line))
						{
							contentBuilder.AppendLine(line);
						}
					}
					// Ignora linhas antes do primeiro subtítulo
				}
			}

			// Adicionar o último conteúdo
			if (currentSubtitleContent != null)
			{
				currentSubtitleContent.Content = contentBuilder.ToString().Trim();
				currentSubtitleContent.ImageNames = ExtractImageNames(currentSubtitleContent.RawContent.ToString());
				subtitleContents.Add(currentSubtitleContent);
			}

			return subtitleContents;
		}

		public static bool IsImageLine(string line)
		{
			// Verifica se a linha contém uma referência a uma imagem no formato ![[...]]
			return Regex.IsMatch(line, @"!\[\[.*?\]\]");
		}

		public static List<string> ExtractImageNames(string content)
		{
			List<string> imageNames = new List<string>();
			Regex regex = new Regex(@"\[\[(.*?)\]\]");
			MatchCollection matches = regex.Matches(content);
			foreach (Match match in matches)
			{
				imageNames.Add(match.Groups[1].Value);
			}
			return imageNames;
		}
	}

	public class SubtitleContent
	{
		public string Subtitle { get; set; }
		public string Content { get; set; } // Conteúdo sem as imagens
		public List<string> ImageNames { get; set; }
		public StringBuilder RawContent { get; set; } // Conteúdo bruto incluindo as imagens
	}
}
