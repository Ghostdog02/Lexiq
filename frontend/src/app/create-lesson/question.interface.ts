export interface QuestionOption {
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface Question {
  exerciseName: string;
  questionText: string;
  orderIndex: number;
  points: number;
  explanation?: string;
  questionType: 'MultipleChoice' | 'FillInBlank' | 'Listening' | 'Translation';
}

export interface MultipleChoiceQuestion extends Question {
  questionType: 'MultipleChoice';
  options: QuestionOption[];
}

export interface FillInBlankQuestion extends Question {
  questionType: 'FillInBlank';
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  trimWhitespace: boolean;
}

export interface ListeningQuestion extends Question {
  questionType: 'Listening';
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  maxReplays: number;
}

export interface TranslationQuestion extends Question {
  questionType: 'Translation';
  sourceLanguageCode: string;
  targetLanguageCode: string;
  matchingThreshold: number;
}
