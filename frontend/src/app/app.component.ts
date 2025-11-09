import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { LoansService } from './services/loans.service';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  private service = inject(LoansService);
  private authService = inject(AuthService);
  loans: any[] = [];

  displayedColumns: string[] = [
    'loanAmount',
    'currentBalance',
    'applicant',
    'status',
  ];

  ngOnInit() {
    this.authService.getToken().subscribe({
      next: () => {
        this.loadLoans();
      },
      error: (error) => {
        console.error('Error getting token:', error);
      }
    });
  }

  private loadLoans() {
    this.service.getLoans().subscribe({
      next: (data: any) => {
        this.loans = data;
      },
      error: (error) => {
        console.error('Error to get data:', error);
      }
    });
  }
}
