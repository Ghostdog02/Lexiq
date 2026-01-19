import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { Exercise, Lesson } from './lesson.interface';
import {email, form, FormField, required, submit} from '@angular/forms/signals';

@Component({
  selector: 'app-create-exercise',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-lesson.component.html',
  styleUrl: './create-lesson.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateLesson {
  lessonForm: FormGroup;
  
  lessonModel = signal<Lesson>({
    title: '',
    description: '',
    estimatedDuration: 0,
    content: '',
    courseId: '',
    exercises: Array<Exercise>()
  })

  exerciseTypes = [
    { value: 'multiple-choice', label: 'Multiple Choice' },
    { value: 'fill-in-blank', label: 'Fill in the Blank' },
    { value: 'translation', label: 'Translation' }
  ];

  constructor(private formBuilder: FormBuilder) {
    this.lessonForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      content: ['', [Validators.required, Validators.minLength(20)]], // Textbook content
      difficulty: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
      exercises: this.formBuilder.array([])
    });
  }

  get formControls() { return this.lessonForm.controls; }
  get exercises() { return this.lessonForm.get('exercises') as FormArray; }

  addExercise() {
    const exerciseGroup = this.formBuilder.group({
      type: ['multiple-choice', Validators.required],
      question: ['', Validators.required],
      // Fields for Multiple Choice
      options: this.formBuilder.array([
        this.formBuilder.control(''),
        this.formBuilder.control('')
      ]), 
      correctAnswer: [''],
      // Field for Fill-in-blank / Translation
      answer: [''] 
    });
    
    this.exercises.push(exerciseGroup);
  }

  removeAtExercise(index: number) {
    this.exercises.removeAt(index);
  }

  // Helper for Multiple Choice options
  getOptions(exerciseIndex: number): FormArray {
    return this.exercises.at(exerciseIndex).get('options') as FormArray;
  }

  addOption(exerciseIndex: number) {
    this.getOptions(exerciseIndex).push(this.formBuilder.control(''));
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
