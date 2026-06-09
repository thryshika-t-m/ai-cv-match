import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { CvMatchComponent } from './cv-match.component';
import { CvMatchService } from '../../services/cv-match.service';
import { CvMatchResult } from '../../models/cv-match-result';

describe('CvMatchComponent', () => {
  let component: CvMatchComponent;
  let fixture: ComponentFixture<CvMatchComponent>;
  let cvMatchService: jasmine.SpyObj<CvMatchService>;

  const mockResult: CvMatchResult = {
    matchScore: 82,
    matchedSkills: ['C#'],
    skillGaps: ['Azure'],
    recommendations: ['Add cloud examples'],
  };

  beforeEach(async () => {
    cvMatchService = jasmine.createSpyObj<CvMatchService>('CvMatchService', ['analyze']);

    await TestBed.configureTestingModule({
      imports: [CvMatchComponent],
      providers: [{ provide: CvMatchService, useValue: cvMatchService }],
    }).compileComponents();

    fixture = TestBed.createComponent(CvMatchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('onFileSelected should reject non-pdf files', () => {
    const input = document.createElement('input');
    const file = new File(['content'], 'resume.txt', { type: 'text/plain' });
    Object.defineProperty(input, 'files', { value: [file] });

    component.onFileSelected({ target: input } as unknown as Event);

    expect(component.selectedFile).toBeNull();
    expect(component.errorMessage).toBe('Please select a PDF file.');
  });

  it('onFileSelected should accept pdf files', () => {
    const input = document.createElement('input');
    const file = new File(['content'], 'resume.pdf', { type: 'application/pdf' });
    Object.defineProperty(input, 'files', { value: [file] });

    component.onFileSelected({ target: input } as unknown as Event);

    expect(component.selectedFile).toBe(file);
    expect(component.errorMessage).toBeNull();
  });

  it('submit should require a pdf file', () => {
    component.submit();

    expect(component.errorMessage).toBe('Please upload your CV as a PDF.');
    expect(cvMatchService.analyze).not.toHaveBeenCalled();
  });

  it('submit should require a job description', () => {
    component.selectedFile = new File(['content'], 'resume.pdf', {
      type: 'application/pdf',
    });

    component.submit();

    expect(component.errorMessage).toBe('Please enter a job description.');
    expect(cvMatchService.analyze).not.toHaveBeenCalled();
  });

  it('submit should call service and store result on success', () => {
    const file = new File(['content'], 'resume.pdf', { type: 'application/pdf' });
    component.selectedFile = file;
    component.jobDescription = '  Senior .NET role  ';
    cvMatchService.analyze.and.returnValue(of(mockResult));

    component.submit();

    expect(cvMatchService.analyze).toHaveBeenCalledWith(file, 'Senior .NET role');
    expect(component.result).toEqual(mockResult);
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBeNull();
  });

  it('submit should show api detail on http error', () => {
    component.selectedFile = new File(['content'], 'resume.pdf', {
      type: 'application/pdf',
    });
    component.jobDescription = 'Job description';
    cvMatchService.analyze.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 502,
            error: { detail: 'Gemini quota exceeded' },
          })
      )
    );

    component.submit();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('Gemini quota exceeded');
  });

  it('submit should show unreachable message when status is 0', () => {
    component.selectedFile = new File(['content'], 'resume.pdf', {
      type: 'application/pdf',
    });
    component.jobDescription = 'Job description';
    cvMatchService.analyze.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 0 }))
    );

    component.submit();

    expect(component.errorMessage).toContain('Cannot reach the API');
  });

  it('reset should clear form state', () => {
    component.selectedFile = new File(['content'], 'resume.pdf', {
      type: 'application/pdf',
    });
    component.jobDescription = 'Job description';
    component.result = mockResult;
    component.errorMessage = 'Error';

    component.reset();

    expect(component.selectedFile).toBeNull();
    expect(component.jobDescription).toBe('');
    expect(component.result).toBeNull();
    expect(component.errorMessage).toBeNull();
  });

  it('scoreLabel should return expected labels', () => {
    expect(component.scoreLabel(85)).toBe('Strong match');
    expect(component.scoreLabel(70)).toBe('Good match');
    expect(component.scoreLabel(50)).toBe('Partial match');
    expect(component.scoreLabel(20)).toBe('Low match');
  });
});
