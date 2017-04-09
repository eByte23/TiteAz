﻿using System.Data;
using System.Data.SqlClient;
using Autofac;
using TiteAz.Common;

namespace TiteAz.SeedData
{
    public class EventStoreDatabaseModule : Module
    {
        private string ConnectionString { get; }

        public EventStoreDatabaseModule(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new SqlConnectionString(ConnectionString))
                .As<IConnectionString>().SingleInstance();

            builder.Register(c =>
            {
                var conn = new SqlConnection(c.Resolve<IConnectionString>().ConnectionString);
                conn.Open();
                return conn;
            }).AsSelf().As<IDbConnection>().InstancePerLifetimeScope();



            builder.Register(c =>
          {
              var conn = c.Resolve<IDbConnection>();
              return conn.BeginTransaction();
          }).As<IDbTransaction>().InstancePerLifetimeScope();
        }

        public static void TestLocalDbExists(IConnectionString connString)
        {
            using (var connection = new SqlConnection(string.Format(@"Server={0};Database=Master;Integrated Security=true;", connString.Keys["Server"])))
            {
                connection.Open();

                var createDb = string.Format(
                    @"
IF EXISTS(SELECT * FROM sys.databases WHERE name='{0}')
BEGIN
	ALTER DATABASE [{0}]
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE
	DROP DATABASE [{0}]
END

DECLARE @FILENAME AS VARCHAR(255)

SET @FILENAME = CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{0}';

EXEC ('CREATE DATABASE [{0}] ON PRIMARY
	(NAME = [{0}],
	FILENAME =''' + @FILENAME + ''',
	SIZE = 25MB,
	MAXSIZE = 50MB,
	FILEGROWTH = 5MB )')
", connString.Keys["Database"]);

                var command = connection.CreateCommand();
                command.CommandText = createDb;
                command.ExecuteNonQuery();
            }

            using (var connection = new SqlConnection(connString.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"
CREATE TABLE [dbo].[events](
       [id] [bigint] IDENTITY(1,1) NOT NULL,
       [category] [nvarchar](500) NOT NULL,
       [streamid] [uniqueidentifier] NOT NULL,
       [transactionid] [uniqueidentifier] NOT NULL,
       [metadata] [nvarchar](max) NOT NULL,
       [bodytype] [nvarchar](500) NOT NULL,
       [body] [nvarchar](max) NOT NULL,
       [who] [uniqueidentifier] NOT NULL,
       [_when] [datetime] NOT NULL,
       [version] [int] NOT NULL,
       [appversion] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED
(
       [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE TABLE users (
    id UNIQUEIDENTIFIER PRIMARY key,
    email nvarchar(255) not null,
    first_name nvarchar(255) not null,
    last_name nvarchar(255) not null,
	created datetime not null
);

CREATE TABLE debts (
    id UNIQUEIDENTIFIER PRIMARY key,
    bill_id UNIQUEIDENTIFIER,
    debit_user_id UNIQUEIDENTIFIER not null,
    credit_user_id UNIQUEIDENTIFIER not null,
    amount DECIMAL(12,0) not null,
    debt_status nvarchar(100) not null
);


CREATE TABLE bills (
    id UNIQUEIDENTIFIER PRIMARY key,
    [description] nvarchar(500) not null,
    created_date DATETIME not null
)

CREATE TABLE read_model (
    id UNIQUEIDENTIFIER PRIMARY key,
    model_type text not null,
    body text not null,
    last_updated datetime not null
);




";
                command.ExecuteNonQuery();
            }
        }
    }
}
