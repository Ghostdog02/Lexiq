import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './error-page.component.html',
  styleUrl: './error-page.component.scss',
})
export class ErrorPageComponent implements OnInit {
  private route = inject(ActivatedRoute);

  statusCode = 404;
  title = 'Page not found';
  message = "The page you're looking for doesn't exist or has been moved.";

  private static readonly STATUS_MAP: Record<number, { title: string; message: string }> = {
    400: { title: 'Bad request', message: 'The server could not understand the request.' },
    403: { title: 'Forbidden', message: "You don't have permission to access this resource." },
    404: { title: 'Page not found', message: "The page you're looking for doesn't exist or has been moved." },
    500: { title: 'Server error', message: 'Something went wrong on our end. Please try again later.' },
  };

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const status = Number(params.get('status'));

    if (status && !isNaN(status)) {
      this.statusCode = status;
      const mapped = ErrorPageComponent.STATUS_MAP[status];
      this.title = mapped?.title ?? 'Something went wrong';
      this.message = params.get('message') ?? mapped?.message ?? 'An unexpected error occurred.';
    }
  }
}
