import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { LoansService } from './services/loans.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  private service = inject(LoansService);
  loans: any[] = []

  ngOnInit() {
    this.service.getLoans().subscribe({
      next: (data: any) => {
        this.loans = data;
        console.log('Data loaded:', this.loans);
      },
      error: (error) => {
        console.error('Error to get data:', error);
      }
    });
  }

  displayedColumns: string[] = [
    'loanAmount',
    'currentBalance',
    'applicant',
    'status',
  ];
}
