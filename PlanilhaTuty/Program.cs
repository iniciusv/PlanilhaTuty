using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using System.Globalization;

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
				Console.WriteLine("Data do print mais recente: " + item.MostRecentImageDate?.ToString("dd/MM/yyyy HH:mm:ss"));
				Console.WriteLine(new string('-', 30));
			}

			// Exportar para Excel
			string excelPath = @"C:\Users\vinic\Downloads\TutyTRAMPO\Setembro_TX.xlsx";
			ExportToExcel(contents, excelPath);
			Console.WriteLine("Dados exportados para o arquivo Excel com sucesso!");

			// Obter lista de prints com datas e projetos
			List<ImagePrint> imagePrints = GetImagePrints(contents);

			// Exibir os prints com datas e projetos
			Console.WriteLine("Lista de Prints com Datas e Projetos:");
			foreach (var print in imagePrints)
			{
				Console.WriteLine($"Projeto: {print.Subtitle}, Imagem: {print.ImageName}, Data do Print: {print.ImageDate?.ToString("dd/MM/yyyy HH:mm:ss")}");
			}

			// Opcional: Exportar essa lista para um arquivo Excel separado
			string excelPrintsPath = @"C:\Users\vinic\Downloads\TutyTRAMPO\Setembro_TX_Prints.xlsx";
			ExportImagePrintsToExcel(imagePrints, excelPrintsPath);
			Console.WriteLine("Lista de prints exportada para o arquivo Excel com sucesso!");
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
						currentSubtitleContent.MostRecentImageDate = GetMostRecentImageDate(currentSubtitleContent.ImageNames);
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
				currentSubtitleContent.MostRecentImageDate = GetMostRecentImageDate(currentSubtitleContent.ImageNames);
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

		public static DateTime? GetMostRecentImageDate(List<string> imageNames)
		{
			DateTime? mostRecentDate = null;
			foreach (var imageName in imageNames)
			{
				DateTime? date = ExtractDateFromImageName(imageName);
				if (date.HasValue)
				{
					if (!mostRecentDate.HasValue || date > mostRecentDate)
					{
						mostRecentDate = date;
					}
				}
			}
			return mostRecentDate;
		}

		public static DateTime? ExtractDateFromImageName(string imageName)
		{
			// Procurar por um padrão de data no nome da imagem, por exemplo: "Pasted image 20240906104120.png"
			Regex regex = new Regex(@"\d{14}");
			Match match = regex.Match(imageName);
			if (match.Success)
			{
				string dateString = match.Value;
				// Parse da string de data
				if (DateTime.TryParseExact(dateString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
				{
					return date;
				}
			}
			return null;
		}

		// Novo método para obter a lista de prints com datas e projetos
		public static List<ImagePrint> GetImagePrints(List<SubtitleContent> contents)
		{
			List<ImagePrint> imagePrints = new List<ImagePrint>();
			foreach (var content in contents)
			{
				foreach (var imageName in content.ImageNames)
				{
					DateTime? imageDate = ExtractDateFromImageName(imageName);
					imagePrints.Add(new ImagePrint
					{
						Subtitle = content.Subtitle,
						ImageName = imageName,
						ImageDate = imageDate
					});
				}
			}
			return imagePrints;
		}

		// Opcional: Método para exportar a lista de prints para um arquivo Excel
		public static void ExportImagePrintsToExcel(List<ImagePrint> imagePrints, string filePath)
		{
			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Prints");

				// Cabeçalhos
				worksheet.Cell(1, 1).Value = "Projeto";
				worksheet.Cell(1, 2).Value = "Imagem";
				worksheet.Cell(1, 3).Value = "Data do Print";

				int currentRow = 2;

				foreach (var print in imagePrints)
				{
					worksheet.Cell(currentRow, 1).Value = print.Subtitle;
					worksheet.Cell(currentRow, 2).Value = print.ImageName;
					worksheet.Cell(currentRow, 3).Value = print.ImageDate?.ToString("dd/MM/yyyy HH:mm:ss");
					currentRow++;
				}

				// Ajustar a largura das colunas
				worksheet.Columns().AdjustToContents();

				// Salvar o arquivo
				workbook.SaveAs(filePath);
			}
		}

		public static void ExportToExcel(List<SubtitleContent> contents, string filePath)
		{
			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Dados");

				// Cabeçalhos
				worksheet.Cell(1, 1).Value = "Subtítulo";
				worksheet.Cell(1, 2).Value = "Conteúdo";
				worksheet.Cell(1, 3).Value = "Imagens";
				worksheet.Cell(1, 4).Value = "Data do Print Mais Recente";

				int currentRow = 2;

				foreach (var content in contents)
				{
					worksheet.Cell(currentRow, 1).Value = content.Subtitle;
					worksheet.Cell(currentRow, 2).Value = content.Content;
					worksheet.Cell(currentRow, 3).Value = string.Join(", ", content.ImageNames);
					worksheet.Cell(currentRow, 4).Value = content.MostRecentImageDate?.ToString("dd/MM/yyyy HH:mm:ss");
					currentRow++;
				}

				// Ajustar a largura das colunas
				worksheet.Columns().AdjustToContents();

				// Salvar o arquivo
				workbook.SaveAs(filePath);
			}
		}
	}

	public class SubtitleContent
	{
		public string Subtitle { get; set; }
		public string Content { get; set; } // Conteúdo sem as imagens
		public List<string> ImageNames { get; set; }
		public StringBuilder RawContent { get; set; } // Conteúdo bruto incluindo as imagens
		public DateTime? MostRecentImageDate { get; set; } // Nova propriedade para a data mais recente
	}

	// Nova classe para armazenar informações de cada print
	public class ImagePrint
	{
		public string Subtitle { get; set; } // Nome do projeto (subtítulo)
		public string ImageName { get; set; } // Nome da imagem
		public DateTime? ImageDate { get; set; } // Data do print
	}
}
