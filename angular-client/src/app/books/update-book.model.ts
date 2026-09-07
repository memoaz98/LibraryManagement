export interface UpdateBook {
  title: string;
  isbn: string;
  publicationYear: number;
  categoryId: number;
  synopsis: string | null;
}
