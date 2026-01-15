import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { HelpCategory, FaqItem } from './help.interface';

@Injectable({
  providedIn: 'root'
})
export class HelpService {
  
  getCategories(): Observable<HelpCategory[]> {
    return of([
      { id: 'start', name: 'Getting Started', icon: '🚀', description: 'Account setup and basics' },
      { id: 'billing', name: 'Billing & Plans', icon: '💳', description: 'Subscriptions and payments' },
      { id: 'learn', name: 'Learning Tools', icon: '🎓', description: 'Courses, XP, and streaks' },
      { id: 'tech', name: 'Technical Support', icon: '🔧', description: 'Bugs and troubleshooting' }
    ]);
  }

  getFaqs(): Observable<FaqItem[]> {
    return of([
      {
        id: '1',
        categoryId: 'start',
        question: 'How do I reset my password?',
        answer: 'To reset your password, go to the login page and click "Forgot Password". We will send a secure link to your registered email address.'
      },
      {
        id: '2',
        categoryId: 'learn',
        question: 'How is XP calculated?',
        answer: 'XP is earned by completing lessons, maintaining daily streaks, and unlocking achievements. Higher difficulty lessons award more XP.'
      },
      {
        id: '3',
        categoryId: 'billing',
        question: 'Can I pause my subscription?',
        answer: 'Yes, you can pause your Lexiq Pro subscription for up to 3 months from your Account Settings page under the "Billing" tab.'
      },
      {
        id: '4',
        categoryId: 'tech',
        question: 'The audio isn\'t playing in lessons',
        answer: 'Please ensure your device is not in "Silent Mode" and that browser permissions allow audio playback. If the issue persists, try clearing your cache.'
      },
      {
        id: '5',
        categoryId: 'start',
        question: 'Can I learn multiple languages at once?',
        answer: 'Absolutely! You can switch between language courses at any time from the dashboard. Your progress is saved individually for each language.'
      }
    ]);
  }
}