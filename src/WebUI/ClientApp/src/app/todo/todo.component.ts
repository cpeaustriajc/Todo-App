import { Component, TemplateRef, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import {
  TodoListsClient, TodoItemsClient, TagsClient,
  TodoListDto, TodoItemDto, PriorityLevelDto, TagDto,
  CreateTodoListCommand, UpdateTodoListCommand,
  CreateTodoItemCommand, UpdateTodoItemDetailCommand,
  CreateTagCommand
} from '../web-api-client';

@Component({
  selector: 'app-todo-component',
  templateUrl: './todo.component.html',
  styleUrls: ['./todo.component.scss']
})
export class TodoComponent implements OnInit {
  debug = false;
  deleting = false;
  deleteCountDown = 0;
  deleteCountDownInterval: any;
  lists: TodoListDto[];
  priorityLevels: PriorityLevelDto[];
  selectedList: TodoListDto;
  selectedItem: TodoItemDto;
  newListEditor: any = {};
  listOptionsEditor: any = {};
  newListModalRef: BsModalRef;
  listOptionsModalRef: BsModalRef;
  deleteListModalRef: BsModalRef;
  itemDetailsModalRef: BsModalRef;

  // Tag-related properties
  availableTags: TagDto[] = [];
  filteredTags: TagDto[] = [];
  selectedTags: TagDto[] = [];
  tagSearchTerm: string = '';
  newTagName: string = '';
  showTagDropdown: boolean = false;

  // Search functionality
  searchTerm: string = '';

  // Bulk operations
  selectedItems: Set<number> = new Set();
  bulkMode: boolean = false;

  // Advanced search options
  searchInTitle: boolean = true;
  searchInNote: boolean = true;
  searchInTags: boolean = true;
  showSearchOptions: boolean = false;

  itemDetailsFormGroup = this.fb.group({
    id: [null],
    listId: [null],
    priority: [''],
    note: ['']
  });


  constructor(
    private listsClient: TodoListsClient,
    private itemsClient: TodoItemsClient,
    private tagsClient: TagsClient,
    private modalService: BsModalService,
    private fb: FormBuilder
  ) { }

  ngOnInit(): void {
    this.listsClient.get().subscribe(
      result => {
        this.lists = result.lists;
        this.priorityLevels = result.priorityLevels;
        if (this.lists.length) {
          this.selectedList = this.lists[0];
        }
      },
      error => console.error(error)
    );

    this.loadTags();
    this.setupKeyboardShortcuts();
  }

  setupKeyboardShortcuts(): void {
    document.addEventListener('keydown', (event: KeyboardEvent) => {
      // Ctrl/Cmd + F for search focus
      if ((event.ctrlKey || event.metaKey) && event.key === 'f') {
        event.preventDefault();
        const searchInput = document.querySelector('#search-input') as HTMLInputElement;
        if (searchInput) {
          searchInput.focus();
        }
      }

      // Escape to clear filters
      if (event.key === 'Escape') {
        if (this.searchTerm || this.selectedTags.length > 0) {
          this.clearAllFilters();
        }
        if (this.bulkMode) {
          this.toggleBulkMode();
        }
      }

      // Ctrl/Cmd + A to select all in bulk mode
      if (this.bulkMode && (event.ctrlKey || event.metaKey) && event.key === 'a') {
        event.preventDefault();
        this.selectAllItems();
      }
    });
  }

  loadTags(): void {
    this.tagsClient.get().subscribe(
      result => {
        this.availableTags = result;
        this.filteredTags = [...this.availableTags];
      },
      error => console.error(error)
    );
  }

  // Lists
  remainingItems(list: TodoListDto): number {
    return list.items.filter(t => !t.done).length;
  }

  showNewListModal(template: TemplateRef<any>): void {
    this.newListModalRef = this.modalService.show(template);
    setTimeout(() => document.getElementById('title').focus(), 250);
  }

  newListCancelled(): void {
    this.newListModalRef.hide();
    this.newListEditor = {};
  }

  addList(): void {
    const list = {
      id: 0,
      title: this.newListEditor.title,
      items: []
    } as TodoListDto;

    this.listsClient.create(list as CreateTodoListCommand).subscribe(
      result => {
        list.id = result;
        this.lists.push(list);
        this.selectedList = list;
        this.newListModalRef.hide();
        this.newListEditor = {};
      },
      error => {
        const errors = JSON.parse(error.response);

        if (errors && errors.Title) {
          this.newListEditor.error = errors.Title[0];
        }

        setTimeout(() => document.getElementById('title').focus(), 250);
      }
    );
  }

  showListOptionsModal(template: TemplateRef<any>) {
    this.listOptionsEditor = {
      id: this.selectedList.id,
      title: this.selectedList.title
    };

    this.listOptionsModalRef = this.modalService.show(template);
  }

  updateListOptions() {
    const list = this.listOptionsEditor as UpdateTodoListCommand;
    this.listsClient.update(this.selectedList.id, list).subscribe(
      () => {
        (this.selectedList.title = this.listOptionsEditor.title),
          this.listOptionsModalRef.hide();
        this.listOptionsEditor = {};
      },
      error => console.error(error)
    );
  }

  confirmDeleteList(template: TemplateRef<any>) {
    this.listOptionsModalRef.hide();
    this.deleteListModalRef = this.modalService.show(template);
  }

  deleteListConfirmed(): void {
    this.listsClient.delete(this.selectedList.id).subscribe(
      () => {
        this.deleteListModalRef.hide();
        this.lists = this.lists.filter(t => t.id !== this.selectedList.id);
        this.selectedList = this.lists.length ? this.lists[0] : null;
      },
      error => console.error(error)
    );
  }

  // Items
  showItemDetailsModal(template: TemplateRef<any>, item: TodoItemDto): void {
    this.selectedItem = item;
    this.itemDetailsFormGroup.patchValue(this.selectedItem);

    this.itemDetailsModalRef = this.modalService.show(template);
    this.itemDetailsModalRef.onHidden.subscribe(() => {
        this.stopDeleteCountDown();
    });
  }

  updateItemDetails(): void {
    const item = new UpdateTodoItemDetailCommand(this.itemDetailsFormGroup.value);
    this.itemsClient.updateItemDetails(this.selectedItem.id, item).subscribe(
      () => {
        if (this.selectedItem.listId !== item.listId) {
          this.selectedList.items = this.selectedList.items.filter(
            i => i.id !== this.selectedItem.id
          );
          const listIndex = this.lists.findIndex(
            l => l.id === item.listId
          );
          this.selectedItem.listId = item.listId;
          this.lists[listIndex].items.push(this.selectedItem);
        }

        this.selectedItem.priority = item.priority;
        this.selectedItem.note = item.note;
        this.itemDetailsModalRef.hide();
        this.itemDetailsFormGroup.reset();
      },
      error => console.error(error)
    );
  }

  addItem() {
    const item = {
      id: 0,
      listId: this.selectedList.id,
      priority: this.priorityLevels[0].value,
      title: '',
      done: false
    } as TodoItemDto;

    this.selectedList.items.push(item);
    const index = this.selectedList.items.length - 1;
    this.editItem(item, 'itemTitle' + index);
  }

  editItem(item: TodoItemDto, inputId: string): void {
    this.selectedItem = item;
    setTimeout(() => document.getElementById(inputId).focus(), 100);
  }

  updateItem(item: TodoItemDto, pressedEnter: boolean = false): void {
    const isNewItem = item.id === 0;

    if (!item.title.trim()) {
      this.deleteItem(item);
      return;
    }

    if (item.id === 0) {
      this.itemsClient
        .create({
          ...item, listId: this.selectedList.id
        } as CreateTodoItemCommand)
        .subscribe(
          result => {
            item.id = result;
          },
          error => console.error(error)
        );
    } else {
      this.itemsClient.update(item.id, item).subscribe(
        () => console.log('Update succeeded.'),
        error => console.error(error)
      );
    }

    this.selectedItem = null;

    if (isNewItem && pressedEnter) {
      setTimeout(() => this.addItem(), 250);
    }
  }

  deleteItem(item: TodoItemDto, countDown?: boolean) {
    if (countDown) {
      if (this.deleting) {
        this.stopDeleteCountDown();
        return;
      }
      this.deleteCountDown = 3;
      this.deleting = true;
      this.deleteCountDownInterval = setInterval(() => {
        if (this.deleting && --this.deleteCountDown <= 0) {
          this.deleteItem(item, false);
        }
      }, 1000);
      return;
    }
    this.deleting = false;
    if (this.itemDetailsModalRef) {
      this.itemDetailsModalRef.hide();
    }

    if (item.id === 0) {
      const itemIndex = this.selectedList.items.indexOf(this.selectedItem);
      this.selectedList.items.splice(itemIndex, 1);
    } else {
      this.itemsClient.delete(item.id).subscribe(
        () =>
        (this.selectedList.items = this.selectedList.items.filter(
          t => t.id !== item.id
        )),
        error => console.error(error)
      );
    }
  }

  stopDeleteCountDown() {
    clearInterval(this.deleteCountDownInterval);
    this.deleteCountDown = 0;
    this.deleting = false;
  }

  // Tag management methods

  filterTags(): void {
    if (!this.tagSearchTerm) {
      this.filteredTags = [...this.availableTags];
    } else {
      this.filteredTags = this.availableTags.filter(tag =>
        tag.name.toLowerCase().includes(this.tagSearchTerm.toLowerCase())
      );
    }
  }

  addTagToItem(item: TodoItemDto, tag: TagDto): void {
    if (!item.id || !tag.id) return;

    this.tagsClient.addTagToTodoItem(item.id, tag.id).subscribe(
      () => {
        if (!item.tags) {
          item.tags = [];
        }
        if (!item.tags.find(t => t.id === tag.id)) {
          item.tags.push(tag);
        }
        this.loadTags(); // Refresh tag usage counts
      },
      error => console.error(error)
    );
  }

  removeTagFromItem(item: TodoItemDto, tag: TagDto): void {
    if (!item.id || !tag.id) return;

    this.tagsClient.removeTagFromTodoItem(item.id, tag.id).subscribe(
      () => {
        if (item.tags) {
          item.tags = item.tags.filter(t => t.id !== tag.id);
        }
        this.loadTags(); // Refresh tag usage counts
      },
      error => console.error(error)
    );
  }

  createNewTag(): void {
    if (!this.newTagName.trim()) return;

    const command = new CreateTagCommand({
      name: this.newTagName.trim(),
      color: this.getRandomTagColor()
    });

    this.tagsClient.create(command).subscribe(
      (tagId: number) => {
        this.newTagName = '';
        this.loadTags();
      },
      error => console.error(error)
    );
  }

  createTagFromSearch(): void {
    this.newTagName = this.tagSearchTerm;
    this.createNewTag();
    this.showTagDropdown = false;
  }

  getRandomTagColor(): string {
    const colors = [
      '#007bff', '#6c757d', '#28a745', '#dc3545',
      '#ffc107', '#17a2b8', '#6f42c1', '#e83e8c',
      '#fd7e14', '#20c997'
    ];
    return colors[Math.floor(Math.random() * colors.length)];
  }

  getMostUsedTags(): TagDto[] {
    return this.availableTags
      .filter(tag => tag.usageCount > 0)
      .sort((a, b) => b.usageCount - a.usageCount)
      .slice(0, 5);
  }

  toggleTagDropdown(): void {
    this.showTagDropdown = !this.showTagDropdown;
    if (this.showTagDropdown) {
      this.tagSearchTerm = '';
      this.filterTags();
    }
  }

  selectTag(tag: TagDto): void {
    if (this.selectedItem && tag.id) {
      this.addTagToItem(this.selectedItem, tag);
    }
    this.showTagDropdown = false;
  }

  // Filtering methods for searching
  filteredTodoItems(): TodoItemDto[] {
    if (!this.selectedList || !this.selectedList.items) {
      return [];
    }

    let items = this.selectedList.items;

    // Apply tag filtering
    if (this.selectedTags.length > 0) {
      items = items.filter(item =>
        item.tags && this.selectedTags.some(selectedTag =>
          item.tags.some(itemTag => itemTag.id === selectedTag.id)
        )
      );
    }

    // Apply text search
    if (this.searchTerm && this.searchTerm.trim()) {
      const searchTermLower = this.searchTerm.toLowerCase().trim();
      items = items.filter(item => {
        let matches = false;

        // Search in title
        if (this.searchInTitle && item.title && item.title.toLowerCase().includes(searchTermLower)) {
          matches = true;
        }

        // Search in note
        if (this.searchInNote && item.note && item.note.toLowerCase().includes(searchTermLower)) {
          matches = true;
        }

        // Search in tag names
        if (this.searchInTags && item.tags && item.tags.some(tag =>
          tag.name && tag.name.toLowerCase().includes(searchTermLower)
        )) {
          matches = true;
        }

        return matches;
      });
    }

    return items;
  }

  toggleTagFilter(tag: TagDto): void {
    const index = this.selectedTags.findIndex(t => t.id === tag.id);
    if (index > -1) {
      this.selectedTags.splice(index, 1);
    } else {
      this.selectedTags.push(tag);
    }
  }

  isTagSelected(tag: TagDto): boolean {
    return this.selectedTags.some(t => t.id === tag.id);
  }

  clearTagFilters(): void {
    this.selectedTags = [];
  }

  clearSearch(): void {
    this.searchTerm = '';
  }

  clearAllFilters(): void {
    this.selectedTags = [];
    this.searchTerm = '';
  }

  shouldShowCreateTag(): boolean {
    return this.tagSearchTerm &&
           !this.filteredTags.find(t => t.name.toLowerCase() === this.tagSearchTerm.toLowerCase());
  }

  isTagAlreadyAdded(tag: TagDto): boolean {
    return this.selectedItem?.tags?.some(t => t.id === tag.id) || false;
  }

  getTagButtonText(tag: TagDto): string {
    return this.isTagAlreadyAdded(tag) ? 'Added' : 'Add';
  }

  // Bulk operations methods
  toggleBulkMode(): void {
    this.bulkMode = !this.bulkMode;
    if (!this.bulkMode) {
      this.selectedItems.clear();
    }
  }

  toggleItemSelection(itemId: number): void {
    if (this.selectedItems.has(itemId)) {
      this.selectedItems.delete(itemId);
    } else {
      this.selectedItems.add(itemId);
    }
  }

  isItemSelected(itemId: number): boolean {
    return this.selectedItems.has(itemId);
  }

  selectAllItems(): void {
    const items = this.filteredTodoItems();
    items.forEach(item => {
      if (item.id) this.selectedItems.add(item.id);
    });
  }

  clearSelection(): void {
    this.selectedItems.clear();
  }

  addTagToSelectedItems(tag: TagDto): void {
    const items = this.filteredTodoItems().filter(item =>
      item.id && this.selectedItems.has(item.id)
    );

    items.forEach(item => {
      if (item.id && tag.id && (!item.tags || !item.tags.find(t => t.id === tag.id))) {
        this.addTagToItem(item, tag);
      }
    });
  }

  removeTagFromSelectedItems(tag: TagDto): void {
    const items = this.filteredTodoItems().filter(item =>
      item.id && this.selectedItems.has(item.id)
    );

    items.forEach(item => {
      if (item.id && tag.id && item.tags && item.tags.find(t => t.id === tag.id)) {
        this.removeTagFromItem(item, tag);
      }
    });
  }

  getSelectedItemsCount(): number {
    return this.selectedItems.size;
  }

  // Search options methods
  toggleSearchOptions(): void {
    this.showSearchOptions = !this.showSearchOptions;
  }

  hasActiveSearchFilters(): boolean {
    return !this.searchInTitle || !this.searchInNote || !this.searchInTags;
  }
}
