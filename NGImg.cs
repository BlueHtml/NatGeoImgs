using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NatGeoImgs
{
    class NGImg
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string EnDesc { get; set; }
        public string ZhDesc { get; set; }
        public DateTime AddTime { get; set; } = DateTime.UtcNow;

        public static async Task<List<NGImg>> Query(DateTime utcAddTime)
        {
            using var connection = new SqliteConnection(CONN);

            return await connection.QueryAsync<NGImg>($"SELECT * FROM {NAME} WHERE AddTime>@AddTime;", new { AddTime = utcAddTime }) as List<NGImg>;
        }

        public static async Task<int> Add(NGImg img)
        {
            using var connection = new SqliteConnection(CONN);

            return await connection.ExecuteAsync($@"INSERT INTO {NAME} (Key, Title, Link, EnDesc, ZhDesc, AddTime)
VALUES (@Key, @Title, @Link, @EnDesc, @ZhDesc, @AddTime);", img);
        }

        public static async Task InitDB()
        {
            using var connection = new SqliteConnection(CONN);

            var table = connection.Query<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name = '{NAME}';");
            var tableName = table.FirstOrDefault();
            if (!string.IsNullOrEmpty(tableName) && tableName == NAME)
                return;

            await connection.ExecuteAsync($@"
Create Table {NAME} (
Key VARCHAR(50) PRIMARY KEY,
Title VARCHAR(300) NOT NULL,
Link VARCHAR(300) NOT NULL,
EnDesc VARCHAR(1000) NOT NULL,
ZhDesc NVARCHAR(1000) NOT NULL,
AddTime DATETIME NOT NULL)
");
        }

        const string NAME = "NGImg";
        const string CONN = "Data Source=" + NAME + ".sqlite";
    }
}
