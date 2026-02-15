import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HelpService } from './help.service';
import { HelpCategory, FaqItem } from './help.interface';

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './help.component.html',
  styleUrls: ['./help.component.scss']
})
export class HelpComponent implements OnInit {
  private helpService = inject(HelpService);

  categories: HelpCategory[] = [];
  allFaqs: FaqItem[] = [];
  filteredFaqs: FaqItem[] = [];
  searchQuery = '';
  selectedCategory: string | null = null;

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
}
