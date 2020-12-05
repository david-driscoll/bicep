import * as monacoEditor from 'monaco-editor';
import { CloseAction, createConnection, DidOpenTextDocumentNotification, ErrorAction, MonacoLanguageClient, MonacoServices, TextDocumentFeature } from 'monaco-languageclient';
import React, { useRef, useState } from 'react';
import MonacoEditor from 'react-monaco-editor';
import { makeLspRequest } from './lspInterop';
import { createMessageConnection, Message, Trace, TraceFormat } from 'vscode-jsonrpc';
import { AbstractMessageReader, DataCallback, StreamMessageReader, } from 'vscode-jsonrpc/lib/messageReader';
import { AbstractMessageWriter, StreamMessageWriter } from 'vscode-jsonrpc/lib/messageWriter';
import { SemanticTokensFeature } from 'vscode-languageclient/lib/semanticTokens.proposed';
import { Readable, Writable, Duplex } from 'stream'

interface Props {
  initialCode: string,
  onBicepChange: (bicepContent: string) => void,
  onJsonChange: (jsonContent: string) => void,
}

function marshalToString(data : any, encoding: BufferEncoding | 'buffer') {
  return Buffer.isBuffer(data) ? data.toString(encoding === 'buffer' ? undefined : encoding) : typeof data === 'string' ? data : data.toString();
}

function createStream() {
  const output = new Duplex({
    write: (data, encoding, cb) => {
      makeLspRequest(marshalToString(data, encoding));
      cb();
    },
    read(size: number) {
    }
  });

  self['receiveLspData'] = (data: string | Buffer) => {
    console.log(`RCV: ${data}`);
    output.push(marshalToString(data, 'utf8'));
  };

  return [new StreamMessageReader(output, 'utf8'), new StreamMessageWriter(output, 'utf8')] as const;
}

function configureEditorForBicep(editor: monacoEditor.editor.IStandaloneCodeEditor, monaco: typeof monacoEditor) {
  monaco.languages.register({
    id: 'bicep',
    extensions: ['.bicep'],
    aliases: ['bicep'],
  });

  MonacoServices.install(editor);


  const [reader, writer] = createStream();
  const messageConnection = createMessageConnection(reader, writer);
  const client = new MonacoLanguageClient({
    name: "Bicep Monaco Client",
    clientOptions: {
      initializationOptions: {

      },
      documentSelector: [{ language: 'bicep' }],
      errorHandler: {
        error: () => ErrorAction.Continue,
        closed: () => CloseAction.DoNotRestart
      },
    },
    connectionProvider: {
      get: (errorHandler, closeHandler) => {
        return Promise.resolve(createConnection(messageConnection, errorHandler, closeHandler))
      }
    }
  });
(client as any)._trace = Trace.Verbose;
(client as any)._traceFormat = TraceFormat.Text;


  client.registerFeature(new SemanticTokensFeature(client));
  client.start();

  // @ts-expect-error
  editor._themeService._theme.getTokenStyleMetadata = (type, modifiers) => {
    // see 'monaco-editor/esm/vs/editor/standalone/common/themes.js' to understand these indices
    switch (type) {
      case 'keyword':
        return { foreground: 12 };
      case 'comment':
        return { foreground: 7 };
      case 'parameter':
        return { foreground: 2 };
      case 'property':
        return { foreground: 3 };
      case 'type':
        return { foreground: 8 };
      case 'member':
        return { foreground: 6 };
      case 'string':
        return { foreground: 5 };
      case 'variable':
        return { foreground: 4 };
      case 'operator':
        return { foreground: 9 };
      case 'function':
        return { foreground: 13 };
      case 'number':
        return { foreground: 15 };
      case 'class':
      case 'enummember':
      case 'event':
      case 'modifier':
      case 'label':
      case 'typeParameter':
      case 'macro':
      case 'interface':
      case 'enum':
      case 'regexp':
      case 'struct':
      case 'namespace':
        return { foreground: 0 };
    }
  };

  return client;
}

export const BicepEditor: React.FC<Props> = (props) => {
  const monacoRef = useRef<MonacoEditor>();
  const [initialCode, setInitialCode] = useState(props.initialCode);
  const [bicepContent, setBicepContent] = useState(props.initialCode);
  let client: MonacoLanguageClient;

  const options: monacoEditor.editor.IStandaloneEditorConstructionOptions = {
    scrollBeyondLastLine: false,
    automaticLayout: true,
    minimap: {
      enabled: false,
    },
    'semanticHighlighting.enabled': true,
  };

  const handleContentChange = (editor: monacoEditor.editor.IStandaloneCodeEditor, text: string) => {
    setBicepContent(text);
    client.sendNotification(DidOpenTextDocumentNotification.type, {
      textDocument: {
        "uri": "inmemory:///main.bicep",
        "languageId": "bicep",
        "version": 1,
        "text": editor.getModel().getValue(),
      }
    });
  }

  const handleEditorDidMount = (editor: monacoEditor.editor.IStandaloneCodeEditor, monaco: typeof monacoEditor) => {
    client = configureEditorForBicep(editor, monaco);
    client.onReady().then(() => handleContentChange(editor, bicepContent));
  }

  if (initialCode != props.initialCode) {
    setInitialCode(props.initialCode);
    handleContentChange(monacoRef.current.editor, props.initialCode);

    // clear the selection after this completes
    setTimeout(() => {
      monacoRef.current.editor.setSelection({ startColumn: 1, startLineNumber: 1, endColumn: 1, endLineNumber: 1 });
      monacoRef.current.editor.setScrollPosition({ scrollLeft: 0, scrollTop: 0 });
    }, 0);
  }

  return <MonacoEditor
    ref={monacoRef}
    language="bicep"
    theme="vs-dark"
    value={bicepContent}
    options={options}
    onChange={text => handleContentChange(monacoRef.current.editor, text)}
    editorDidMount={handleEditorDidMount}
  />
};