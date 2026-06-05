import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CvMatchResult } from '../../models/cv-match-result';
import { CvMatchService } from '../../services/cv-match.service';

@Component({
  selector: 'app-cv-match',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cv-match.component.html',
  styleUrl: './cv-match.component.css',
})
export class CvMatchComponent {
  private readonly cvMatchService = inject(CvMatchService);

  selectedFile: File | null = null;
  jobDescription = '';
  loading = false;
  errorMessage: string | null = null;
  result: CvMatchResult | null = null;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.selectedFile = null;
      return;
    }

    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      this.errorMessage = 'Please select a PDF file.';
      this.selectedFile = null;
      input.value = '';
      return;
    }

    this.errorMessage = null;
    this.selectedFile = file;
  }

  submit(): void {
    this.errorMessage = null;
    this.result = null;

    if (!this.selectedFile) {
      this.errorMessage = 'Please upload your CV as a PDF.';
      return;
    }

    if (!this.jobDescription.trim()) {
      this.errorMessage = 'Please enter a job description.';
      return;
    }

    this.loading = true;

    this.cvMatchService
      .analyze(this.selectedFile, this.jobDescription.trim())
      .subscribe({
        next: (response) => {
          this.result = response;
          this.loading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage = this.resolveErrorMessage(error);
        },
      });
  }

  reset(): void {
    this.selectedFile = null;
    this.jobDescription = '';
    this.result = null;
    this.errorMessage = null;
  }

  scoreLabel(score: number): string {
    if (score >= 80) {
      return 'Strong match';
    }
    if (score >= 60) {
      return 'Good match';
    }
    if (score >= 40) {
      return 'Partial match';
    }
    return 'Low match';
  }

  private resolveErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Cannot reach the API. Ensure the .NET app is running on http://localhost:5196.';
    }

    const detail = error.error?.detail ?? error.error?.title;
    if (typeof detail === 'string' && detail.length > 0) {
      return detail;
    }

    return error.message || 'Something went wrong. Please try again.';
  }
}
