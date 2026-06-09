import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CvMatchService } from './cv-match.service';
import { CvMatchResult } from '../models/cv-match-result';

describe('CvMatchService', () => {
  let service: CvMatchService;
  let httpMock: HttpTestingController;

  const mockResult: CvMatchResult = {
    matchScore: 77,
    matchedSkills: ['C#'],
    skillGaps: ['Azure'],
    recommendations: ['Add cloud work'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CvMatchService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CvMatchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('analyze should post multipart form data to the api endpoint', () => {
    const file = new File(['pdf-content'], 'resume.pdf', {
      type: 'application/pdf',
    });
    const jobDescription = 'Senior .NET developer role';

    let actualResult: CvMatchResult | undefined;
    service.analyze(file, jobDescription).subscribe((result) => {
      actualResult = result;
    });

    const request = httpMock.expectOne('/api/cv-match');
    expect(request.request.method).toBe('POST');
    expect(request.request.body instanceof FormData).toBeTrue();

    const formData = request.request.body as FormData;
    expect(formData.get('jobDescription')).toBe(jobDescription);
    expect(formData.get('cvPdf')).toBeTruthy();

    request.flush(mockResult);

    expect(actualResult).toEqual(mockResult);
  });
});
