import EditorJS from '@editorjs/editorjs';
// @ts-ignore - EditorJS plugins may not have type definitions
import ImageTool from '@editorjs/image';
// @ts-ignore - EditorJS plugins may not have type definitions
import AttachesTool from '@editorjs/attaches';

import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-editor',
  template: `
    <div #editorContainer id="editorjs"></div>
  `,
  styles: [`
    #editorjs {
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      padding: 20px;
      min-height: 400px;
    }
  `]
})
export class EditorComponent implements OnInit {
  @ViewChild('editorContainer', { static: true }) editorContainer!: ElementRef;

  private editor!: EditorJS;
  private apiUrl = import.meta.env.BACKEND_API_URL;

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.initializeEditor();
  }

  initializeEditor(): void {
    this.editor = new EditorJS({
      holder: 'editorjs',
      tools: {
        // Image tool - for images only
        image: {
          class: ImageTool,
          config: {
            endpoints: {
              byFile: `${this.apiUrl}/upload/image`,
              byUrl: `${this.apiUrl}/upload/image-by-url`,
            },
            field: 'image',
            types: 'image/*',
            uploader: {
              uploadByFile: (file: File) => {
                return this.uploadFile(file, 'image');
              },
              uploadByUrl: (url: string) => {
                return this.uploadFileByUrl(url, 'image');
              }
            }
          }
        },

        // Attaches tool - for documents, PDFs, etc.
        attaches: {
          class: AttachesTool,
          config: {
            endpoint: `${this.apiUrl}/upload/file`,
            field: 'file',
            types: '*', // Accept all file types
            // Or specify specific types:
            // types: 'application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            uploader: {
              uploadByFile: (file: File) => {
                return this.uploadFile(file, 'file');
              }
            }
          }
        }
      },
      placeholder: 'Start writing your content...',
      autofocus: true
    });
  }

  /**
   * Generic file upload function
   * @param file - File to upload
   * @param fileType - Type of file (image, file, video, audio)
   */
  private async uploadFile(file: File, fileType: string = 'file'): Promise<any> {
    try {
      const formData = new FormData();
      formData.append(fileType, file);

      // Determine endpoint based on file type
      const endpoint = fileType === 'image'
        ? `${this.apiUrl}/upload/image`
        : `${this.apiUrl}/upload/file`;

      const response = await firstValueFrom(
        this.http.post<{
          url: string;
          name: string;
          size: number;
          extension?: string;
          title?: string;
        }>(endpoint, formData)
      );

      // Format for EditorJS Image tool
      if (fileType === 'image') {
        return {
          success: 1,
          file: {
            url: response.url,
            name: response.name,
            size: response.size
          }
        };
      }

      // Format for EditorJS Attaches tool
      return {
        success: 1,
        file: {
          url: response.url,
          size: response.size,
          name: response.name,
          extension: response.extension,
          title: response.title || response.name
        }
      };
    } catch (error: any) {
      console.error('File upload failed:', error);
      return {
        success: 0,
        message: error.error?.message || 'Upload failed'
      };
    }
  }

  /**
   * Upload file by URL
   */
  private async uploadFileByUrl(url: string, fileType: string = 'file'): Promise<any> {
    try {
      const endpoint = fileType === 'image'
        ? `${this.apiUrl}/upload/image-by-url`
        : `${this.apiUrl}/upload/file-by-url`;

      const response = await firstValueFrom(
        this.http.post<{
          url: string;
          name?: string;
          size?: number;
          extension?: string;
        }>(endpoint, { url })
      );

      if (fileType === 'image') {
        return {
          success: 1,
          file: {
            url: response.url
          }
        };
      }

      return {
        success: 1,
        file: {
          url: response.url,
          size: response.size,
          name: response.name,
          extension: response.extension
        }
      };
    } catch (error: any) {
      console.error('File URL fetch failed:', error);
      return {
        success: 0,
        message: error.error?.message || 'Fetch failed'
      };
    }
  }

  /**
   * Advanced configuration with custom file types
   */
  initializeEditorWithCustomTypes(): void {
    this.editor = new EditorJS({
      holder: 'editorjs',
      tools: {
        image: {
          class: ImageTool,
          config: {
            uploader: {
              uploadByFile: (file: File) => this.uploadFile(file, 'image')
            }
          }
        },

        // Documents (PDF, Word, Excel)
        documents: {
          class: AttachesTool,
          config: {
            field: 'document',
            types: 'application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            uploader: {
              uploadByFile: (file: File) => this.uploadFile(file, 'document')
            }
          }
        },

        // Any file type
        files: {
          class: AttachesTool,
          config: {
            field: 'file',
            types: '*',
            buttonText: 'Upload File',
            uploader: {
              uploadByFile: (file: File) => this.uploadFile(file, 'file')
            }
          }
        }
      }
    });
  }

  /**
   * Save editor content
   */
  async saveContent(): Promise<void> {
    try {
      const outputData = await this.editor.save();
      console.log('Saved data:', outputData);

      const response = await firstValueFrom(
        this.http.post(`${this.apiUrl}/content/save`, outputData)
      );

      console.log('Content saved successfully', response);
    } catch (error) {
      console.error('Save failed:', error);
    }
  }

  ngOnDestroy(): void {
    if (this.editor) {
      this.editor.destroy();
    }
  }
}