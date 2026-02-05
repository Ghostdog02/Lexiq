import { Injectable } from '@angular/core';
import edjsHTML from 'editorjs-html';
import { marked } from 'marked';

/** Renders @editorjs/table blocks — not included in editorjs-html built-ins. */
const tablePlugin = ({ data }: { data: any }): string => {
  let html = '<table>';

  if (data.withHeadings && data.content.length > 0) {
    html += '<thead><tr>';
    data.content[0].forEach((cell: string) => { html += `<th>${cell}</th>`; });
    html += '</tr></thead><tbody>';
    data.content.slice(1).forEach((row: string[]) => {
      html += '<tr>';
      row.forEach((cell: string) => { html += `<td>${cell}</td>`; });
      html += '</tr>';
    });
    html += '</tbody>';
  } else {
    html += '<tbody>';
    data.content.forEach((row: string[]) => {
      html += '<tr>';
      row.forEach((cell: string) => { html += `<td>${cell}</td>`; });
      html += '</tr>';
    });
    html += '</tbody>';
  }

  html += '</table>';
  return html;
};

// Base parser used only to delegate the legacy 'List' block type to the built-in 'list' parser.
const baseParser = edjsHTML();

const edjsParser = edjsHTML({
  table: tablePlugin,
  /** Legacy @editorjs/list v1 used capitalised 'List' — delegate to the built-in 'list' parser. */
  List: (block: any) => (baseParser.parseBlock({ ...block, type: 'list' }) as string) || ''
});

@Injectable({ providedIn: 'root' })
export class ContentParserService {
  /**
   * Converts an Editor.js JSON string to HTML.
   * Falls back to Markdown rendering if the input is not valid Editor.js output.
   */
  parse(content: string): string {
    if (!content) return '';

    try {
      const data = JSON.parse(content);
      if (data?.blocks && Array.isArray(data.blocks)) {
        return edjsParser.parse(data);
      }
    } catch {
      // Not valid JSON — fall through to markdown
    }

    return marked(content, { async: false, breaks: true }) as string;
  }
}
