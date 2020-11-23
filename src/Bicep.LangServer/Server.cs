// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Completions;
using Bicep.LanguageServer.Handlers;
using Bicep.LanguageServer.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using OmnisharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace Bicep.LanguageServer
{
    public class TestLoggerFactory : ILoggerFactory
    {
        private class TestLogger : ILogger
        {
            private class Scope : IDisposable
            {
                public void Dispose() {}
            }

            public IDisposable BeginScope<TState>(TState state) => new Scope();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                => Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }

        public void AddProvider(ILoggerProvider provider) {}

        public ILogger CreateLogger(string categoryName) => new TestLogger();

        public void Dispose() {}
    }

    public class Server
    {
        public class CreationOptions
        {
            public IResourceTypeProvider? ResourceTypeProvider { get; set; }

            public IFileResolver? FileResolver { get; set; }
        }

        private readonly OmnisharpLanguageServer server;

        public Server(PipeReader input, PipeWriter output, CreationOptions creationOptions)
            : this(creationOptions, options => options.WithInput(input).WithOutput(output))
        {
        }

        public Server(Stream input, Stream output, CreationOptions creationOptions)
            : this(creationOptions, options => options.WithInput(input).WithOutput(output))
        {
        }
        
        private Server(CreationOptions creationOptions, Action<LanguageServerOptions> onOptionsFunc)
        {
            BicepDeploymentsInterop.Initialize();
            server = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(options =>
            {
                options
                    .WithHandler<BicepTextDocumentSyncHandler>()
                    .WithHandler<BicepDocumentSymbolHandler>()
                    .WithHandler<BicepDefinitionHandler>()
                    .WithHandler<BicepReferencesHandler>()
                    .WithHandler<BicepDocumentHighlightHandler>()
                    .WithHandler<BicepDocumentFormattingHandler>()
                    .WithHandler<BicepRenameHandler>()
                    .WithHandler<BicepHoverHandler>()
                    .WithHandler<BicepCompletionHandler>()
                    .WithHandler<BicepCodeActionHandler>()
                    .WithHandler<BicepDidChangeWatchedFilesHandler>()
#pragma warning disable 0612 // disable 'obsolete' warning for proposed LSP feature
                    .WithHandler<BicepSemanticTokensHandler>()
#pragma warning restore 0612
                    .WithServices(services => RegisterServices(creationOptions, services));

                options.WithLoggerFactory(new TestLoggerFactory());

                onOptionsFunc(options);
            });
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try {
                Console.WriteLine("RunAsync: Running!");

                await server.Initialize(cancellationToken);

                Console.WriteLine("RunAsync: Initialized!");

                await server.WaitForExit;

                Console.WriteLine("RunAsync: Exited!");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"RunAsync: {exception}!");
            }
        }

        private static void RegisterServices(CreationOptions creationOptions, IServiceCollection services)
        {
            // using type based registration so dependencies can be injected automatically
            // without manually constructing up the graph
            services.AddSingleton<IResourceTypeProvider>(services => creationOptions.ResourceTypeProvider ?? new AzResourceTypeProvider());
            services.AddSingleton<IFileResolver>(services => creationOptions.FileResolver ?? new FileResolver());
            services.AddSingleton<IWorkspace, Workspace>();
            services.AddSingleton<ICompilationManager, BicepCompilationManager>();
            services.AddSingleton<ICompilationProvider, BicepCompilationProvider>();
            services.AddSingleton<ISymbolResolver, BicepSymbolResolver>();
            services.AddSingleton<ICompletionProvider, BicepCompletionProvider>();
        }
    }
}
