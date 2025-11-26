import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CoursesService } from './courses.service';
import { ScrollingModule } from '@angular/cdk/scrolling';

interface Course {
  id: number;
  title: string;
  description: string;
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced';
  estimatedHours: number;
  isLocked: boolean;
  progress: number; // 0-100
  icon: string;
}

interface SkillLevel {
  level: string;
  courses: Course[];
}

interface DailyQuest {
  id: number;
  title: string;
  description: string;
  progress: number;
  maxProgress: number;
  xpReward: number;
  icon: string;
}

@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, ScrollingModule],
  templateUrl: './courses.component.html',
  styleUrl: './courses.component.scss'
})
export class CoursesComponent implements OnInit{
  courseService = inject(CoursesService);

  courses: Course[] = [];
  
  ngOnInit() {
    this.loadCourses();
  }

  loadCourses() {

  }


  // User stats
  userXP = 2450;
  userStreak = 7;
  userLeaderboardPosition = 12;

  // Daily quests
  dailyQuests: DailyQuest[] = [
    {
      id: 1,
      title: 'Complete 3 Lessons',
      description: 'Finish 3 lessons today',
      progress: 2,
      maxProgress: 3,
      xpReward: 50,
      icon: '📚'
    },
    {
      id: 2,
      title: 'Practice 20 Words',
      description: 'Review vocabulary',
      progress: 15,
      maxProgress: 20,
      xpReward: 30,
      icon: '📝'
    },
    {
      id: 3,
      title: 'Maintain Streak',
      description: 'Login daily',
      progress: 1,
      maxProgress: 1,
      xpReward: 25,
      icon: '🔥'
    }
  ];

  skillLevels: SkillLevel[] = [
    {
      level: 'Beginner',
      courses: [
        {
          id: 1,
          title: 'Italian Alphabet & Pronunciation',
          description: 'Master the Italian alphabet and perfect your pronunciation',
          difficulty: 'Beginner',
          estimatedHours: 2,
          isLocked: false,
          progress: 100,
          icon: '🔤'
        },
        {
          id: 2,
          title: 'Basic Greetings & Phrases',
          description: 'Learn essential Italian greetings and everyday phrases',
          difficulty: 'Beginner',
          estimatedHours: 3,
          isLocked: false,
          progress: 65,
          icon: '👋'
        },
        {
          id: 3,
          title: 'Present Tense (Presente Indicativo)',
          description: 'Conjugate regular and irregular verbs in the present tense',
          difficulty: 'Beginner',
          estimatedHours: 5,
          isLocked: false,
          progress: 30,
          icon: '⏰'
        },
        {
          id: 4,
          title: 'Definite & Indefinite Articles',
          description: 'Master il, lo, la, i, gli, le and un, uno, una',
          difficulty: 'Beginner',
          estimatedHours: 3,
          isLocked: true,
          progress: 0,
          icon: '📝'
        },
        {
          id: 5,
          title: 'Basic Nouns & Adjectives',
          description: 'Learn common Italian nouns and descriptive adjectives',
          difficulty: 'Beginner',
          estimatedHours: 4,
          isLocked: true,
          progress: 0,
          icon: '📖'
        }
      ]
    },
    {
      level: 'Intermediate',
      courses: [
        {
          id: 6,
          title: 'Past Tenses',
          description: 'Master Passato Prossimo and Imperfetto',
          difficulty: 'Intermediate',
          estimatedHours: 6,
          isLocked: true,
          progress: 0,
          icon: '⏮️'
        },
        {
          id: 7,
          title: 'Future Tense',
          description: 'Express future actions with Futuro Semplice',
          difficulty: 'Intermediate',
          estimatedHours: 4,
          isLocked: true,
          progress: 0,
          icon: '🔮'
        },
        {
          id: 8,
          title: 'Pronouns & Prepositions',
          description: 'Navigate Italian pronouns and common prepositions',
          difficulty: 'Intermediate',
          estimatedHours: 5,
          isLocked: true,
          progress: 0,
          icon: '🎯'
        },
        {
          id: 9,
          title: 'Irregular Verbs Mastery',
          description: 'Conquer the most common irregular Italian verbs',
          difficulty: 'Intermediate',
          estimatedHours: 7,
          isLocked: true,
          progress: 0,
          icon: '⚡'
        },
        {
          id: 10,
          title: 'Conversational Italian',
          description: 'Practice real-world conversations and dialogues',
          difficulty: 'Intermediate',
          estimatedHours: 8,
          isLocked: true,
          progress: 0,
          icon: '💬'
        }
      ]
    },
    {
      level: 'Advanced',
      courses: [
        {
          id: 11,
          title: 'Subjunctive Mood (Congiuntivo)',
          description: 'Master the complex Italian subjunctive mood',
          difficulty: 'Advanced',
          estimatedHours: 10,
          isLocked: true,
          progress: 0,
          icon: '🤔'
        },
        {
          id: 12,
          title: 'Conditional Tense',
          description: 'Express hypothetical situations with confidence',
          difficulty: 'Advanced',
          estimatedHours: 6,
          isLocked: true,
          progress: 0,
          icon: '❓'
        },
        {
          id: 13,
          title: 'Advanced Grammar Structures',
          description: 'Perfect your understanding of complex grammar',
          difficulty: 'Advanced',
          estimatedHours: 12,
          isLocked: true,
          progress: 0,
          icon: '📚'
        },
        {
          id: 14,
          title: 'Idiomatic Expressions',
          description: 'Sound like a native with Italian idioms and sayings',
          difficulty: 'Advanced',
          estimatedHours: 5,
          isLocked: true,
          progress: 0,
          icon: '🗣️'
        },
        {
          id: 15,
          title: 'Business Italian',
          description: 'Professional Italian for workplace communication',
          difficulty: 'Advanced',
          estimatedHours: 8,
          isLocked: true,
          progress: 0,
          icon: '💼'
        }
      ]
    }
  ];
}
