import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  title = 'Introduccion a Angular';
  subtitle = 'Primeros Pasos';  

  ngOnInit(): void {
  }

  duplicarNumero(n1: number): number {
    return n1 * 2;
  }


}
