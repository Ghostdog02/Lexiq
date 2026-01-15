export interface HelpCategory {
  id: string;
  name: string;
  icon: string;
  description: string;
}

export interface FaqItem {
  id: string;
  categoryId: string;
  question: string;
  answer: string;
  isOpen?: boolean; // For accordion state
}