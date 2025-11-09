import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class LoansService {
  private apiUrl = "http://localhost:56702/";
  constructor(private http: HttpClient) { }

  getLoans() {
    return this.http.get(`${this.apiUrl}loans`);
  }

  getLoanById(id: string) {
    return this.http.get(`${this.apiUrl}loans/${id}`);
  }

  createLoan(loanData: any) {
    return this.http.post(`${this.apiUrl}loans`, loanData);
  }

}
