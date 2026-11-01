import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  title = 'Introduccion a Angular';
  subtitle = 'Primeros Pasos';
  peliculasEpoca80: Array<any> = [];

  ngOnInit(): void {
    setTimeout(() => {
      this.peliculasEpoca80 = [
        {
          titulo: 'Terminator 1',
          director: 'James Cameron',
          fecha: new Date('10-26-1984'),
          precio: 14.99,
        },
        {
          titulo: 'Volver al Futuro',
          director: 'Robert Zemeckis',
          fecha: new Date('07-03-1985'),
          precio: 10.79,
        },
          {
          titulo: 'Terminator 2',
          director: 'James Cameron',
          fecha: new Date('10-26-1984'),
          precio: 14.99,
        },
        {
          titulo: 'Volver al Futuro 2',
          director: 'Robert Zemeckis',
          fecha: new Date('07-03-1985'),
          precio: 10.79,
        },
      ];
    }, 2500);
  }

  duplicarNumero(n1: number): number {
    return n1 * 2;
  }
}
