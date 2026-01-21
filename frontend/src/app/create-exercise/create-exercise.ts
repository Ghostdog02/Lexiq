import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';

@Component({
  selector: 'app-create-exercise',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-exercise.html',
  styleUrl: './create-exercise.scss',
})
export class CreateLesson {
  lessonForm: FormGroup;
  
  exerciseTypes = [
    { value: 'multiple-choice', label: 'Multiple Choice' },
    { value: 'fill-in-blank', label: 'Fill in the Blank' },
    { value: 'translation', label: 'Translation' }
  ];

  constructor(private fb: FormBuilder, private sanitizer: DomSanitizer) {
    this.lessonForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      content: ['', [Validators.required, Validators.minLength(20)]], // Textbook content
      difficulty: [1, [Validators.required, Validators.min(1), Validators.max(3)]],
      exercises: this.fb.array([])
    });
  }

  get f() { return this.lessonForm.controls; }
  get exercises() { return this.lessonForm.get('exercises') as FormArray; }

  parseMarkdown(markdown: string): SafeHtml {
    if (!markdown) return this.sanitizer.bypassSecurityTrustHtml('');
    const html = marked(markdown, { async: false, breaks: true });
    return this.sanitizer.bypassSecurityTrustHtml(html as string);
  }

  replaceFillInBlank(question: string, answer: string): string {
    if (!question) return question;
    if (!answer) {
      return question.replace(/{%special%}/g, '');
    }
    const underscores = '_'.repeat(answer.length);
    return question.replace(/{%special%}/g, underscores);
  }

  addExercise() {
    const exerciseGroup = this.fb.group({
      type: ['multiple-choice', Validators.required],
      question: ['', Validators.required],
      // Fields for Multiple Choice
      options: this.fb.array([
        this.fb.control(''),
        this.fb.control('')
      ]), 
      correctAnswer: [''],
      // Field for Fill-in-blank / Translation
      answer: [''] 
    });
    this.exercises.push(exerciseGroup);
  }

  removeExercise(index: number) {
    this.exercises.removeAt(index);
  }

  // Helper for Multiple Choice options
  getOptions(exerciseIndex: number): FormArray {
    return this.exercises.at(exerciseIndex).get('options') as FormArray;
  }

  addOption(exerciseIndex: number) {
    this.getOptions(exerciseIndex).push(this.fb.control(''));
  }

  removeOption(exerciseIndex: number, optionIndex: number) {
    this.getOptions(exerciseIndex).removeAt(optionIndex);
  }

  onSubmit() {
    if (this.lessonForm.valid) {
      console.log('Lesson submitted:', this.lessonForm.value);
    }
  }
}
