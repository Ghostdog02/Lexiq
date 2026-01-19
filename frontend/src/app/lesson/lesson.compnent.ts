import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { Exercise, Lesson, LessonForm } from './lesson.interface';

@Component({
  selector: 'app-create-exercise',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lesson.component.html',
  styleUrl: './lesson.component.scss'
})
export class LessonComponent {
  private formBuilder = inject(NonNullableFormBuilder);

  exerciseTypes = [
    { value: 'multiple-choice', label: 'Multiple Choice' },
    { value: 'fill-in-blank', label: 'Fill in the Blank' },
    { value: 'translation', label: 'Translation' }
  ];

  lessonForm: LessonForm = this.formBuilder.group({
    title: ['', Validators.required],
    description: [''],
    estimatedDuration: [0, [Validators.required, Validators.min(1)]],
    content: [''],
    courseId: ['', Validators.required],
    exercises: [[] as Exercise[]]
  });

  // constructor(private fb: FormBuilder) {
  //   this.lessonForm = this.fb.group({
  //     title: ['', Validators.required],
  //     description: [''],
  //     estimatedDuration: [0, [Validators.required, Validators.min(1)]],
  //     content: [''],
  //     courseId: ['', Validators.required],
  //     exercises: 
  //   }) as LessonForm;
  // }

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
