import { Routes } from '@angular/router';
import { CvMatchComponent } from './components/cv-match/cv-match.component';

export const routes: Routes = [
  { path: '', component: CvMatchComponent },
  { path: '**', redirectTo: '' },
];
