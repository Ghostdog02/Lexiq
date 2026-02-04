// form-validation.service.ts
import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

@Injectable({ providedIn: 'root' })
export class FormValidationService {
  
  // Custom validators
  minValue(min: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (control.value === null || control.value === undefined || control.value === '') {
        return null;
      }
      return control.value >= min ? null : { minValue: { min, actual: control.value } };
    };
  }

  maxValue(max: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (control.value === null || control.value === undefined || control.value === '') {
        return null;
      }
      return control.value <= max ? null : { maxValue: { max, actual: control.value } };
    };
  }

  // Get error messages
  getErrorMessage(control: AbstractControl | null, fieldName: string): string {
    if (!control || !control.errors || !control.touched) {
      return '';
    }

    if (control.errors['required']) {
      return `${fieldName} is required`;
    }

    if (control.errors['min']) {
      return `${fieldName} must be at least ${control.errors['min'].min}`;
    }

    if (control.errors['max']) {
      return `${fieldName} must be at most ${control.errors['max'].max}`;
    }
    
    if (control.errors['minlength']) {
      return `${fieldName} must be at least ${control.errors['minlength'].requiredLength} characters`;
    }

    return 'Invalid value';
  }
}