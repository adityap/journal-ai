import { apiClient } from './apiClient';

/**
 * Categories API Client
 * User-scoped category CRUD. Names are unique per user.
 */

export interface Category {
  id: string;
  name: string;
  color: string;
  parentId?: string | null;
  createdAt: string;
  entryCount: number;
}

export interface CreateCategoryRequest {
  name: string;
  color?: string;
  parentId?: string | null;
}

/**
 * List the current user's categories (with entry counts).
 */
export const listCategories = async (): Promise<Category[]> => {
  const response = await apiClient.get<Category[]>('/categories');
  return response.data;
};

/**
 * Create a new category. Throws on duplicate name (HTTP 409).
 */
export const createCategory = async (data: CreateCategoryRequest): Promise<Category> => {
  const response = await apiClient.post<Category>('/categories', data);
  return response.data;
};

/**
 * Delete a category. Entries keep existing; their category is cleared server-side.
 */
export const deleteCategory = async (id: string): Promise<void> => {
  await apiClient.delete(`/categories/${id}`);
};

/**
 * Build an id -> name lookup for resolving category references in the UI.
 */
export const buildCategoryNameMap = (categories: Category[]): Record<string, string> => {
  const map: Record<string, string> = {};
  for (const c of categories) {
    map[c.id] = c.name;
  }
  return map;
};
