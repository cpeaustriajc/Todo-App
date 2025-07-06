import { Component, OnInit } from '@angular/core';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import {
  TodoListsClient, TodoItemsClient,
  TodoListDto, TodoItemDto
} from '../web-api-client';

@Component({
  selector: 'app-trash-component',
  templateUrl: './trash.component.html',
  styleUrls: ['./trash.component.scss']
})
export class TrashComponent implements OnInit {
  deletedLists: TodoListDto[] = [];
  deletedItems: TodoItemDto[] = [];
  loading = false;
  confirmModalRef: BsModalRef;
  selectedAction: 'restore' | 'purge' = 'restore';
  selectedItem: TodoItemDto | TodoListDto | null = null;
  selectedType: 'list' | 'item' = 'item';

  constructor(
    private listsClient: TodoListsClient,
    private itemsClient: TodoItemsClient,
    private modalService: BsModalService
  ) { }

  ngOnInit(): void {
    this.loadTrashData();
  }

  loadTrashData(): void {
    this.loading = true;
    
    this.listsClient.getDeletedTodoLists(undefined, undefined).subscribe(
      result => {
        this.deletedLists = result.items || [];
        this.loadDeletedItems();
      },
      error => {
        console.error('Error loading deleted lists:', error);
        this.loading = false;
      }
    );
  }

  loadDeletedItems(): void {
    this.itemsClient.getDeletedTodoItems(undefined, undefined).subscribe(
      result => {
        this.deletedItems = result.items || [];
        this.loading = false;
      },
      error => {
        console.error('Error loading deleted items:', error);
        this.loading = false;
      }
    );
  }

  confirmRestore(item: TodoItemDto | TodoListDto, type: 'list' | 'item', template: any): void {
    this.selectedItem = item;
    this.selectedType = type;
    this.selectedAction = 'restore';
    this.confirmModalRef = this.modalService.show(template);
  }

  confirmPurge(item: TodoItemDto | TodoListDto, type: 'list' | 'item', template: any): void {
    this.selectedItem = item;
    this.selectedType = type;
    this.selectedAction = 'purge';
    this.confirmModalRef = this.modalService.show(template);
  }

  executeAction(): void {
    if (!this.selectedItem) return;

    if (this.selectedAction === 'restore') {
      this.restoreItem();
    } else {
      this.purgeItem();
    }
  }

  restoreItem(): void {
    if (!this.selectedItem) return;

    if (this.selectedType === 'list') {
      this.listsClient.restore(this.selectedItem.id!).subscribe(
        () => {
          this.confirmModalRef.hide();
          this.loadTrashData();
        },
        error => console.error('Error restoring list:', error)
      );
    } else {
      this.itemsClient.restore(this.selectedItem.id!).subscribe(
        () => {
          this.confirmModalRef.hide();
          this.loadTrashData();
        },
        error => console.error('Error restoring item:', error)
      );
    }
  }

  purgeItem(): void {
    if (!this.selectedItem) return;

    if (this.selectedType === 'list') {
      this.listsClient.purge(this.selectedItem.id!).subscribe(
        () => {
          this.confirmModalRef.hide();
          this.loadTrashData();
        },
        error => console.error('Error purging list:', error)
      );
    } else {
      this.itemsClient.purge(this.selectedItem.id!).subscribe(
        () => {
          this.confirmModalRef.hide();
          this.loadTrashData();
        },
        error => console.error('Error purging item:', error)
      );
    }
  }

  getItemTypeName(): string {
    return this.selectedType === 'list' ? 'Todo List' : 'Todo Item';
  }

  getActionName(): string {
    return this.selectedAction === 'restore' ? 'restore' : 'permanently delete';
  }
}
