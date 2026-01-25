import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { LessonService } from '../lesson/lesson.service';
import { Lesson } from '../lesson/lesson.interface';
import {
  AnyExercise,
  ExerciseType,
  MultipleChoiceExercise,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise
} from '../lesson/exercise.interface';

interface ExerciseAnswer {
  answer: string | string[];
  isSubmitted: boolean;
  isCorrect: boolean | null;
}

@Component({
  selector: 'app-lesson-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lesson-detail.html',
  styleUrl: './lesson-detail.scss'
})
export class LessonDetailComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);
  private lessonService = inject(LessonService);

  lesson: Lesson | null = null;
  currentView: 'content' | 'exercises' = 'content';
  currentExerciseIndex = 0;
  answers: Map<number, ExerciseAnswer> = new Map();

  // For tracking completion
  earnedXp = 0;
  totalPossibleXp = 0;

  // Expose enum to template
  ExerciseType = ExerciseType;

  ngOnInit() {
    // Get lesson ID from route params
    const lessonId = this.route.snapshot.paramMap.get('id');
    if (lessonId) {
      this.loadLesson(parseInt(lessonId, 10));
    }
  }

  private loadLesson(id: number) {
    // Try to find lesson from service's created lessons
    const createdLessons = this.lessonService.getCreatedLessons();
    this.lesson = createdLessons.find(l => l.id === id) || null;

    if (this.lesson) {
      this.initializeAnswers();
      this.calculateTotalXp();
    }
  }

  private initializeAnswers() {
    if (!this.lesson) return;

    this.lesson.exercises.forEach((_, index) => {
      this.answers.set(index, {
        answer: '',
        isSubmitted: false,
        isCorrect: null
      });
    });
  }

  private calculateTotalXp() {
    if (!this.lesson) return;
    this.totalPossibleXp = this.lesson.exercises.reduce((sum, ex) => sum + (ex.points || 10), 0);
  }

  // Content rendering
  parseContent(content: string): SafeHtml {
    if (!content) return this.sanitizer.bypassSecurityTrustHtml('');
    
    try {
      const data = JSON.parse(content);
      if (data && data.blocks && Array.isArray(data.blocks)) {
        let html = '';
        data.blocks.forEach((block: any) => {
          switch (block.type) {
            case 'header':
              html += `<h${block.data.level}>${block.data.text}</h${block.data.level}>`;
              break;
            case 'paragraph':
              html += `<p>${block.data.text}</p>`;
              break;
            case 'List':
            case 'list':
              const tag = block.data.style === 'ordered' ? 'ol' : 'ul';
              html += `<${tag}>`;
              block.data.items.forEach((item: string) => {
                html += `<li>${item}</li>`;
              });
              html += `</${tag}>`;
              break;
            case 'delimiter':
              html += '<hr />';
              break;
            case 'table':
              html += '<table>';
              if (block.data.withHeadings && block.data.content.length > 0) {
                html += '<thead><tr>';
                block.data.content[0].forEach((cell: string) => {
                  html += `<th>${cell}</th>`;
                });
                html += '</tr></thead><tbody>';
                block.data.content.slice(1).forEach((row: string[]) => {
                  html += '<tr>';
                  row.forEach((cell: string) => {
                    html += `<td>${cell}</td>`;
                  });
                  html += '</tr>';
                });
                html += '</tbody>';
              } else {
                html += '<tbody>';
                block.data.content.forEach((row: string[]) => {
                  html += '<tr>';
                  row.forEach((cell: string) => {
                    html += `<td>${cell}</td>`;
                  });
                  html += '</tr>';
                });
                html += '</tbody>';
              }
              html += '</table>';
              break;
            default:
              break;
          }
        });
        return this.sanitizer.bypassSecurityTrustHtml(html);
      }
    } catch (e) {
      // Not JSON, fallback to markdown
    }

    const html = marked(content, { async: false, breaks: true });
    return this.sanitizer.bypassSecurityTrustHtml(html as string);
  }

  // Navigation
  get currentExercise(): AnyExercise | null {
    if (!this.lesson || this.lesson.exercises.length === 0) return null;
    return this.lesson.exercises[this.currentExerciseIndex] as AnyExercise;
  }

  get currentAnswer(): ExerciseAnswer | null {
    return this.answers.get(this.currentExerciseIndex) || null;
  }

  get exerciseProgress(): number {
    if (!this.lesson || this.lesson.exercises.length === 0) return 0;
    const completed = Array.from(this.answers.values()).filter(a => a.isSubmitted).length;
    return (completed / this.lesson.exercises.length) * 100;
  }

  get allExercisesCompleted(): boolean {
    if (!this.lesson) return false;
    return Array.from(this.answers.values()).every(a => a.isSubmitted);
  }

  startExercises() {
    this.currentView = 'exercises';
    this.currentExerciseIndex = 0;
  }

  goToContent() {
    this.currentView = 'content';
  }

  previousExercise() {
    if (this.currentExerciseIndex > 0) {
      this.currentExerciseIndex--;
    }
  }

  nextExercise() {
    if (this.lesson && this.currentExerciseIndex < this.lesson.exercises.length - 1) {
      this.currentExerciseIndex++;
    }
  }

  // Answer handling
  updateAnswer(value: string) {
    const current = this.answers.get(this.currentExerciseIndex);
    if (current && !current.isSubmitted) {
      current.answer = value;
    }
  }

  selectMultipleChoiceOption(optionIndex: number) {
    const current = this.answers.get(this.currentExerciseIndex);
    if (current && !current.isSubmitted) {
      current.answer = optionIndex.toString();
    }
  }

  submitAnswer() {
    const current = this.answers.get(this.currentExerciseIndex);
    const exercise = this.currentExercise;

    if (!current || !exercise || current.isSubmitted) return;

    current.isSubmitted = true;
    current.isCorrect = this.checkAnswer(exercise, current.answer);

    if (current.isCorrect) {
      this.earnedXp += exercise.points || 10;
    }
  }

  private checkAnswer(exercise: AnyExercise, answer: string | string[]): boolean {
    const userAnswer = Array.isArray(answer) ? answer[0] : answer;

    switch (exercise.exerciseType) {
      case ExerciseType.MultipleChoice: {
        const mcExercise = exercise as MultipleChoiceExercise;
        const selectedIndex = parseInt(userAnswer, 10);
        return mcExercise.options[selectedIndex]?.isCorrect || false;
      }

      case ExerciseType.FillInBlank: {
        const fibExercise = exercise as FillInBlankExercise;
        let correctAnswer = fibExercise.correctAnswer;
        let userInput = userAnswer;

        if (fibExercise.trimWhitespace) {
          correctAnswer = correctAnswer.trim();
          userInput = userInput.trim();
        }

        if (!fibExercise.caseSensitive) {
          correctAnswer = correctAnswer.toLowerCase();
          userInput = userInput.toLowerCase();
        }

        // Check main answer
        if (userInput === correctAnswer) return true;

        // Check accepted alternatives
        if (fibExercise.acceptedAnswers) {
          const alternatives = fibExercise.acceptedAnswers.split(',').map(a => {
            let alt = a.trim();
            if (fibExercise.trimWhitespace) alt = alt.trim();
            if (!fibExercise.caseSensitive) alt = alt.toLowerCase();
            return alt;
          });
          return alternatives.includes(userInput);
        }

        return false;
      }

      case ExerciseType.Translation: {
        const transExercise = exercise as TranslationExercise;
        const userInput = userAnswer.trim().toLowerCase();
        const target = transExercise.targetText.trim().toLowerCase();

        // Simple similarity check - could be enhanced with fuzzy matching
        const similarity = this.calculateSimilarity(userInput, target);
        return similarity >= transExercise.matchingThreshold;
      }

      case ExerciseType.Listening: {
        const listenExercise = exercise as ListeningExercise;
        let correctAnswer = listenExercise.correctAnswer;
        let userInput = userAnswer;

        if (!listenExercise.caseSensitive) {
          correctAnswer = correctAnswer.toLowerCase();
          userInput = userInput.toLowerCase();
        }

        if (userInput.trim() === correctAnswer.trim()) return true;

        if (listenExercise.acceptedAnswers) {
          const alternatives = listenExercise.acceptedAnswers.split(',').map(a => {
            let alt = a.trim();
            if (!listenExercise.caseSensitive) alt = alt.toLowerCase();
            return alt;
          });
          return alternatives.includes(userInput.trim());
        }

        return false;
      }

      default:
        return false;
    }
  }

  private calculateSimilarity(str1: string, str2: string): number {
    // Simple Levenshtein-based similarity
    const longer = str1.length > str2.length ? str1 : str2;
    const shorter = str1.length > str2.length ? str2 : str1;

    if (longer.length === 0) return 1.0;

    const editDistance = this.levenshteinDistance(longer, shorter);
    return (longer.length - editDistance) / longer.length;
  }

  private levenshteinDistance(str1: string, str2: string): number {
    const matrix: number[][] = [];

    for (let i = 0; i <= str2.length; i++) {
      matrix[i] = [i];
    }

    for (let j = 0; j <= str1.length; j++) {
      matrix[0][j] = j;
    }

    for (let i = 1; i <= str2.length; i++) {
      for (let j = 1; j <= str1.length; j++) {
        if (str2.charAt(i - 1) === str1.charAt(j - 1)) {
          matrix[i][j] = matrix[i - 1][j - 1];
        } else {
          matrix[i][j] = Math.min(
            matrix[i - 1][j - 1] + 1,
            matrix[i][j - 1] + 1,
            matrix[i - 1][j] + 1
          );
        }
      }
    }

    return matrix[str2.length][str1.length];
  }

  // Completion
  finishLesson() {
    // Could update lesson status here via service
    this.router.navigate(['/']);
  }

  goBack() {
    this.router.navigate(['/']);
  }

  // Helper for multiple choice
  isOptionSelected(optionIndex: number): boolean {
    const current = this.currentAnswer;
    return current?.answer === optionIndex.toString();
  }

  getOptionClass(optionIndex: number, isCorrect: boolean): string {
    const current = this.currentAnswer;
    if (!current?.isSubmitted) {
      return this.isOptionSelected(optionIndex) ? 'selected' : '';
    }

    // After submission
    if (isCorrect) return 'correct';
    if (this.isOptionSelected(optionIndex) && !isCorrect) return 'incorrect';
    return '';
  }
}
