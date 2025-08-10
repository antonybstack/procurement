import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-search-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './search-layout.component.html',
  styleUrl: './search-layout.component.css'
})
export class SearchLayoutComponent {
  constructor() { }
}
