const byteUnitDictionary = ['', 'K', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y'];
export function byteUnit(size: number): string {
  let i = 0;
  while (size > 1024) {
    size /= 1024;
    i++;
  }
  return `${size.toFixed(2)} ${byteUnitDictionary[i]}iB`;
}
