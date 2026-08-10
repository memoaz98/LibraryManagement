export interface Book {
  id: number;
  categoryId: number;
  title: string;
  isbn: string;
  publicationYear: number;
  synopsis: string | null;
  createdAt: string;
}
