///// <reference path="Microsoft.JSInterop.d.ts"/>
import { IDBPDatabase, IDBPObjectStore, deleteDB, openDB } from 'idb';
import { IDbInformation, IDbStore, IDotNetInstanceWrapper, IIndexSearch, IStoreRecord } from './InteropInterfaces';

export class IndexedDbManager {

    private dbInstance: any = undefined;
    private dotnetCallback = (message: string) => { };

    constructor() { }

    public openDb = async (data: IDbStore, instanceWrapper: IDotNetInstanceWrapper): Promise<string> => {
        const dbStore = data;
        //just a test for the moment
        this.dotnetCallback = (message: string) => {
            instanceWrapper.instance.invokeMethodAsync(instanceWrapper.methodName, message);
        }

        try {
            if (!this.dbInstance || this.dbInstance.version < dbStore.version) {
                if (this.dbInstance) {
                    this.dbInstance.close();
                }
                this.dbInstance = await openDB(dbStore.dbName, dbStore.version, {
                    upgrade(upgradeDB, oldVersion, newVersion, transaction) { }
                });
            }
        } catch (e) {
            this.dbInstance = await openDB(dbStore.dbName);
        }

        return `IndexedDB ${data.dbName} opened`;
    }

    public getDbInfo = async (dbName: string): Promise<IDbInformation> => {
        if (!this.dbInstance) {
            this.dbInstance = await openDB(dbName);
        }

        const currentDb = <IDBDatabase>this.dbInstance;

        let getStoreNames = (list: DOMStringList): string[] => {
            let names: string[] = [];
            for (var i = 0; i < list.length; i++) {
                names.push(list[i]);
            }
            return names;
        }
        const dbInfo: IDbInformation = {
            version: currentDb.version,
            storeNames: getStoreNames(currentDb.objectStoreNames)
        };

        return dbInfo;
    }

    public deleteDb = async (dbName: string): Promise<string> => {
        this.dbInstance.close();

        await deleteDB(dbName);

        this.dbInstance = undefined;

        return `The database ${dbName} has been deleted`;
    }

    public addRecord = async (record: IStoreRecord): Promise<string> => {
        const stName = record.storename;
        let itemToSave = record.data;
        const tx = this.getTransaction(this.dbInstance, stName, 'readwrite');
        const objectStore = tx.objectStore(stName);

        itemToSave = this.checkForKeyPath(objectStore, itemToSave);

        const result = await this.dbInstance.add(stName, itemToSave, record.key);

        return `Added new record with id ${result}`;
    }

    public updateRecord = async (record: IStoreRecord): Promise<string> => {
        const stName = record.storename;
        const result = await this.dbInstance.put(stName, record.data, record.key);

        return `updated record with id ${result}`;
    }

    public getRecords = async (storeName: string): Promise<any> => {
        const tx = this.getTransaction(this.dbInstance, storeName, 'readonly');
        let results = await tx.objectStore(storeName).getAll();

        await tx.done;

        return results;
    }

    public clearStore = async (storeName: string): Promise<string> => {
        const tx = this.getTransaction(this.dbInstance, storeName, 'readwrite');
        tx.objectStore<string>(storeName).clear;

        await tx.done;

        return `Store ${storeName} cleared`;
    }

    public getRecordByIndex = async (searchData: IIndexSearch): Promise<any> => {
        const tx = this.getTransaction(this.dbInstance, searchData.storename, 'readonly');
        const results = await tx.objectStore(searchData.storename)
            .index(searchData.indexName)
            .get(searchData.queryValue);

        await tx.done;
        return results;
    }

    public getAllRecordsByIndex = async (searchData: IIndexSearch): Promise<any> => {
        const tx = this.getTransaction(this.dbInstance, searchData.storename, 'readonly');
        const store = tx.objectStore(searchData.storename);
        let results: any[] = [];

        for await (const cursor of store.index(searchData.indexName).iterate(searchData.queryValue)) {
            results.push(cursor.value);
        }

        await tx.done;

        return results;
    }

    public getRecordById = async (storename: string, id: any): Promise<any> => {
        const tx = this.getTransaction(this.dbInstance, storename, 'readonly');

        let result = await tx.objectStore(storename).get(id);
        return result;
    }

    public deleteRecord = async (storename: string, id: any): Promise<string> => {
        if (!this.dbInstance) {
            throw new Error("No database instance is open.");
        }

        await this.dbInstance.delete(storename, id);

        return `Record with id: ${id} deleted`;
    }

    private getTransaction(dbInstance: IDBPDatabase, stName: string, mode?: 'readonly' | 'readwrite') {
        const tx = dbInstance.transaction(stName, mode);

        tx.done.catch(
            (err: Error) => {
                if (err) {
                    console.error((err as Error).message);
                } else {
                    console.error('Undefined error in getTransaction()');
                }
            });

        return tx;
    }

    // Currently don't support aggregate keys
    private checkForKeyPath(objectStore: IDBPObjectStore, data: any) {
        if (!objectStore.autoIncrement || !objectStore.keyPath) {
            return data;
        }

        if (typeof objectStore.keyPath !== 'string') {
            return data;
        }

        const keyPath = objectStore.keyPath as string;

        if (!data[keyPath]) {
            delete data[keyPath];
        }
        return data;
    }
}