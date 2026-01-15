import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HelpService } from './help.service';
import { HelpCategory, FaqItem } from './help.interface';

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './help.component.html',
  styleUrls: ['./help.component.scss']
})
export class HelpComponent implements OnInit {
  categories: HelpCategory[] = [];
  allFaqs: FaqItem[] = [];
  filteredFaqs: FaqItem[] = [];
  searchQuery: string = '';
  selectedCategory: string | null = null;
  isFocused: boolean = false; // Tracks search bar glow

  constructor(private helpService: HelpService) {}

  ngOnInit(): void {
    this.helpService.getCategories().subscribe(cats => this.categories = cats);
    this.helpService.getFaqs().subscribe(faqs => {
      this.allFaqs = faqs;
      this.filteredFaqs = faqs;
    });
  }

  toggleFaq(item: FaqItem): void {
    this.allFaqs.forEach(f => {
      if (f.id !== item.id) 
        f.isOpen = false;
    });
    item.isOpen = !item.isOpen;
  }

  filterFaqs(): void {
    const query = this.searchQuery.toLowerCase();
    
    this.filteredFaqs = this.allFaqs.filter(faq => {
      const matchesSearch = faq.question.toLowerCase().includes(query) || 
                            faq.answer.toLowerCase().includes(query);
      const matchesCategory = this.selectedCategory ? faq.categoryId === this.selectedCategory : true;
      
      return matchesSearch && matchesCategory;
    });
  }

  selectCategory(categoryId: string | null): void {
    this.selectedCategory = this.selectedCategory === categoryId ? null : categoryId;
    this.filterFaqs();
  }

  triggerShake(elementId: string) {
    const el = document.getElementById(elementId);
    if (el) {
      el.classList.add('shake-anim');
      setTimeout(() => el.classList.remove('shake-anim'), 500);
    }
  } 
}