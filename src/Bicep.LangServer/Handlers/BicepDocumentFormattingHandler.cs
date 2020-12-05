// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.PrettyPrint;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Bicep.Core.PrettyPrint.Options;
using Microsoft.Extensions.Logging;
using Bicep.Core.Syntax;
using Bicep.LanguageServer.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Bicep.LanguageServer.Handlers
{
    public class BicepDocumentFormattingHandler : DocumentFormattingHandlerBase
    {
        private readonly ILogger<BicepDocumentSymbolHandler> logger;
        private readonly ICompilationManager compilationManager;

        public BicepDocumentFormattingHandler(ILogger<BicepDocumentSymbolHandler> logger, ICompilationManager compilationManager)
        {
            this.logger = logger;
            this.compilationManager = compilationManager;
        }

        public override Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
        {
            CompilationContext? context = this.compilationManager.GetCompilation(request.TextDocument.Uri);

            if (context == null)
            {
                // we have not yet compiled this document, which shouldn't really happen
                this.logger.LogError("Document formatting request arrived before file {Uri} could be compiled.", request.TextDocument.Uri);

                return Task.FromResult<TextEditContainer?>(null);
            }

            long indentSize = request.Options.TabSize;
            IndentKindOption indentKindOption = request.Options.InsertSpaces ? IndentKindOption.Space : IndentKindOption.Tab;
            bool insertFinalNewline = request.Options.ContainsKey("insertFinalNewline") && request.Options.InsertFinalNewline;

            ProgramSyntax programSyntax = context.ProgramSyntax;
            PrettyPrintOptions options = new PrettyPrintOptions(NewlineOption.Auto, indentKindOption, indentSize, insertFinalNewline);

            string? output = PrettyPrinter.PrintProgram(programSyntax, options);

            if (output == null)
            {
                return Task.FromResult<TextEditContainer?>(null);
            }

            return Task.FromResult<TextEditContainer?>(new TextEditContainer(new TextEdit
            {
                Range = programSyntax.Span.ToRange(context.LineStarts),
                NewText = output
            }));
        }

        protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
            new DocumentFormattingRegistrationOptions
            {
                DocumentSelector = DocumentSelectorFactory.Create()
            };
    }
}
