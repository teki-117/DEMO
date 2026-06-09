using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
public class ExcelReader
{
    public static List<ExcelData> ReadExcel(string filePath)
    {
        List<ExcelData> excelData = new List<ExcelData>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            while (reader.Read())
            {
                if (reader.IsDBNull(0) && reader.IsDBNull(1))
                    continue;

                var data = new ExcelData
                {
                    speakerName = GetCellString(reader, 0),
                    speakingContent = GetCellString(reader, 1),
                    avatarImageFileName = GetCellString(reader, 2),
                    vocalAudioFileName = GetCellString(reader, 3),
                    backgroundImageFileName = GetCellString(reader, 4),
                    backgroundMusicFileName = GetCellString(reader, 5),
                    characterCommands = new List<CharacterCommand>(),
                    englishName = GetCellString(reader, 7),
                    englishContent = GetCellString(reader, 8),
                    japaneseName = GetCellString(reader, 9),
                    japaneseContent = GetCellString(reader, 10)
                };

                var raw = GetCellString(reader, 6);
                if (!string.IsNullOrEmpty(raw))
                {
                    var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var block = p.Trim();
                        if (string.IsNullOrEmpty(block))
                        {
                            continue;
                        }

                        var fields = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length < 2)
                        {
                            continue;
                        }

                        var cmd = new CharacterCommand
                        {
                            characterID = fields[0].Trim(),
                            action = fields[1].Trim(),
                        };
                        if (cmd.action != Constants.DISAPPEAR)
                        {
                            if (fields.Length < 4)
                            {
                                continue;
                            }

                            var third = fields[2].Trim();
                            var fourth = fields[3].Trim();

                            if (float.TryParse(third, out var px))
                            {
                                cmd.positionX = px;
                                cmd.expressionName = string.IsNullOrWhiteSpace(fourth) ? null : fourth;
                            }
                            else if (float.TryParse(fourth, out px))
                            {
                                cmd.expressionName = string.IsNullOrWhiteSpace(third) ? null : third;
                                cmd.positionX = px;
                            }
                            else
                            {
                                cmd.expressionName = null;
                                cmd.positionX = 0f;
                            }
                        }
                        data.characterCommands.Add(cmd);
                    }
                }
                excelData.Add(data);
            }
        } while (reader.NextResult());
        return excelData;
    }
    private static string GetCellString(IExcelDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? string.Empty : reader.GetValue(index)?.ToString();
    }
}
