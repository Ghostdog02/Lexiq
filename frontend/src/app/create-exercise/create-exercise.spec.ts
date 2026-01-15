import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateExercise } from './create-exercise';

describe('CreateExercise', () => {
  let component: CreateExercise;
  let fixture: ComponentFixture<CreateExercise>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateExercise]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateExercise);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
