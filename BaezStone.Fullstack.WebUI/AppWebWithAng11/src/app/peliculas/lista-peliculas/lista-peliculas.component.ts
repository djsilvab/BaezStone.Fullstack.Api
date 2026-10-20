import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-lista-peliculas',
  templateUrl: './lista-peliculas.component.html',
  styleUrls: ['./lista-peliculas.component.scss']
})
export class ListaPeliculasComponent implements OnInit {
  peliculas: Array<any> = [];

  constructor() {
  }

  ngOnInit(): void {
    setTimeout(() => {

      this.peliculas = [
        {
          titulo: 'El Padrino',
          director: 'Francis Ford Coppola',
          fecha: new Date("03-24-1972"),
          precio: 14.99
        },
        {
          titulo: 'El Padrino II',
          director: 'Francis Ford Coppola',
          fecha: new Date("12-20-1974"),
          precio: 14.99
        },
        {
          titulo: 'El Padrino III',
          director: 'Francis Ford Coppola',
          fecha: new Date("12-25-1990"),
          precio: 14.99
        }
      ]
    }, 2500);

  }

}
