import { Service } from '../service';

export class Thumbnailer extends Service {
  public static readonly MAX_WIDTH = 128;
  public static readonly MAX_HEIGHT = 128;

  public constructor() {
    super();
  }
}
