import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CvMatchResult } from '../models/cv-match-result';

@Injectable({
  providedIn: 'root',
})
export class CvMatchService {
  constructor(private readonly http: HttpClient) {}

  analyze(cvPdf: File, jobDescription: string): Observable<CvMatchResult> {
    const formData = new FormData();
    formData.append('cvPdf', cvPdf, cvPdf.name);
    formData.append('jobDescription', jobDescription);

    return this.http.post<CvMatchResult>(
      `${environment.apiUrl}/api/cv-match`,
      formData
    );
  }
}
