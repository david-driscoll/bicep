// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Bicep.LanguageServer.Completions
{
    public static class CompletionItemBuilder
    {
        public static CompletionItem Create(CompletionItemKind kind) => new CompletionItem { Kind = kind };

        public static CompletionItem WithLabel(this CompletionItem item, string label)
        {
            return item with { Label = label };
        }

        public static CompletionItem WithInsertText(this CompletionItem item, string insertText, InsertTextMode insertTextMode = InsertTextMode.AsIs)
        {
            AssertNoTextEdit(item);

            return item with
            {
                InsertText = insertText,
                InsertTextFormat = InsertTextFormat.PlainText,
                InsertTextMode = insertTextMode,
            };
        }

        public static CompletionItem WithSnippet(this CompletionItem item, string snippet, InsertTextMode insertTextMode = InsertTextMode.AsIs)
        {
            AssertNoTextEdit(item);

            return item with
            {
                InsertText = snippet,
                InsertTextFormat = InsertTextFormat.PlainText,
                InsertTextMode = insertTextMode,
            };
        }

        public static CompletionItem WithPlainTextEdit(this CompletionItem item, Range range, string text, InsertTextMode insertTextMode = InsertTextMode.AsIs)
        {
            AssertNoInsertText(item);
            item = SetTextEditInternal(item, range, InsertTextFormat.PlainText, text, insertTextMode);
            return item;
        }

        public static CompletionItem WithSnippetEdit(this CompletionItem item, Range range, string snippet, InsertTextMode insertTextMode = InsertTextMode.AsIs)
        {
            AssertNoInsertText(item);
            item = SetTextEditInternal(item, range, InsertTextFormat.Snippet, snippet, insertTextMode);
            return item;
        }

        public static CompletionItem WithAdditionalEdits(this CompletionItem item, TextEditContainer editContainer)
        {
            return item with { AdditionalTextEdits = editContainer };
        }

        public static CompletionItem WithDetail(this CompletionItem item, string detail)
        {
            return item with { Detail = detail };
        }

        public static CompletionItem WithDocumentation(this CompletionItem item, string markdown)
        {
            return item with
            {
                Documentation = new StringOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = markdown
                })
            };
        }

        public static CompletionItem WithSortText(this CompletionItem item, string sortText)
        {
            return item with { SortText = sortText };
        }

        public static CompletionItem Preselect(this CompletionItem item) => item.Preselect(preselect: true);

        public static CompletionItem Preselect(this CompletionItem item, bool preselect)
        {
            return item with { Preselect = preselect };
        }

        public static CompletionItem WithCommitCharacters(this CompletionItem item, Container<string> commitCharacters)
        {
            return item with { CommitCharacters = commitCharacters };
        }

        private static CompletionItem SetTextEditInternal(CompletionItem item, Range range, InsertTextFormat format, string text, InsertTextMode insertTextMode)
        {
            return item with
            {
                InsertTextFormat = format,
                TextEdit = new TextEdit
                {
                    Range = range,
                    NewText = text
                },
                InsertTextMode = insertTextMode
            };
        }

        private static void AssertNoTextEdit(CompletionItem item)
        {
            if (item.TextEdit != null)
            {
                throw new InvalidOperationException("Unable to set the specified insert text because a text edit is already set.");
            }
        }

        private static void AssertNoInsertText(CompletionItem item)
        {
            if (item.InsertText != null)
            {
                throw new InvalidOperationException("Unable to set the text edit because the insert text is already set.");
            }
        }
    }
}
