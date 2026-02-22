import EditorJS from '@editorjs/editorjs';
// @ts-ignore - EditorJS plugins may not have type definitions
import ImageTool from '@editorjs/image';
// @ts-ignore - EditorJS plugins may not have type definitions
import AttachesTool from '@editorjs/attaches';

import { Component, OnInit, ViewChild, ElementRef, forwardRef, OnDestroy, Output, EventEmitter, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-editor',
  template: `
    <div #editorContainer id="editorjs"></div>
  `,
  styleUrl: './editor.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => EditorComponent),
      multi: true
    }
  ]
})
export class EditorComponent implements OnInit, OnDestroy, ControlValueAccessor {
  @ViewChild('editorContainer', { static: true }) editorContainer!: ElementRef;
  @Output() imageUploaded = new EventEmitter<string>();

  private editor!: EditorJS;
  private apiUrl = import.meta.env.BACKEND_API_URL;
  private http = inject(HttpClient);
  private onChange: any = () => { };
  private onTouched: any = () => { };
  private changeDebounceTimer: any = null;
  private lastSavedBlocks: string = '';

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
              byFile: `${this.apiUrl}/uploads/image`,
              byUrl: `${this.apiUrl}/uploads/image-by-url`,
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
            endpoint: `${this.apiUrl}/uploads/file`,
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
      autofocus: true,
      logLevel: 'ERROR' as any,
      onChange: async (api, event) => {
        // Debounce to avoid excessive saves on mouse movements
        if (this.changeDebounceTimer) {
          clearTimeout(this.changeDebounceTimer);
        }

        this.changeDebounceTimer = setTimeout(async () => {
          const content = await this.editor.save();
          // Compare only blocks — `time` is a fresh timestamp on every save()
          // and would make this check always pass, causing the live preview to
          // re-render (and re-request images) on every mouse move.
          const blocksJson = JSON.stringify(content.blocks);

          if (blocksJson !== this.lastSavedBlocks) {
            this.lastSavedBlocks = blocksJson;
            this.onChange(JSON.stringify(content));
            this.onTouched();
          }
        }, 300); // 300ms debounce
      }
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
      formData.append('file', file);

      // Determine endpoint based on file type
      const endpoint = fileType === 'image'
        ? `${this.apiUrl}/uploads/image`
        : `${this.apiUrl}/uploads/file`;

      const response = await firstValueFrom(
        this.http.post<{
          success: Number,
          file: {
            url: string;
            name: string;
            size: number;
            extension?: string;
            title?: string;
          }
        }>(endpoint, formData)
      );

      if (fileType === 'image') {
        this.imageUploaded.emit(response.file.url);
        return {
          success: 1,
          file: {
            url: response.file.url,
            name: response.file.name,
            size: response.file.size
          }
        };
      }

      return {
        success: 1,
        file: {
          url: response.file.url,
          size: response.file.size,
          name: response.file.name,
          extension: response.file.extension,
          title: response.file.title || response.file.name
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

  private async uploadFileByUrl(url: string, fileType: string = 'file'): Promise<any> {
    try {
      const endpoint = fileType === 'image'
        ? `${this.apiUrl}/uploads/image-by-url`
        : `${this.apiUrl}/uploads/file-by-url`;

      const response = await firstValueFrom(
        this.http.post<{
          success: Number,
          file: {
            url: string;
            name: string;
            size: number;
            extension?: string;
            title?: string;
          }
        }>(endpoint, { url })
      );

      if (fileType === 'image') {
        this.imageUploaded.emit(response.file.url);
        return {
          success: 1,
          file: {
            url: response.file.url
          }
        };
      }

      return {
        success: 1,
        file: {
          url: response.file.url,
          size: response.file.size,
          name: response.file.name,
          extension: response.file.extension
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

  writeValue(value: any): void {
    if (value && this.editor) {
      try {
        const data = typeof value === 'string' ? JSON.parse(value) : value;
        this.lastSavedBlocks = JSON.stringify(data.blocks ?? []);
        this.editor.render(data);
      } catch (error) {
        console.error('Failed to write value to editor:', error);
      }
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // EditorJS doesn't have a built-in disabled state
    // Could be implemented by making the editor read-only
  }

  ngOnDestroy(): void {
    if (this.changeDebounceTimer) {
      clearTimeout(this.changeDebounceTimer);
    }
    if (this.editor) {
      this.editor.destroy();
    }
  }
}