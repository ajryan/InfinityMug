export interface MoveStart {
    active: boolean;
    type: string | null;
    position: MovePosition | null;
    x1: number;
    y1: number;
    x2: number;
    y2: number;
    clientX: number;
    clientY: number;
}

export enum MovePosition {
  Left = 'left',
  TopLeft = 'topleft',
  Top = 'top',
  TopRight = 'topright',
  Right = 'right',
  BottomRight = 'bottomright',
  Bottom = 'bottom',
  BottomLeft = 'bottomleft'
}
